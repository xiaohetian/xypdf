using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfToolbox.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string _statusMessage = "";

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public virtual void OnActivated() { }
    public virtual void OnDeactivated() { }

    /// <summary>
    /// 通用异步操作执行器：统一处理 IsBusy / IsProcessing、进度回调、取消、错误弹窗。
    /// 子类覆写 OnProcessingChanged 来自定义 IsProcessing 状态（若需要）。
    /// </summary>
    protected async Task<string?> RunOperationAsync(
        string operationName,
        System.Func<System.Threading.CancellationToken, System.IProgress<int>, System.Threading.Tasks.Task<string?>> operation,
        bool setProcessing = true)
    {
        IsBusy = true;
        ProgressValue = 0;
        StatusMessage = $"正在{operationName}...";

        using var cts = new System.Threading.CancellationTokenSource();
        var progress = new Progress<int>(value => ProgressValue = value);

        string? result = null;
        try
        {
            result = await operation(cts.Token, progress);
        }
        catch (System.OperationCanceledException)
        {
            StatusMessage = $"{operationName}已取消";
        }
        catch (System.Exception ex)
        {
            result = $"操作失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            if (setProcessing) IsProcessing = false;
        }
        return result;
    }

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isProcessing;

    protected void ShowResult(string operationName, string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            System.Windows.MessageBox.Show($"{operationName}成功!", "完成",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            StatusMessage = $"{operationName}完成";
        }
        else
        {
            System.Windows.MessageBox.Show(result, $"{operationName}失败",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = $"{operationName}失败";
        }
    }
}
