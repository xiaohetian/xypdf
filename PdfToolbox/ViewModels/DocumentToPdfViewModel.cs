using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using System.Collections.ObjectModel;

namespace PdfToolbox.ViewModels;

public partial class DocumentToPdfViewModel : ViewModelBase
{
    private readonly DocumentService _documentService = new();

    [ObservableProperty]
    private string _inputPath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private string _pageSize = "A4";

    [ObservableProperty]
    private int _margin = 20;

    [ObservableProperty]
    private string _documentType = "TXT";

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isConverting;

    private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<string> PageSizes { get; } = new() { "A4", "A3", "Letter" };
    public ObservableCollection<string> DocumentTypes { get; } = new() { 
        "TXT", "RTF", "HTML", "Markdown (MD)", "CSV", "JSON", "XML", "LOG" 
    };

    public DocumentToPdfViewModel()
    {
        OutputPath = System.IO.Path.Combine(
            SettingsService.Instance.GetDefaultOutputPath(), "output.pdf");
    }

    partial void OnDocumentTypeChanged(string value)
    {
        UpdateOutputPath();
    }

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

        if (dialog.ShowDialog() == true)
        {
            SetInputFile(dialog.FileName);
        }
    }

    /// <summary>
    /// 设置输入文档文件（公开方法，用于拖拽支持）。
    /// 如果扩展名不属于当前 DocumentType，则尝试自动切换 DocumentType。
    /// </summary>
    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return;
        }

        // 自动适配文档类型：检查文件扩展名属于哪种类型并切换
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var matchedType = DetectDocumentTypeByExtension(ext);
        if (!string.IsNullOrEmpty(matchedType))
        {
            DocumentType = matchedType;
        }

        InputPath = filePath;
        OutputPath = System.IO.Path.ChangeExtension(InputPath, ".pdf");
    }

    private static string? DetectDocumentTypeByExtension(string extension)
    {
        return extension switch
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
    }

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
        {
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(OutputPath);
        }

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
        }
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

        IsConverting = true;
        ProgressValue = 0;
        IsBusy = true;
        StatusMessage = "正在转换...";

        _cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<int>(value => ProgressValue = value);

        try
        {
            string? result = null;

            switch (DocumentType)
            {
                case "TXT":
                    result = await _documentService.ConvertTextToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, null, progress, _cancellationTokenSource.Token);
                    break;
                case "RTF":
                    result = await _documentService.ConvertRtfToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, progress, _cancellationTokenSource.Token);
                    break;
                case "HTML":
                    result = await _documentService.ConvertHtmlToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, progress, _cancellationTokenSource.Token);
                    break;
                case "Markdown (MD)":
                case "CSV":
                case "JSON":
                case "XML":
                case "LOG":
                    // 这些类型都作为文本文件处理
                    result = await _documentService.ConvertTextToPdfAsync(
                        InputPath, OutputPath, PageSize, Margin, null, progress, _cancellationTokenSource.Token);
                    break;
            }

            if (string.IsNullOrEmpty(result))
            {
                System.Windows.MessageBox.Show("转换完成!", "完成",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                StatusMessage = "转换完成";
            }
            else
            {
                System.Windows.MessageBox.Show(result, "转换失败",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "转换失败";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "转换已取消";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"转换失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "转换失败";
        }
        finally
        {
            IsConverting = false;
            IsBusy = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelConvert()
    {
        _cancellationTokenSource?.Cancel();
    }
}