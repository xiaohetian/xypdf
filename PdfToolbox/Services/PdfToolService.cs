using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfToolbox.Services;

public class PdfToolService
{
    public int GetPdfPageCount(string filePath)
    {
        try
        {
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            return document.PageCount;
        }
        catch { return 0; }
    }

    // ===== 添加文字水印 =====
    public async Task<string?> AddTextWatermarkAsync(
        string inputPath, string outputPath, string watermarkText,
        double fontSize = 48, string color = "#C0C0C0", double opacity = 0.3,
        double angle = -45, bool tile = true,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                var totalPages = document.PageCount;
                var brushColor = ParseColor(color);
                var brush = new XSolidBrush(XColor.FromArgb((int)(opacity * 255), brushColor.R, brushColor.G, brushColor.B));
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
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"添加水印失败: {ex.Message}"; }
        }, cancellationToken);
    }

    // ===== 添加图片水印 =====
    public async Task<string?> AddImageWatermarkAsync(
        string inputPath, string outputPath, string watermarkImagePath,
        double width = 150, double height = 150, double opacity = 0.3, bool tile = false,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
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
                            for (double x = 0; x < pageWidth; x += spacingX)
                                gfx.DrawImage(watermarkImage, x, y, imgWidth, imgHeight);
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
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"添加图片水印失败: {ex.Message}"; }
        }, cancellationToken);
    }

    // ===== 水印去除（v4.0 矢量优先）=====
    /// <summary>
    /// 去除 PDF 水印：
    /// 1. 矢量层：删除含目标灰色的文本 span（通过内容流匹配着色命令）
    /// 2. 图像层：若矢量层无匹配，退回位图处理
    /// </summary>
    public async Task<string?> RemoveWatermarkAsync(
        string inputPath, string outputPath,
        string watermarkColor, int tolerance = 60, double strength = 0.7,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var targetColor = ParseColor(watermarkColor);
                var targetHex = $"{targetColor.R:X2}{targetColor.G:X2}{targetColor.B:X2}";
                var outputDir = Path.GetDirectoryName(outputPath)!;
                Directory.CreateDirectory(outputDir);
                var totalPages = GetPdfPageCount(inputPath);
                if (totalPages == 0) return "无法读取 PDF 文件";

                // Strategy A: Vector-layer removal
                var vectorSuccess = TryVectorWatermarkRemoval(inputPath, outputPath, targetHex, tolerance, progress, cancellationToken);
                if (vectorSuccess)
                    return null;

                // Strategy B: Fall back to image-based removal
                var removalService = new WatermarkRemovalService();
                return removalService.RemoveWatermarkAsync(
                    inputPath, outputPath,
                    targetColor, tolerance, strength, progress, cancellationToken).Result;
            }
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"去除水印失败: {ex.Message}"; }
        }, cancellationToken);
    }

    // ===== 矢量层水印去除 =====
    private static bool TryVectorWatermarkRemoval(
        string inputPath, string outputPath,
        string targetHex, int tolerance,
        IProgress<int>? progress, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
            bool anyModified = false;
            int pageCount = doc.PageCount;

            for (int i = 0; i < pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = doc.Pages[i];

                // Process page content stream via page.Stream
                if (page.Stream != null)
                {
                    var original = Encoding.UTF8.GetString(page.Stream.Value);
                    var result = ProcessContentStream(original, targetHex, tolerance);
                    if (result != original)
                    {
                        page.Stream.Value = Encoding.UTF8.GetBytes(result);
                        anyModified = true;
                    }
                }

                // Process Form XObjects in page resources
                var resources = page.Resources;
                if (resources != null)
                {
                    foreach (var elemName in resources.Elements.KeyNames)
                    {
                        var nameStr = elemName.ToString();
                        if (nameStr.StartsWith("/X"))
                        {
                            var elem = resources.Elements[elemName];
                            if (elem is PdfFormXObject xobj && xobj.Stream != null)
                            {
                                var formOriginal = Encoding.UTF8.GetString(xobj.Stream.Value);
                                var formResult = ProcessContentStream(formOriginal, targetHex, tolerance);
                                if (formResult != formOriginal)
                                {
                                    xobj.Stream.Value = Encoding.UTF8.GetBytes(formResult);
                                    anyModified = true;
                                }
                            }
                        }
                    }
                }

                progress?.Report((int)((double)(i + 1) / pageCount * 50));
            }

            if (anyModified)
            {
                doc.Save(outputPath);
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    /// <summary>
    /// 处理内容流：移除含目标颜色的着色命令及随后的文本绘制操作。
    /// 使用正则匹配 "R G B k" 模式（RGB 归一化到 [0,1]），替换为白色（1 1 1）覆盖。
    /// </summary>
    private static string ProcessContentStream(string content, string targetHex, int tolerance)
    {
        var result = new StringBuilder();
        var lines = content.Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Detect color command: "R G B k" or "R G B K" where RGB matches target
            if (IsTargetColorLine(trimmed, targetHex, tolerance))
            {
                // Replace with white (1 1 1 k) to cover the watermark
                result.AppendLine("1 1 1 k");
                i++;
                // Skip following text-showing operators (Tj, ', ", TJ, T*)
                while (i < lines.Length)
                {
                    var nextTrimmed = lines[i].Trim();
                    if (nextTrimmed == "ET" || nextTrimmed == "Q" || nextTrimmed == "BT" || nextTrimmed.StartsWith("q"))
                        break;
                    if (nextTrimmed.EndsWith("Tj") || nextTrimmed.EndsWith("'") ||
                        nextTrimmed.EndsWith("\"") || nextTrimmed.EndsWith("TJ") || nextTrimmed.EndsWith("T*"))
                    {
                        // Replace text showing with a no-op
                        result.AppendLine("% [watermark removed]");
                        i++;
                        continue;
                    }
                    // Also replace any other color commands in the same block
                    if (nextTrimmed.EndsWith(" k") || nextTrimmed.EndsWith(" K"))
                    {
                        result.AppendLine("% [watermark removal] " + lines[i]);
                        i++;
                        continue;
                    }
                    break;
                }
                continue;
            }

            // BT/ET block: check if contains target color
            if (trimmed == "BT")
            {
                var blockLines = new List<string> { line };
                i++;
                bool hasTargetColor = false;
                while (i < lines.Length)
                {
                    var blockLine = lines[i];
                    blockLines.Add(blockLine);
                    if (IsTargetColorLine(blockLine.Trim(), targetHex, tolerance))
                        hasTargetColor = true;
                    if (blockLine.Trim() == "ET") break;
                    i++;
                }

                if (hasTargetColor)
                {
                    // Replace color commands with white, comment out text operators
                    foreach (var bl in blockLines)
                    {
                        var bt = bl.Trim();
                        if (IsTargetColorLine(bt, targetHex, tolerance))
                            result.AppendLine("1 1 1 k");
                        else if (bt.EndsWith("Tj") || bt.EndsWith("'") || bt.EndsWith("\"") ||
                                 bt.EndsWith("TJ") || bt.EndsWith("T*"))
                            result.AppendLine("% [watermark removed]");
                        else
                            result.AppendLine(bl);
                    }
                }
                else
                {
                    foreach (var bl in blockLines)
                        result.AppendLine(bl);
                }
                i++;
                continue;
            }

            result.AppendLine(line);
            i++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Check if a line contains a color command matching the target hex color.
    /// PDF uses normalized [0,1] float values for RGB color commands.
    /// </summary>
    private static bool IsTargetColorLine(string line, string targetHex, int tolerance)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        // Match: "R G B k", "R G B K", "R G B rg", or "R G B RG"
        if (!line.EndsWith(" k") && !line.EndsWith(" K") &&
            !line.EndsWith(" rg") && !line.EndsWith(" RG")) return false;

        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return false;

        // Format: "<r> <g> <b> <op>" where op is "k"/"K"/"rg"/"RG".
        // The three color floats immediately precede the single-token operator.
        if (float.TryParse(parts[parts.Length - 4], out float r) &&
            float.TryParse(parts[parts.Length - 3], out float g) &&
            float.TryParse(parts[parts.Length - 2], out float b))
        {
            byte br = (byte)(r * 255);
            byte bg = (byte)(g * 255);
            byte bb = (byte)(b * 255);
            string hex = $"{br:X2}{bg:X2}{bb:X2}";
            int tr = int.Parse(targetHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int tg = int.Parse(targetHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int tb = int.Parse(targetHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return Math.Abs(br - tr) <= tolerance &&
                   Math.Abs(bg - tg) <= tolerance &&
                   Math.Abs(bb - tb) <= tolerance;
        }
        return false;
    }

    // ===== 工具方法 =====

    private static System.Drawing.Color ParseColor(string color)
    {
        color = color.TrimStart('#');
        if (color.Length == 6)
            return System.Drawing.Color.FromArgb(
                Convert.ToByte(color.Substring(0, 2), 16),
                Convert.ToByte(color.Substring(2, 2), 16),
                Convert.ToByte(color.Substring(4, 2), 16));
        return System.Drawing.Color.FromArgb(192, 192, 192);
    }

    // ===== 加密/解密 =====

    public async Task<string?> EncryptPdfAsync(
        string inputPath, string outputPath, string userPassword, string ownerPassword,
        bool allowPrinting = true, bool allowCopy = true, bool allowModify = true,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                document.SecuritySettings.UserPassword = userPassword;
                document.SecuritySettings.OwnerPassword = ownerPassword;
                if (!allowPrinting) document.SecuritySettings.PermitPrint = false;
                if (!allowCopy) document.SecuritySettings.PermitExtractContent = false;
                if (!allowModify) document.SecuritySettings.PermitModifyDocument = false;
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                document.Save(outputPath);
                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"加密失败: {ex.Message}"; }
        }, cancellationToken);
    }

    public async Task<string?> DecryptPdfAsync(
        string inputPath, string outputPath, string password,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputDocument = PdfReader.Open(inputPath, password, PdfDocumentOpenMode.Import);
                var outputDocument = new PdfDocument();
                for (int i = 0; i < inputDocument.PageCount; i++)
                    outputDocument.AddPage(inputDocument.Pages[i]);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                outputDocument.Save(outputPath);
                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"解密失败: {ex.Message}"; }
        }, cancellationToken);
    }

    public async Task<string?> RotatePagesAsync(
        string inputPath, string outputPath, string pageRange, int degrees,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                var pages = ParsePageRange(pageRange, document.PageCount);
                foreach (var idx in pages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    document.Pages[idx - 1].Rotate = (document.Pages[idx - 1].Rotate + degrees) % 360;
                    progress?.Report((int)((double)pages.IndexOf(idx) / pages.Count * 100));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                document.Save(outputPath);
                return null;
            }
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"旋转页面失败: {ex.Message}"; }
        }, cancellationToken);
    }

    private List<int> ParsePageRange(string rangeStr, int maxPages)
    {
        if (string.IsNullOrWhiteSpace(rangeStr) || rangeStr.ToLower() == "all")
            return Enumerable.Range(1, maxPages).ToList();
        var pages = new List<int>();
        foreach (var part in rangeStr.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Contains('-'))
            {
                var rp = trimmed.Split('-');
                if (rp.Length == 2 && int.TryParse(rp[0].Trim(), out int s) && int.TryParse(rp[1].Trim(), out int e))
                {
                    s = Math.Max(1, Math.Min(s, maxPages));
                    e = Math.Max(1, Math.Min(e, maxPages));
                    for (int j = s; j <= e; j++) if (!pages.Contains(j)) pages.Add(j);
                }
            }
            else if (int.TryParse(trimmed, out int pn))
            {
                pn = Math.Max(1, Math.Min(pn, maxPages));
                if (!pages.Contains(pn)) pages.Add(pn);
            }
        }
        return pages;
    }
}
