using System.Threading.Channels;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.Network;
using MediPro.Core.Models;
using Microsoft.Extensions.Logging;

namespace MediPro.Dicom.Services;

/// <summary>
/// DICOM C-STORE SCP implementation using fo-dicom.
/// Handles incoming DICOM associations and stores received files to the temp directory.
/// Each received file is enqueued into the processing channel for downstream handling.
/// </summary>
public class CStoreScp : DicomService, IDicomServiceProvider, IDicomCStoreProvider
{
    private static ChannelWriter<DicomJob>? s_jobChannel;
    private static string s_tempStoragePath = @"C:\MediPro\DicomTemp";
    private static ILogger? s_logger;

    private readonly ILogger<CStoreScp> _logger;

    public CStoreScp(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger<CStoreScp> logger,
        DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, logger, dependencies)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configures the static dependencies shared across all SCP instances.
    /// Must be called before starting the DICOM server.
    /// </summary>
    public static void Configure(
        ChannelWriter<DicomJob> jobChannel,
        string tempStoragePath,
        ILogger logger)
    {
        s_jobChannel = jobChannel ?? throw new ArgumentNullException(nameof(jobChannel));
        s_tempStoragePath = tempStoragePath;
        s_logger = logger;
    }

    /// <inheritdoc />
    public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        _logger.LogInformation(
            "Association request from AE '{CallingAe}' to '{CalledAe}' on port {Port}",
            association.CallingAE,
            association.CalledAE,
            association.RemotePort);

        // Accept all presentation contexts for maximum compatibility
        foreach (var pc in association.PresentationContexts)
        {
            // Accept with all offered transfer syntaxes
            pc.AcceptTransferSyntaxes(
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.JPEGProcess14SV1,
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.RLELossless,
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEG2000Lossy);
        }

        return SendAssociationAcceptAsync(association);
    }

    /// <inheritdoc />
    public Task OnReceiveAssociationReleaseRequestAsync()
    {
        _logger.LogDebug("Association released");
        return SendAssociationReleaseResponseAsync();
    }

    /// <inheritdoc />
    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
    {
        _logger.LogWarning("Association aborted: Source={Source}, Reason={Reason}", source, reason);
    }

    /// <inheritdoc />
    public void OnConnectionClosed(Exception? exception)
    {
        if (exception is not null)
            _logger.LogError(exception, "DICOM connection closed with error");
        else
            _logger.LogDebug("DICOM connection closed");
    }

    /// <inheritdoc />
    public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
    {
        try
        {
            var sopInstanceUid = request.SOPInstanceUID.UID;
            var callingAe = Association.CallingAE ?? string.Empty;
            var calledAe = Association.CalledAE ?? string.Empty;

            _logger.LogInformation(
                "C-STORE received: SOP={SopUid}, Modality={Modality}, CallingAE={CallingAe}",
                sopInstanceUid,
                request.Dataset.GetSingleValueOrDefault(DicomTag.Modality, "Unknown"),
                callingAe);

            // Ensure temp directory exists
            Directory.CreateDirectory(s_tempStoragePath);

            // Save .dcm file to temp storage with unique filename
            var fileName = $"{sopInstanceUid}.dcm";
            var filePath = Path.Combine(s_tempStoragePath, fileName);
            await request.File.SaveAsync(filePath).ConfigureAwait(false);

            // Extract key DICOM header tags
            var dataset = request.Dataset;
            var job = new DicomJob
            {
                FilePath = filePath,
                SopInstanceUid = sopInstanceUid,
                CallingAeTitle = callingAe,
                CalledAeTitle = calledAe,
                ReceivedPort = Association.RemotePort,
                Modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty),
                PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty),
                PatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty),
                StudyDescription = dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty),
                BodyPartExamined = dataset.GetSingleValueOrDefault(DicomTag.BodyPartExamined, string.Empty),
                PhotometricInterpretation = dataset.GetSingleValueOrDefault(DicomTag.PhotometricInterpretation, string.Empty),
                BitsAllocated = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16),
                WindowCenter = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 0.0),
                WindowWidth = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 0.0),
                StudyInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SeriesInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                ReferringPhysicianName = dataset.GetSingleValueOrDefault(DicomTag.ReferringPhysicianName, string.Empty),
                InstitutionName = dataset.GetSingleValueOrDefault(DicomTag.InstitutionName, string.Empty)
            };

            // Enqueue for async processing via the Channel pipeline
            if (s_jobChannel is not null)
            {
                await s_jobChannel.WriteAsync(job).ConfigureAwait(false);
                _logger.LogDebug("DicomJob {JobId} enqueued for processing", job.JobId);
            }
            else
            {
                _logger.LogError("Job channel is not configured — DicomJob {JobId} dropped!", job.JobId);
            }

            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing C-STORE request for SOP {SopUid}",
                request.SOPInstanceUID?.UID ?? "Unknown");
            return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
        }
    }

    /// <inheritdoc />
    public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
    {
        _logger.LogError(e, "C-STORE exception for temp file {TempFile}", tempFileName);
        return Task.CompletedTask;
    }
}
