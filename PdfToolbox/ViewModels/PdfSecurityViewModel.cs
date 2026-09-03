using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using PdfToolbox.Helpers;

namespace PdfToolbox.ViewModels;

public partial class PdfSecurityViewModel : ViewModelBase
{
    private readonly PdfToolService _pdfToolService = new();

    [ObservableProperty] private int _activeTab;
    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private int _totalPages;
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
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择PDF文件"
        };
        if (dialog.ShowDialog() == true) SetInputFile(dialog.FileName);
    }

    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
        InputPath = filePath;
        try { TotalPages = new PdfToolService().GetPdfPageCount(filePath); }
        catch { TotalPages = 0; }
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
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(OutputPath);
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ProcessAsync()
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

        IsProcessing = true;
        string? result = null;

        try
        {
            using var cts = new System.Threading.CancellationTokenSource();
            var progress = new Progress<int>(v => ProgressValue = v);

            if (ActiveTab == 0)
            {
                // 加密
                if (string.IsNullOrWhiteSpace(UserPassword))
                {
                    System.Windows.MessageBox.Show("请输入用户密码", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    IsProcessing = false; return;
                }
                result = await _pdfToolService.EncryptPdfAsync(
                    InputPath, OutputPath, UserPassword, OwnerPassword,
                    AllowPrinting, AllowCopy, AllowModify, progress, cts.Token);
                ShowResult("加密", result);
            }
            else
            {
                // 解密
                if (string.IsNullOrWhiteSpace(DecryptPassword))
                {
                    System.Windows.MessageBox.Show("请输入密码", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    IsProcessing = false; return;
                }
                result = await _pdfToolService.DecryptPdfAsync(
                    InputPath, OutputPath, DecryptPassword, progress, cts.Token);
                ShowResult("解密", result);
            }
        }
        catch (System.Exception ex)
        {
            IsProcessing = false;
            System.Windows.MessageBox.Show($"处理失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "处理失败";
        }
        finally { IsBusy = false; }
    }
}
