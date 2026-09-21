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

                    using var xImage = XImage.FromFile(imagePath);

                    var imageWidth = xImage.PixelWidth;
                    var imageHeight = xImage.PixelHeight;

                    // 横版图片 → 页面也横放（交换宽高）
                    var actualWidth = imageWidth > imageHeight ? pageHeight : pageWidth;
                    var actualHeight = imageWidth > imageHeight ? pageWidth : pageHeight;

                    var pdfPage = document.AddPage();
                    pdfPage.MediaBox = new PdfRectangle(new XRect(0, 0, actualWidth.Point, actualHeight.Point));

                    using var gfx = XGraphics.FromPdfPage(pdfPage);

                    double scale = Math.Min(actualWidth.Point / imageWidth, actualHeight.Point / imageHeight);
                    double displayWidth = imageWidth * scale;
                    double displayHeight = imageHeight * scale;

                    gfx.DrawImage(xImage, (actualWidth.Point - displayWidth) / 2, (actualHeight.Point - displayHeight) / 2, displayWidth, displayHeight);

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