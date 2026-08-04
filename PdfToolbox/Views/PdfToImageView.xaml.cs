using System.Windows;
using System.Windows.Controls;
using PdfToolbox.Helpers;
using PdfToolbox.ViewModels;

namespace PdfToolbox.Views;

/// <summary>
/// PdfToImageView 的拖拽支持：将 PDF 文件拖入即添加到转换列表。
/// 支持一次拖入多个 PDF，也支持多次拖入（重复的会自动跳过）。
/// </summary>
public partial class PdfToImageView : UserControl
{
    public PdfToImageView()
    {
        InitializeComponent();
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

        if (DataContext is PdfToImageViewModel vm)
        {
            vm.AddPdfFiles(validFiles);
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
