using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using System.Collections.ObjectModel;

namespace PdfToolbox.ViewModels;

public partial class ImageToPdfViewModel : ViewModelBase
{
    private readonly ImageService _imageService = new();

    [ObservableProperty]
    private ObservableCollection<ImageFileItem> _imageFiles = new();

    [ObservableProperty]
    private ImageFileItem? _selectedFile;

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private string _pageSize = "A4";

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isConverting;

    private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<string> PageSizes { get; } = new()
    {
        "A4", "A3", "Letter"
    };

    public ImageToPdfViewModel()
    {
        OutputPath = System.IO.Path.Combine(
            SettingsService.Instance.GetDefaultOutputPath(), "output.pdf");
    }

    [RelayCommand]
    private void AddImages()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif",
            Title = "选择图片文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            AddImageFiles(dialog.FileNames);
        }
    }

    /// <summary>
    /// 公开方法：添加多个图片文件到转换列表（用于拖拽支持）。
    /// 自动跳过非图片文件、跳过已存在的文件。
    /// </summary>
    public void AddImageFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;

        int added = 0, skipped = 0, invalid = 0;
        foreach (var file in filePaths)
        {
            if (!_imageService.IsImageFile(file))
            {
                invalid++;
                continue;
            }

            if (ImageFiles.Any(p => string.Equals(p.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            {
                skipped++;
                continue;
            }

            ImageFiles.Add(new ImageFileItem
            {
                FilePath = file,
                FileName = System.IO.Path.GetFileName(file)
            });
            added++;
        }

        if (added > 0 || skipped > 0 || invalid > 0)
        {
            var messages = new List<string>();
            if (added > 0) messages.Add($"已添加 {added} 张图片");
            if (skipped > 0) messages.Add($"跳过 {skipped} 张重复");
            if (invalid > 0) messages.Add($"{invalid} 个非图片文件");
            StatusMessage = string.Join("，", messages);
        }
    }

    [RelayCommand]
    private void RemoveImage(ImageFileItem? item)
    {
        if (item != null)
        {
            ImageFiles.Remove(item);
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        ImageFiles.Clear();
    }

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedFile == null) return;
        var index = ImageFiles.IndexOf(SelectedFile);
        if (index > 0)
        {
            ImageFiles.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedFile == null) return;
        var index = ImageFiles.IndexOf(SelectedFile);
        if (index < ImageFiles.Count - 1)
        {
            ImageFiles.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "保存PDF文件",
            FileName = "images.pdf"
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
        if (ImageFiles.Count == 0)
        {
            System.Windows.MessageBox.Show("请先添加图片文件", "提示",
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
            var imagePaths = ImageFiles.Select(f => f.FilePath).ToList();
            var result = await _imageService.ImagesToPdfAsync(imagePaths, OutputPath, PageSize, progress, _cancellationTokenSource.Token);

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

public partial class ImageFileItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _fileName = "";
}