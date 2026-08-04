using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using System.Collections.ObjectModel;

namespace PdfToolbox.ViewModels;

public partial class PdfMergeViewModel : ViewModelBase
{
    private readonly PdfService _pdfService = new();

    [ObservableProperty]
    private ObservableCollection<PdfFileItem> _pdfFiles = new();

    [ObservableProperty]
    private PdfFileItem? _selectedFile;

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isMerging;

    private CancellationTokenSource? _cancellationTokenSource;

    public PdfMergeViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "merged.pdf");
    }

    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择PDF文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            AddPdfFiles(dialog.FileNames);
        }
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = Helpers.FolderBrowserHelper.SelectFolder("选择包含PDF文件的文件夹");
        if (string.IsNullOrEmpty(path)) return;

        var files = System.IO.Directory.EnumerateFiles(path, "*.pdf", System.IO.SearchOption.AllDirectories);
        AddPdfFiles(files);
    }

    /// <summary>
    /// 公开方法：添加多个 PDF 文件到合并列表（用于拖拽支持）。
    /// 自动跳过已存在的文件、跳过非 PDF、跳过读取失败的损坏文件。
    /// </summary>
    public void AddPdfFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;

        int added = 0, skipped = 0, failed = 0;
        foreach (var file in filePaths)
        {
            // 跳过已存在的文件
            if (PdfFiles.Any(p => string.Equals(p.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            {
                skipped++;
                continue;
            }

            int pageCount;
            try
            {
                pageCount = _pdfService.GetPdfPageCount(file);
                if (pageCount <= 0)
                {
                    failed++;
                    continue;
                }
            }
            catch
            {
                failed++;
                continue;
            }

            PdfFiles.Add(new PdfFileItem
            {
                FilePath = file,
                FileName = System.IO.Path.GetFileName(file),
                PageCount = pageCount
            });
            added++;
        }

        // 给用户反馈（合并拖拽多次时，失败信息不至于淹没）
        if (failed > 0 || skipped > 0)
        {
            var messages = new List<string>();
            if (added > 0) messages.Add($"已添加 {added} 个文件");
            if (skipped > 0) messages.Add($"跳过 {skipped} 个重复文件");
            if (failed > 0) messages.Add($"{failed} 个文件读取失败");

            StatusMessage = string.Join("，", messages);
        }
        else if (added > 0)
        {
            StatusMessage = $"已添加 {added} 个文件";
        }
    }

    [RelayCommand]
    private void RemoveFile(PdfFileItem? item)
    {
        if (item != null)
        {
            PdfFiles.Remove(item);
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        PdfFiles.Clear();
    }

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index > 0)
        {
            PdfFiles.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index < PdfFiles.Count - 1)
        {
            PdfFiles.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private void MoveToTop()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index > 0)
        {
            PdfFiles.Move(index, 0);
        }
    }

    [RelayCommand]
    private void MoveToBottom()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index < PdfFiles.Count - 1)
        {
            PdfFiles.Move(index, PdfFiles.Count - 1);
        }
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "保存合并后的PDF",
            FileName = "merged.pdf"
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
    private async Task MergeAsync()
    {
        if (PdfFiles.Count == 0)
        {
            System.Windows.MessageBox.Show("请先添加PDF文件", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrEmpty(OutputPath))
        {
            System.Windows.MessageBox.Show("请选择输出路径", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsMerging = true;
        ProgressValue = 0;
        IsBusy = true;
        StatusMessage = "正在合并...";

        _cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<int>(value => ProgressValue = value);

        try
        {
            var files = PdfFiles.Select(f => f.FilePath).ToList();
            var result = await _pdfService.MergePdfsAsync(files, OutputPath, progress, _cancellationTokenSource.Token);

            if (string.IsNullOrEmpty(result))
            {
                System.Windows.MessageBox.Show("合并成功!", "完成",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                StatusMessage = "合并完成";
            }
            else
            {
                System.Windows.MessageBox.Show(result, "合并失败",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "合并失败";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "合并已取消";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"合并失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "合并失败";
        }
        finally
        {
            IsMerging = false;
            IsBusy = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelMerge()
    {
        _cancellationTokenSource?.Cancel();
    }
}

public partial class PdfFileItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private int _pageCount;
}