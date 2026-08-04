using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using PdfToolbox.Helpers;
using System.Collections.ObjectModel;
using System.IO;

namespace PdfToolbox.ViewModels;

public partial class PdfSecurityViewModel : ViewModelBase
{
    private readonly PdfToolService _pdfToolService = new();

    [ObservableProperty] private int _activeTab;
    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isProcessing;

    // Encrypt
    [ObservableProperty] private string _userPassword = "";
    [ObservableProperty] private string _ownerPassword = "";
    [ObservableProperty] private bool _allowPrinting = true;
    [ObservableProperty] private bool _allowCopy = true;
    [ObservableProperty] private bool _allowModify = true;

    // Decrypt
    [ObservableProperty] private string _decryptPassword = "";

    public PdfSecurityViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "secured.pdf");
    }

    [RelayCommand]
    private void SelectInputFile()
    {
        var dialog = new OpenFileDialog { Filter = "PDF文件 (*.pdf)|*.pdf", Title = "选择PDF文件" };
        if (dialog.ShowDialog() == true)
        {
            SetInputFile(dialog.FileName);
        }
    }

    /// <summary>
    /// 设置输入 PDF 文件（公开方法，用于拖拽支持）。
    /// </summary>
    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return;
        }

        InputPath = filePath;
        try
        {
            TotalPages = _pdfToolService.GetPdfPageCount(InputPath);
        }
        catch
        {
            TotalPages = 0;
        }
        OutputPath = Path.Combine(
            Path.GetDirectoryName(InputPath) ?? "",
            Path.GetFileNameWithoutExtension(InputPath) + (ActiveTab == 0 ? "_加密" : "_解密") + ".pdf");
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog { Filter = "PDF文件 (*.pdf)|*.pdf", Title = "保存PDF", FileName = Path.GetFileName(OutputPath) };
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ProcessAsync()
    {
        if (string.IsNullOrEmpty(InputPath))
        {
            System.Windows.MessageBox.Show("请选择输入文件", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrEmpty(OutputPath))
        {
            System.Windows.MessageBox.Show("请选择输出路径", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsProcessing = true; ProgressValue = 0; IsBusy = true;

        using var cts = new CancellationTokenSource();
        var progress = new Progress<int>(v => ProgressValue = v);

        try
        {
            string? result = null;
            if (ActiveTab == 0)
            {
                if (string.IsNullOrWhiteSpace(UserPassword))
                {
                    System.Windows.MessageBox.Show("请输入用户密码", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    IsProcessing = false; IsBusy = false; return;
                }
                StatusMessage = "正在加密...";
                result = await _pdfToolService.EncryptPdfAsync(InputPath, OutputPath, UserPassword, OwnerPassword, AllowPrinting, AllowCopy, AllowModify, progress, cts.Token);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(DecryptPassword))
                {
                    System.Windows.MessageBox.Show("请输入密码", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    IsProcessing = false; IsBusy = false; return;
                }
                StatusMessage = "正在解密...";
                result = await _pdfToolService.DecryptPdfAsync(InputPath, OutputPath, DecryptPassword, progress, cts.Token);
            }

            if (string.IsNullOrEmpty(result))
            {
                System.Windows.MessageBox.Show(ActiveTab == 0 ? "加密成功!" : "解密成功!", "完成", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                StatusMessage = "处理完成";
            }
            else
            {
                System.Windows.MessageBox.Show(result, "失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "处理失败";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"处理失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally { IsProcessing = false; IsBusy = false; }
    }
}
