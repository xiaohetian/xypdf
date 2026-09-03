using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using System.Collections.ObjectModel;

namespace PdfToolbox.ViewModels;

/// <summary>
/// 文档转 PDF 视图模型。
/// 注意：仅 TXT / RTF / HTML 能保留基本格式；
/// Markdown / CSV / JSON / XML / LOG 以纯文本方式转换，无格式渲染。
/// </summary>
public partial class DocumentToPdfViewModel : ViewModelBase
{
    private readonly DocumentService _documentService = new();

    // 支持格式渲染的文档类型
    private static readonly string[] FormatCapableTypes = { "TXT", "RTF", "HTML" };
    // 仅纯文本转换的类型（会弹出提示）
    private static readonly string[] PlainTextOnlyTypes = { "Markdown (MD)", "CSV", "JSON", "XML", "LOG" };

    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private string _pageSize = "A4";
    [ObservableProperty] private int _margin = 20;
    [ObservableProperty] private string _documentType = "TXT";
    [ObservableProperty] private bool _isConverting;

    public ObservableCollection<string> PageSizes { get; } = new() { "A4", "A3", "Letter" };
    public ObservableCollection<string> DocumentTypes { get; } = new()
    {
        "TXT", "RTF", "HTML",
        "Markdown (MD)", "CSV", "JSON", "XML", "LOG"
    };

    public DocumentToPdfViewModel()
    {
        OutputPath = System.IO.Path.Combine(
            SettingsService.Instance.GetDefaultOutputPath(), "output.pdf");
    }

    partial void OnDocumentTypeChanged(string value) => UpdateOutputPath();

    private void UpdateOutputPath()
    {
        if (!string.IsNullOrEmpty(InputPath))
        {
            var dir = System.IO.Path.GetDirectoryName(OutputPath) ?? "";
            var name = System.IO.Path.GetFileNameWithoutExtension(InputPath);
            OutputPath = System.IO.Path.Combine(dir, name + ".pdf");
        }
    }

    [RelayCommand]
    private void SelectInputFile()
    {
        var filter = DocumentType switch
        {
            "TXT" => "文本文件 (*.txt)|*.txt",
            "RTF" => "RTF文件 (*.rtf)|*.rtf",
            "HTML" => "HTML文件 (*.html;*.htm)|*.html;*.htm",
            "Markdown (MD)" => "Markdown文件 (*.md;*.markdown)|*.md;*.markdown",
            "CSV" => "CSV文件 (*.csv)|*.csv",
            "JSON" => "JSON文件 (*.json)|*.json",
            "XML" => "XML文件 (*.xml)|*.xml",
            "LOG" => "日志文件 (*.log)|*.log",
            _ => "文本文件 (*.txt)|*.txt"
        };

        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = "选择文档文件"
        };
        if (dialog.ShowDialog() == true) SetInputFile(dialog.FileName);
    }

    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var matchedType = DetectDocumentTypeByExtension(ext);
        if (!string.IsNullOrEmpty(matchedType)) DocumentType = matchedType;
        InputPath = filePath;
        OutputPath = System.IO.Path.ChangeExtension(InputPath, ".pdf");
    }

    private static string? DetectDocumentTypeByExtension(string extension) =>
        extension switch
        {
            ".txt" => "TXT",
            ".rtf" => "RTF",
            ".html" or ".htm" => "HTML",
            ".md" or ".markdown" => "Markdown (MD)",
            ".csv" => "CSV",
            ".json" => "JSON",
            ".xml" => "XML",
            ".log" => "LOG",
            _ => null
        };

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "保存PDF文件",
            FileName = System.IO.Path.GetFileName(OutputPath)
        };
        if (!string.IsNullOrEmpty(OutputPath))
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(OutputPath);
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (string.IsNullOrEmpty(InputPath))
        {
            System.Windows.MessageBox.Show("请选择输入文件", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrEmpty(OutputPath))
        {
            System.Windows.MessageBox.Show("请选择输出路径", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        // 提示纯文本格式的特性差异
        if (PlainTextOnlyTypes.Contains(DocumentType))
        {
            System.Windows.MessageBox.Show(
                $"【{DocumentType}】当前仅支持纯文本转换，表格、样式等格式信息将丢失。",
                "格式说明", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        IsConverting = true;
        var result = await RunOperationAsync("转换", async (ct, prog) =>
        {
            switch (DocumentType)
            {
                case "TXT":
                    return await _documentService.ConvertTextToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, null, prog, ct);
                case "RTF":
                    return await _documentService.ConvertRtfToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, prog, ct);
                case "HTML":
                    return await _documentService.ConvertHtmlToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, prog, ct);
                default: // Markdown/CSV/JSON/XML/LOG 均走纯文本路径
                    return await _documentService.ConvertTextToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, null, prog, ct);
            }
        });
        ShowResult("转换", result);
    }

    [RelayCommand]
    private void CancelConvert() { }
}
