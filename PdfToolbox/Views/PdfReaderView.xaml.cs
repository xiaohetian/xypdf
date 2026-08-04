using System.Windows;
using System.Windows.Controls;
using PdfToolbox.Helpers;
using PdfToolbox.ViewModels;

namespace PdfToolbox.Views;

/// <summary>
/// PdfReaderView 的拖拽支持：将 PDF 文件拖入即打开为新标签页。
/// 支持一次拖入多个 PDF 文件（每个文件打开一个标签页）。
/// </summary>
public partial class PdfReaderView : UserControl
{
    public PdfReaderView()
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
        // 不需要做任何处理，离开时 WPF 会自动恢复
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

        if (DataContext is PdfReaderViewModel vm)
        {
            vm.AddPdfs(validFiles);
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
