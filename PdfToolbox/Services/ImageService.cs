using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using System.IO;

namespace PdfToolbox.Services;

public class ImageService
{
    private static readonly string[] SupportedImageFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif" };

    public bool IsImageFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedImageFormats.Contains(ext);
    }

    public async Task<string?> ImagesToPdfAsync(
        IEnumerable<string> imagePaths,
        string outputPath,
        string pageSize = "A4",
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var images = imagePaths.ToList();
                var total = images.Count;

                using var document = new PdfDocument();
                document.Info.Title = "Converted from Images";

                var (pageWidth, pageHeight) = GetPageDimensions(pageSize);

                for (int i = 0; i < total; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var imagePath = images[i];

                    var pdfPage = document.AddPage();
                    pdfPage.MediaBox = new PdfRectangle(new XRect(0, 0, pageWidth.Point, pageHeight.Point));

                    using var gfx = XGraphics.FromPdfPage(pdfPage);
                    using var xImage = XImage.FromFile(imagePath);

                    var imageWidth = xImage.PixelWidth;
                    var imageHeight = xImage.PixelHeight;

                    double scale = Math.Min(pageWidth.Point / imageWidth, pageHeight.Point / imageHeight);
                    double displayWidth = imageWidth * scale;
                    double displayHeight = imageHeight * scale;

                    gfx.DrawImage(xImage, (pageWidth.Point - displayWidth) / 2, (pageHeight.Point - displayHeight) / 2, displayWidth, displayHeight);

                    progress?.Report((int)((double)(i + 1) / total * 100));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                document.Save(outputPath);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"图片转PDF失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    private (XUnit Width, XUnit Height) GetPageDimensions(string size)
    {
        return size.ToUpperInvariant() switch
        {
            "A3" => (XUnit.FromPoint(841.89), XUnit.FromPoint(1190.55)),
            "LETTER" => (XUnit.FromPoint(612), XUnit.FromPoint(792)),
            "LEGAL" => (XUnit.FromPoint(612), XUnit.FromPoint(1008)),
            _ => (XUnit.FromPoint(595.28), XUnit.FromPoint(841.89))
        };
    }
}