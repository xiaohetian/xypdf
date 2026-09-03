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
    private ObservableCollection<PdfMergeItem> _pdfFiles = new();

    [ObservableProperty]
    private PdfMergeItem? _selectedFile;

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private bool _isMerging;

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
            AddPdfFiles(dialog.FileNames);
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = Helpers.FolderBrowserHelper.SelectFolder("选择包含PDF文件的文件夹");
        if (string.IsNullOrEmpty(path)) return;
        var files = System.IO.Directory.EnumerateFiles(path, "*.pdf", System.IO.SearchOption.AllDirectories);
        AddPdfFiles(files);
    }

    public void AddPdfFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;
        int added = 0, skipped = 0, failed = 0;
        foreach (var file in filePaths)
        {
            if (PdfFiles.Any(p => string.Equals(p.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            { skipped++; continue; }
            int pageCount;
            try
            {
                pageCount = _pdfService.GetPdfPageCount(file);
                if (pageCount <= 0) { failed++; continue; }
            }
            catch { failed++; continue; }
            PdfFiles.Add(new PdfMergeItem
            {
                FilePath = file,
                FileName = System.IO.Path.GetFileName(file),
                PageCount = pageCount
            });
            added++;
        }
        if (added > 0 || skipped > 0 || failed > 0)
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
    private void RemoveFile(PdfMergeItem? item) { if (item != null) PdfFiles.Remove(item); }

    [RelayCommand]
    private void ClearAll() => PdfFiles.Clear();

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index > 0) PdfFiles.Move(index, index - 1);
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index < PdfFiles.Count - 1) PdfFiles.Move(index, index + 1);
    }

    [RelayCommand]
    private void MoveToTop()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index > 0) PdfFiles.Move(index, 0);
    }

    [RelayCommand]
    private void MoveToBottom()
    {
        if (SelectedFile == null) return;
        var index = PdfFiles.IndexOf(SelectedFile);
        if (index < PdfFiles.Count - 1) PdfFiles.Move(index, PdfFiles.Count - 1);
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
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(OutputPath);
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
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
        var result = await RunOperationAsync("合并", async (ct, prog) =>
        {
            var files = PdfFiles.Select(f => f.FilePath).ToList();
            return await _pdfService.MergePdfsAsync(files, OutputPath, prog, ct);
        });
        ShowResult("合并", result);
    }
}

public partial class PdfMergeItem : ObservableObject
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private int _pageCount;
}
