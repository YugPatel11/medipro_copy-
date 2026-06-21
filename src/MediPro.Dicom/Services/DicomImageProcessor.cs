using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using ImageMagick;
using MediPro.Core.Interfaces;
using MediPro.Core.Models;
using MediPro.Dicom.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediPro.Dicom.Services;

/// <summary>
/// Production image processing engine that converts raw DICOM pixel data
/// into printable 8-bit images. Implements Window/Level LUT mapping
/// and uses Magick.NET for format conversion, DPI scaling, and gamma correction.
/// </summary>
public sealed class DicomImageProcessor : IDicomImageProcessor
{
    private readonly DicomServerConfig _config;
    private readonly ILogger<DicomImageProcessor> _logger;

    /// <summary>
    /// Modality-specific default Window/Level presets used when
    /// the DICOM file does not contain Window Center/Width tags.
    /// </summary>
    private static readonly Dictionary<string, (double Center, double Width)> ModalityPresets = new()
    {
        // CT presets
        ["CT_BONE"] = (400, 1800),
        ["CT_SOFT"] = (40, 400),
        ["CT_LUNG"] = (-600, 1600),
        ["CT_BRAIN"] = (40, 80),
        ["CT_ABDOMEN"] = (60, 400),
        ["CT_DEFAULT"] = (40, 400),

        // MR presets
        ["MR_BRAIN"] = (600, 1200),
        ["MR_DEFAULT"] = (800, 1600),

        // CR/DX presets
        ["CR_DEFAULT"] = (2048, 4096),
        ["DX_DEFAULT"] = (2048, 4096),

        // Mammography
        ["MG_DEFAULT"] = (3000, 6000),

        // Catch-all
        ["DEFAULT"] = (2048, 4096)
    };

    public DicomImageProcessor(
        IOptions<DicomServerConfig> config,
        ILogger<DicomImageProcessor> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PrintJob> ProcessAsync(DicomJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing DicomJob {JobId}: Modality={Modality}, Patient={Patient}",
            job.JobId, job.Modality, job.PatientName);

        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_config.OutputPath);

        var dicomFile = await DicomFile.OpenAsync(job.FilePath).ConfigureAwait(false);
        var dataset = dicomFile.Dataset;

        // Determine if this is a color image (RGB, YBR) or grayscale (MONOCHROME1/2)
        var photometric = job.PhotometricInterpretation.Trim().ToUpperInvariant();
        bool isColor = photometric.StartsWith("RGB") || photometric.StartsWith("YBR")
                       || photometric.Contains("PALETTE");

        bool isDryFilm = false; // Configurable per-printer, set downstream
        int targetDpi = _config.DefaultDpi;

        string outputFileName = $"{job.SopInstanceUid}.{_config.OutputFormat.ToLowerInvariant()}";
        string outputPath = Path.Combine(_config.OutputPath, outputFileName);

        int width, height;

        if (isColor)
        {
            // --- Color image pathway (Doppler, palette) ---
            (width, height) = await ProcessColorImageAsync(dataset, outputPath, targetDpi, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            // --- Grayscale image pathway with LUT ---
            (width, height) = await ProcessGrayscaleImageAsync(dataset, job, outputPath, targetDpi, cancellationToken)
                .ConfigureAwait(false);
        }

        var printJob = new PrintJob
        {
            OriginatingDicomJobId = job.JobId,
            RenderedImagePath = outputPath,
            TargetDpi = targetDpi,
            WidthPixels = width,
            HeightPixels = height,
            IsDryFilm = isDryFilm,
            IsColor = isColor,
            Modality = job.Modality,
            PatientName = job.PatientName,
            PatientId = job.PatientId,
            StudyDescription = job.StudyDescription,
            ReferringPhysicianName = job.ReferringPhysicianName,
            InstitutionName = job.InstitutionName,
            StudyInstanceUid = job.StudyInstanceUid
        };

        _logger.LogInformation(
            "DicomJob {JobId} processed → PrintJob {PrintJobId} ({W}x{H} @ {Dpi}DPI, Color={Color})",
            job.JobId, printJob.PrintJobId, width, height, targetDpi, isColor);

        return printJob;
    }

    /// <inheritdoc />
    public byte[] ApplyWindowLevelLut(ushort[] rawPixels, double windowCenter, double windowWidth)
    {
        var output = new byte[rawPixels.Length];
        double halfWidth = windowWidth / 2.0;
        double minVal = windowCenter - halfWidth;
        double maxVal = windowCenter + halfWidth;
        double scale = 255.0 / windowWidth;

        for (int i = 0; i < rawPixels.Length; i++)
        {
            double pixel = rawPixels[i];

            if (pixel <= minVal)
                output[i] = 0;
            else if (pixel >= maxVal)
                output[i] = 255;
            else
                output[i] = (byte)((pixel - minVal) * scale);
        }

        return output;
    }

