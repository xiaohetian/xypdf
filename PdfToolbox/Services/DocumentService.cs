using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace PdfToolbox.Services;

public class DocumentService
{
    public async Task<string?> ConvertTextToPdfAsync(
        string inputPath,
        string outputPath,
        string pageSize = "A4",
        int margin = 20,
        string? fontFamily = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress?.Report(10);
                var text = File.ReadAllText(inputPath, Encoding.UTF8);
                progress?.Report(30);

                var font = string.IsNullOrEmpty(fontFamily) ? "Microsoft YaHei" : fontFamily;
                var pdfPageSize = GetPageSize(pageSize);

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(pdfPageSize);
                        page.Margin(margin);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily(font));
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().Text(text);
                        });
                    });
                }).GeneratePdf(outputPath);

                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"转换失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public async Task<string?> ConvertRtfToPdfAsync(
        string inputPath,
        string outputPath,
        string pageSize = "A4",
        int margin = 20,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress?.Report(10);
                var rtfContent = File.ReadAllText(inputPath, Encoding.Unicode);
                var plainText = ParseRtfToPlainText(rtfContent);
                progress?.Report(50);

                var pdfPageSize = GetPageSize(pageSize);
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(pdfPageSize);
                        page.Margin(margin);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Microsoft YaHei"));
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().Text(plainText);
                        });
                    });
                }).GeneratePdf(outputPath);

                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"转换失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    public async Task<string?> ConvertHtmlToPdfAsync(
        string inputPath,
        string outputPath,
        string pageSize = "A4",
        int margin = 20,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                progress?.Report(10);
                var htmlContent = File.ReadAllText(inputPath, Encoding.UTF8);
                var plainText = StripHtml(htmlContent);
                progress?.Report(50);

                var pdfPageSize = GetPageSize(pageSize);
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(pdfPageSize);
                        page.Margin(margin);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Microsoft YaHei"));
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().Text(plainText);
                        });
                    });
                }).GeneratePdf(outputPath);

                progress?.Report(100);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"转换失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    private string ParseRtfToPlainText(string rtf)
    {
        try
        {
            var sb = new StringBuilder();
            int inControl = 0;
            int skipGroup = 0;

            foreach (char c in rtf)
            {
                if (c == '{')
                {
                    skipGroup++;
                    continue;
                }
                if (c == '}')
                {
                    if (skipGroup > 0) skipGroup--;
                    continue;
                }
                if (c == '\\')
                {
                    inControl = 1;
                    continue;
                }
                if (inControl > 0)
                {
                    inControl++;
                    if (inControl > 3 || c == ' ' || c == '\n' || c == '\r')
                    {
                        inControl = 0;
                    }
                    continue;
                }
                if (skipGroup <= 1 && !char.IsControl(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Trim();
        }
        catch
        {
            return rtf;
        }
    }

    private string StripHtml(string html)
    {
        try
        {
            var sb = new StringBuilder();
            bool inTag = false;

            foreach (char c in html)
            {
                if (c == '<')
                {
                    inTag = true;
                }
                else if (c == '>')
                {
                    inTag = false;
                    sb.Append(' ');
                }
                else if (!inTag)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Replace("  ", " ").Trim();
        }
        catch
        {
            return html;
        }
    }

    private PageSize GetPageSize(string size)
    {
        return size.ToUpperInvariant() switch
        {
            "A3" => PageSizes.A3,
            "LETTER" => PageSizes.Letter,
            _ => PageSizes.A4
        };
    }
}