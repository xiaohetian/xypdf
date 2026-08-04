using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using System.IO;

namespace PdfToolbox.Services;

public class PdfService
{
    public async Task<string?> MergePdfsAsync(
        IEnumerable<string> inputPaths,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var files = inputPaths.ToList();
                var totalFiles = files.Count;
                var currentFile = 0;

                using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                outputDocument.Info.Title = "Merged PDF";
                outputDocument.Info.Creator = "PDF全能工具箱";

                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var inputDocument = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);

                    for (int i = 0; i < inputDocument.PageCount; i++)
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }

                    currentFile++;
                    progress?.Report((int)((double)currentFile / totalFiles * 100));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                outputDocument.Save(outputPath);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"合并PDF失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public async Task<string?> SplitPdfByRangeAsync(
        string inputPath,
        string outputDir,
        string fileNameTemplate,
        string pageRange,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputDocument = PdfSharp.Pdf.IO.PdfReader.Open(inputPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                var totalPages = inputDocument.PageCount;
                var ranges = ParsePageRange(pageRange, totalPages);

                Directory.CreateDirectory(outputDir);
                var pageIndex = 0;
                foreach (var range in ranges)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                    outputDocument.Info.Title = $"Split PDF - Pages {range.Start}-{range.End}";

                    for (int i = range.Start; i <= range.End && i <= totalPages; i++)
                    {
                        outputDocument.AddPage(inputDocument.Pages[i - 1]);
                    }

                    var outputPath = Path.Combine(outputDir, string.Format(fileNameTemplate, range.Start, range.End));
                    if (!outputPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        outputPath += ".pdf";
                    }
                    outputDocument.Save(outputPath);

                    pageIndex += range.End - range.Start + 1;
                    progress?.Report((int)((double)pageIndex / totalPages * 100));
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"拆分PDF失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public async Task<string?> SplitPdfToSinglePagesAsync(
        string inputPath,
        string outputDir,
        string fileNameTemplate,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputDocument = PdfSharp.Pdf.IO.PdfReader.Open(inputPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                var totalPages = inputDocument.PageCount;

                Directory.CreateDirectory(outputDir);
                for (int i = 1; i <= totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                    outputDocument.Info.Title = $"Page {i}";
                    outputDocument.AddPage(inputDocument.Pages[i - 1]);

                    var outputPath = Path.Combine(outputDir, string.Format(fileNameTemplate, i));
                    if (!outputPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        outputPath += ".pdf";
                    }
                    outputDocument.Save(outputPath);

                    progress?.Report((int)((double)i / totalPages * 100));
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"拆分PDF失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public async Task<string?> ExtractPagesAsync(
        string inputPath,
        string outputPath,
        IEnumerable<int> pages,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputDocument = PdfSharp.Pdf.IO.PdfReader.Open(inputPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                var pageList = pages.ToList();
                var totalPages = pageList.Count;
                var currentPage = 0;

                using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                outputDocument.Info.Title = "Extracted Pages";

                foreach (var pageNum in pageList)
                {
                    if (pageNum >= 1 && pageNum <= inputDocument.PageCount)
                    {
                        outputDocument.AddPage(inputDocument.Pages[pageNum - 1]);
                    }
                    currentPage++;
                    progress?.Report((int)((double)currentPage / totalPages * 100));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                outputDocument.Save(outputPath);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"提取页面失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public async Task<string?> CropPdfAsync(
        string inputPath,
        string outputPath,
        double leftMargin,
        double topMargin,
        double rightMargin,
        double bottomMargin,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputDocument = PdfSharp.Pdf.IO.PdfReader.Open(inputPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                var totalPages = inputDocument.PageCount;

                using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                outputDocument.Info.Title = "Cropped PDF";

                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var inputPage = inputDocument.Pages[i];
                    var outputPage = outputDocument.AddPage(inputPage);

                    var pageWidth = inputPage.Width.Point;
                    var pageHeight = inputPage.Height.Point;

                    var newLeft = leftMargin;
                    var newTop = topMargin;
                    var newRight = pageWidth - rightMargin;
                    var newBottom = pageHeight - bottomMargin;

                    if (newRight > newLeft && newBottom > newTop)
                    {
                        outputPage.MediaBox = new PdfRectangle(new XRect(newLeft, newTop, newRight - newLeft, newBottom - newTop));
                    }

                    progress?.Report((int)((double)(i + 1) / totalPages * 100));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                outputDocument.Save(outputPath);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"裁剪PDF失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public int GetPdfPageCount(string filePath)
    {
        try
        {
            using var document = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            return document.PageCount;
        }
        catch
        {
            return 0;
        }
    }

    private List<PageRange> ParsePageRange(string rangeStr, int maxPages)
    {
        var ranges = new List<PageRange>();
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
                    if (start <= end)
                    {
                        ranges.Add(new PageRange { Start = start, End = end });
                    }
                }
            }
            else if (int.TryParse(trimmed, out int pageNum))
            {
                pageNum = Math.Max(1, Math.Min(pageNum, maxPages));
                ranges.Add(new PageRange { Start = pageNum, End = pageNum });
            }
        }

        return ranges;
    }

    private struct PageRange
    {
        public int Start { get; set; }
        public int End { get; set; }
    }
}