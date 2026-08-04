using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using System.IO;

namespace PdfToolbox.Services;

public class PdfToolService
{
    /// <summary>
    /// 获取 PDF 文件总页数
    /// </summary>
    public int GetPdfPageCount(string filePath)
    {
        try
        {
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            return document.PageCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 添加文字水印到 PDF
    /// </summary>
    public async Task<string?> AddTextWatermarkAsync(
        string inputPath,
        string outputPath,
        string watermarkText,
        double fontSize = 48,
        string color = "#C0C0C0",
        double opacity = 0.3,
        double angle = -45,
        bool tile = true,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                var totalPages = document.PageCount;

                var brush = new XSolidBrush(ParseColor(color));
                brush.Color = XColor.FromArgb((int)(opacity * 255), brush.Color.R, brush.Color.G, brush.Color.B);

                // 使用 Unicode 编码支持中文
                var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.EmbedCompleteFontFile);
                var font = new XFont("SimHei", fontSize, XFontStyleEx.Regular, fontOptions);

                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.Pages[i];
                    using var gfx = XGraphics.FromPdfPage(page);

                    var pageWidth = page.Width.Point;
                    var pageHeight = page.Height.Point;

                    if (tile)
                    {
                        var size = gfx.MeasureString(watermarkText, font);
                        var spacingX = size.Width + 100;
                        var spacingY = size.Height + 100;

                        for (double y = -pageHeight; y < pageHeight * 2; y += spacingY)
                        {
                            for (double x = -pageWidth; x < pageWidth * 2; x += spacingX)
                            {
                                gfx.Save();
                                gfx.TranslateTransform(x, y);
                                gfx.RotateTransform(angle);
                                gfx.DrawString(watermarkText, font, brush, 0, 0);
                                gfx.Restore();
                            }
                        }
                    }
                    else
                    {
                        var size = gfx.MeasureString(watermarkText, font);
                        var x = (pageWidth - size.Width) / 2;
                        var y = (pageHeight - size.Height) / 2;

                        gfx.Save();
                        gfx.TranslateTransform(x, y);
                        gfx.RotateTransform(angle);
                        gfx.DrawString(watermarkText, font, brush, 0, 0);
                        gfx.Restore();
                    }

                    progress?.Report((int)((double)(i + 1) / totalPages * 100));
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
                return $"添加水印失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 添加图片水印到 PDF
    /// </summary>
    public async Task<string?> AddImageWatermarkAsync(
        string inputPath,
        string outputPath,
        string watermarkImagePath,
        double width = 150,
        double height = 150,
        double opacity = 0.3,
        bool tile = false,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                var totalPages = document.PageCount;

                using var watermarkImage = XImage.FromFile(watermarkImagePath);
                var imgWidth = width;
                var imgHeight = height;

                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.Pages[i];
                    using var gfx = XGraphics.FromPdfPage(page);

                    var pageWidth = page.Width.Point;
                    var pageHeight = page.Height.Point;

                    if (tile)
                    {
                        var spacingX = imgWidth + 50;
                        var spacingY = imgHeight + 50;

                        for (double y = 0; y < pageHeight; y += spacingY)
                        {
                            for (double x = 0; x < pageWidth; x += spacingX)
                            {
                                gfx.DrawImage(watermarkImage, x, y, imgWidth, imgHeight);
                            }
                        }
                    }
                    else
                    {
                        var x = (pageWidth - imgWidth) / 2;
                        var y = (pageHeight - imgHeight) / 2;
                        gfx.DrawImage(watermarkImage, x, y, imgWidth, imgHeight);
                    }

                    progress?.Report((int)((double)(i + 1) / totalPages * 100));
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
                return $"添加图片水印失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 加密 PDF（设置密码）
    /// </summary>
    public async Task<string?> EncryptPdfAsync(
        string inputPath,
        string outputPath,
        string userPassword,
        string ownerPassword,
        bool allowPrinting = true,
        bool allowCopy = true,
        bool allowModify = true,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);

                var securitySettings = document.SecuritySettings;
                securitySettings.UserPassword = userPassword;
                securitySettings.OwnerPassword = ownerPassword;

                if (!allowPrinting)
                    securitySettings.PermitPrint = false;
                if (!allowCopy)
                    securitySettings.PermitExtractContent = false;
                if (!allowModify)
                    securitySettings.PermitModifyDocument = false;

                document.Save(outputPath);
                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"加密失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 解密 PDF（移除密码）
    /// </summary>
    public async Task<string?> DecryptPdfAsync(
        string inputPath,
        string outputPath,
        string password,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 用密码打开加密的PDF（以只读导入模式）
                using var inputDocument = PdfReader.Open(inputPath, password, PdfDocumentOpenMode.Import);

                // 创建新的未加密文档
                var outputDocument = new PdfDocument();

                // 复制所有页面到新文档（这样会去除加密）
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }

                // 保存为未加密的PDF
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                outputDocument.Save(outputPath);
                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"解密失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 旋转 PDF 页面
    /// </summary>
    public async Task<string?> RotatePagesAsync(
        string inputPath,
        string outputPath,
        int rotationAngle,
        string pageRange = "all",
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                var totalPages = document.PageCount;

                var pagesToRotate = ParsePageRange(pageRange, totalPages);

                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.Pages[i];
                    if (pagesToRotate.Contains(i + 1))
                    {
                        page.Rotate = (page.Rotate + rotationAngle) % 360;
                        if (page.Rotate < 0) page.Rotate += 360;
                    }

                    progress?.Report((int)((double)(i + 1) / totalPages * 100));
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
                return $"旋转页面失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 去除 PDF 中的水印（基于图像处理）
    /// </summary>
    public async Task<string?> RemoveWatermarkAsync(
        string inputPath,
        string outputPath,
        string watermarkColor,
        int tolerance = 60,
        double strength = 0.7,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var color = ParseColor(watermarkColor);
        var removalService = new WatermarkRemovalService();
        return await removalService.RemoveWatermarkAsync(
            inputPath, outputPath,
            System.Drawing.Color.FromArgb(color.R, color.G, color.B),
            tolerance, strength, progress, cancellationToken);
    }

    private XColor ParseColor(string color)
    {
        color = color.TrimStart('#');
        if (color.Length == 6)
        {
            var r = Convert.ToByte(color.Substring(0, 2), 16);
            var g = Convert.ToByte(color.Substring(2, 2), 16);
            var b = Convert.ToByte(color.Substring(4, 2), 16);
            return XColor.FromArgb(r, g, b);
        }
        return XColors.Gray;
    }

    private List<int> ParsePageRange(string rangeStr, int maxPages)
    {
        if (string.IsNullOrWhiteSpace(rangeStr) || rangeStr.ToLower() == "all")
        {
            return Enumerable.Range(1, maxPages).ToList();
        }

        var pages = new List<int>();
        var parts = rangeStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Contains('-'))
            {
                var rangeParts = trimmed.Split('-');
                if (rangeParts.Length == 2 &&
                    int.TryParse(rangeParts[0].Trim(), out int start) &&
                    int.TryParse(rangeParts[1].Trim(), out int end))
                {
                    start = Math.Max(1, Math.Min(start, maxPages));
                    end = Math.Max(1, Math.Min(end, maxPages));
                    for (int i = start; i <= end; i++)
                    {
                        if (!pages.Contains(i)) pages.Add(i);
                    }
                }
            }
            else if (int.TryParse(trimmed, out int pageNum))
            {
                pageNum = Math.Max(1, Math.Min(pageNum, maxPages));
                if (!pages.Contains(pageNum)) pages.Add(pageNum);
            }
        }

        return pages;
    }
}
