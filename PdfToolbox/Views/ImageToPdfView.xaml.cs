using System.Windows;
using System.Windows.Controls;
using PdfToolbox.Helpers;
using PdfToolbox.ViewModels;

namespace PdfToolbox.Views;

/// <summary>
/// ImageToPdfView 的拖拽支持：将图片文件拖入即添加到转换列表。
/// 支持一次拖入多个图片，也支持多次拖入（重复的会自动跳过）。
/// </summary>
public partial class ImageToPdfView : UserControl
{
    public ImageToPdfView()
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

        var validFiles = DragDropHelper.FilterByExtension(files, DragDropHelper.ImageExtensions);
        if (validFiles.Length == 0)
        {
            MessageBox.Show("请拖入图片文件（jpg / png / bmp / gif / tiff）", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
            return;
        }

        if (DataContext is ImageToPdfViewModel vm)
        {
            vm.AddImageFiles(validFiles);
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

        var validFiles = DragDropHelper.FilterByExtension(files, DragDropHelper.ImageExtensions);
        DragDropHelper.SetDragEffect(e, validFiles.Length > 0);
    }
}