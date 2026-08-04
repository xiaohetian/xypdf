using System.Windows;
using System.Windows.Controls;
using PdfToolbox.Helpers;
using PdfToolbox.ViewModels;

namespace PdfToolbox.Views;

/// <summary>
/// PdfSecurityView 的拖拽支持：拖入 PDF 文件即设置为加密/解密输入。
/// 加密/解密功能只接受单个文件，拖入多个时取第一个并提示用户。
/// </summary>
public partial class PdfSecurityView : UserControl
{
    public PdfSecurityView()
    {
        InitializeComponent();
        DataContext = new PdfSecurityViewModel();
    }

    private void RootGrid_DragEnter(object sender, DragEventArgs e)
    {
        UpdateDragEffect(e);
    }

    private void RootGrid_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragEffect(e);
    }

    private void RootGrid_DragLeave(object sender, DragEventArgs e)
    {
    }

    private void RootGrid_Drop(object sender, DragEventArgs e)
    {
        if (!DragDropHelper.TryGetFiles(e, out var files))
        {
            return;
        }

        var validFiles = DragDropHelper.FilterByExtension(files, DragDropHelper.PdfExtensions);
        if (validFiles.Length == 0)
        {
            MessageBox.Show("请拖入 PDF 文件", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
            return;
        }

        if (validFiles.Length > 1)
        {
            MessageBox.Show($"加密/解密功能只接受单个文件，已使用第一个文件：\n{validFiles[0]}",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        if (DataContext is PdfSecurityViewModel vm)
        {
            vm.SetInputFile(validFiles[0]);
        }

        e.Handled = true;
    }

    private void UpdateDragEffect(DragEventArgs e)
    {
        if (!DragDropHelper.TryGetFiles(e, out var files))
        {
            DragDropHelper.SetDragEffect(e, false);
            return;
        }

        var validFiles = DragDropHelper.FilterByExtension(files, DragDropHelper.PdfExtensions);
        DragDropHelper.SetDragEffect(e, validFiles.Length > 0);
    }
}
