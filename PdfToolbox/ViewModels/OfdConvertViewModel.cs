using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace PdfToolbox.ViewModels;

/// <summary>OFD 转 PDF：调用内置转换引擎（ofd-convert.exe，Java 打包）逐个转换，输出目录可自定义（默认源文件同目录）。</summary>
public partial class OfdConvertViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<OfdConvertItem> files = new();

    [ObservableProperty]
    private OfdConvertItem? selectedFile;

    [ObservableProperty]
    private string outputDir = "";

    [ObservableProperty]
    private bool isConverting;

    [ObservableProperty]
    private string logText = "";

    public bool HasOutputDir => !string.IsNullOrWhiteSpace(OutputDir);

    partial void OnOutputDirChanged(string value) => OnPropertyChanged(nameof(HasOutputDir));

    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "OFD文件 (*.ofd)|*.ofd",
            Title = "选择OFD文件",
            Multiselect = true
        };
        if (dialog.ShowDialog() == true)
            AddOfdFiles(dialog.FileNames);
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = Helpers.FolderBrowserHelper.SelectFolder("选择包含OFD文件的文件夹");
        if (string.IsNullOrEmpty(path)) return;
        AddOfdFiles(System.IO.Directory.EnumerateFiles(path, "*.ofd"));
    }

    /// <summary>批量加入 OFD 文件：只接受 .ofd（不区分大小写）、文件存在、且未在列表中（大小写不敏感）的路径。</summary>
    public void AddOfdFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;
        int added = 0, skipped = 0;
        foreach (var file in filePaths)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) { skipped++; continue; }
            if (!string.Equals(Path.GetExtension(file), ".ofd", StringComparison.OrdinalIgnoreCase)) { skipped++; continue; }
            if (Files.Any(f => string.Equals(f.FilePath, file, StringComparison.OrdinalIgnoreCase))) { skipped++; continue; }
            Files.Add(new OfdConvertItem { FilePath = file });
            added++;
        }
        if (added > 0 || skipped > 0)
        {
            var messages = new List<string>();
            if (added > 0) messages.Add($"已添加 {added} 个文件");
            if (skipped > 0) messages.Add($"跳过 {skipped} 个无效或重复文件");
            StatusMessage = string.Join("，", messages);
        }
    }

    [RelayCommand]
    private void RemoveFile(OfdConvertItem? item) { if (item != null) Files.Remove(item); }

    [RelayCommand]
    private void ClearAll()
    {
        Files.Clear();
        LogText = "";
        StatusMessage = "";
    }

    [RelayCommand]
    private void SelectOutputDir()
    {
        var dir = Helpers.FolderBrowserHelper.SelectFolder("选择输出目录");
        if (!string.IsNullOrEmpty(dir)) OutputDir = dir;
    }

    [RelayCommand]
    private void ClearOutputDir() => OutputDir = "";

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (Files.Count == 0)
        {
            MessageBox.Show("请先添加OFD文件", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            OfdConvertService.EnsureEngine();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsConverting = true;
        IsBusy = true;
        try
        {
            var outputDir = string.IsNullOrWhiteSpace(OutputDir) ? null : OutputDir.Trim();
            if (outputDir != null) AppendLog($"输出目录: {outputDir}");

            var files = Files.ToList();
            ProgressValue = 0;
            AppendLog($"=== 开始转换 {files.Count} 个文件 ===");
            int success = 0, failed = 0, index = 0;
            foreach (var item in files)
            {
                try
                {
                    var outPath = await OfdConvertService.ConvertAsync(item.FilePath, AppendLog, outputDir);
                    AppendLog($"转换成功: {outPath}");
                    success++;
                }
                catch (Exception ex)
                {
                    AppendLog($"转换失败 [{item.FileName}]: {ex.Message}");
                    failed++;
                }
                index++;
                ProgressValue = index * 100 / files.Count;
            }
            AppendLog("=== 转换完成 ===");
            StatusMessage = $"完成：成功 {success}，失败 {failed}";
        }
        finally
        {
            IsBusy = false;
            IsConverting = false;
        }
    }

    private void AppendLog(string line) => LogText += line + Environment.NewLine;
}

public sealed class OfdConvertItem
{
    public required string FilePath { get; init; }
    public string FileName => Path.GetFileName(FilePath);
}