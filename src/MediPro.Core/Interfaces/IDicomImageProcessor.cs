using MediPro.Core.Models;

namespace MediPro.Core.Interfaces;

/// <summary>
/// Processes raw DICOM pixel data into printable 8-bit images
/// by applying Window/Level LUT transformations and format conversion.
/// </summary>
public interface IDicomImageProcessor
{
    /// <summary>
    /// Processes a DICOM file: applies Window/Level LUT, converts to 8-bit,
    /// scales to target DPI, and saves a printable image to disk.
    /// </summary>
    /// <param name="job">The incoming DICOM job with file path and header metadata.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A fully populated PrintJob with the rendered image path.</returns>
    Task<PrintJob> ProcessAsync(DicomJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a specific Window Center / Window Width to raw pixel data.
    /// Returns the mapped 8-bit grayscale values.
    /// </summary>
    /// <param name="rawPixels">Raw 16-bit pixel data array.</param>
    /// <param name="windowCenter">DICOM tag (0028,1050) value.</param>
    /// <param name="windowWidth">DICOM tag (0028,1051) value.</param>
    /// <returns>8-bit mapped pixel data.</returns>
    byte[] ApplyWindowLevelLut(ushort[] rawPixels, double windowCenter, double windowWidth);
}