    /// <summary>
    /// Processes a grayscale DICOM image by applying Window/Level LUT
    /// and rendering via Magick.NET.
    /// </summary>
    private async Task<(int Width, int Height)> ProcessGrayscaleImageAsync(
        DicomDataset dataset,
        DicomJob job,
        string outputPath,
        int targetDpi,
        CancellationToken ct)
    {
        // Get image dimensions
        int columns = dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);
        int rows = dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
        int bitsAllocated = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16);
        int bitsStored = dataset.GetSingleValueOrDefault(DicomTag.BitsStored, bitsAllocated);
        bool isMonochrome1 = job.PhotometricInterpretation.Contains("MONOCHROME1",
            StringComparison.OrdinalIgnoreCase);

        // Resolve Window Center / Window Width
        double wc = job.WindowCenter;
        double ww = job.WindowWidth;

        if (ww <= 0)
        {
            // No Window Width in DICOM header — use modality preset
            var preset = ResolvePreset(job.Modality, job.BodyPartExamined);
            wc = preset.Center;
            ww = preset.Width;
            _logger.LogDebug("Using preset WC/WW for {Modality}: Center={WC}, Width={WW}",
                job.Modality, wc, ww);
        }

        // Extract raw pixel data
        var pixelData = DicomPixelData.Create(dataset);
        var frameData = pixelData.GetFrame(0);
        var rawBytes = frameData.Data;

        // Convert to ushort array for 16-bit processing
        ushort[] rawPixels;
        if (bitsAllocated == 16)
        {
            rawPixels = new ushort[rawBytes.Length / 2];
            Buffer.BlockCopy(rawBytes, 0, rawPixels, 0, rawBytes.Length);
        }
        else if (bitsAllocated == 8)
        {
            rawPixels = new ushort[rawBytes.Length];
            for (int i = 0; i < rawBytes.Length; i++)
                rawPixels[i] = rawBytes[i];
        }
        else
        {
            // 12-bit packed — treat as 16-bit
            rawPixels = new ushort[rawBytes.Length / 2];
            Buffer.BlockCopy(rawBytes, 0, rawPixels, 0, rawBytes.Length);
        }

        // Apply Window/Level LUT mathematical mapping
        var lutMapped = ApplyWindowLevelLut(rawPixels, wc, ww);

        // Invert for MONOCHROME1 (white = 0 in DICOM convention)
        if (isMonochrome1)
        {
            for (int i = 0; i < lutMapped.Length; i++)
                lutMapped[i] = (byte)(255 - lutMapped[i]);
        }

        // Render with Magick.NET
        await Task.Run(() =>
        {
            var settings = new MagickReadSettings
            {
                Width = (uint)columns,
                Height = (uint)rows,
                Format = MagickFormat.Gray,
                Depth = 8
            };

            using var image = new MagickImage(lutMapped, settings);
            image.Density = new Density(targetDpi, targetDpi, DensityUnit.PixelsPerInch);
            // Neutral gamma; adjust per clinical need
            image.Quality = 95;
            image.Write(outputPath);

        }, ct).ConfigureAwait(false);

        return (columns, rows);
    }

    /// <summary>
    /// Processes a color DICOM image (e.g., Doppler ultrasound, palette color).
    /// Bypasses LUT and decodes as RGB directly.
    /// </summary>
    private async Task<(int Width, int Height)> ProcessColorImageAsync(
        DicomDataset dataset,
        string outputPath,
        int targetDpi,
        CancellationToken ct)
    {
        int columns = dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);
        int rows = dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
        int samplesPerPixel = dataset.GetSingleValueOrDefault(DicomTag.SamplesPerPixel, 3);

        var pixelData = DicomPixelData.Create(dataset);
        var frameData = pixelData.GetFrame(0);
        var rawBytes = frameData.Data;

        await Task.Run(() =>
        {
            var settings = new MagickReadSettings
            {
                Width = (uint)columns,
                Height = (uint)rows,
                Format = samplesPerPixel == 3 ? MagickFormat.Rgb : MagickFormat.Rgba,
                Depth = 8
            };

            using var image = new MagickImage(rawBytes, settings);
            image.Density = new Density(targetDpi, targetDpi, DensityUnit.PixelsPerInch);
            image.Quality = 95;
            image.Write(outputPath);

        }, ct).ConfigureAwait(false);

        return (columns, rows);
    }

    /// <summary>
    /// Resolves the appropriate Window/Level preset based on modality and body part.
    /// </summary>
    private static (double Center, double Width) ResolvePreset(string modality, string bodyPart)
    {
        var mod = modality.Trim().ToUpperInvariant();
        var body = bodyPart.Trim().ToUpperInvariant();

        // CT modality-specific presets
        if (mod == "CT")
        {
            if (body.Contains("BONE") || body.Contains("SPINE") || body.Contains("SKULL"))
                return ModalityPresets["CT_BONE"];
            if (body.Contains("LUNG") || body.Contains("CHEST"))
                return ModalityPresets["CT_LUNG"];
            if (body.Contains("BRAIN") || body.Contains("HEAD"))
                return ModalityPresets["CT_BRAIN"];
            if (body.Contains("ABDOMEN") || body.Contains("PELVIS"))
                return ModalityPresets["CT_ABDOMEN"];
            return ModalityPresets["CT_DEFAULT"];
        }

        // MR modality presets
        if (mod == "MR")
        {
            if (body.Contains("BRAIN") || body.Contains("HEAD"))
                return ModalityPresets["MR_BRAIN"];
            return ModalityPresets["MR_DEFAULT"];
        }

        // Other modalities
        var key = $"{mod}_DEFAULT";
        return ModalityPresets.GetValueOrDefault(key, ModalityPresets["DEFAULT"]);
    }
}
