using System.Windows;
using System.Windows.Controls;
using PdfToolbox.Helpers;
using PdfToolbox.ViewModels;

namespace PdfToolbox.Views;

/// <summary>
/// DocumentToPdfView 的拖拽支持：拖入文档文件即设置为输入。
/// 支持 TXT/RTF/HTML/MD/CSV/JSON/XML/LOG 等格式。
/// 拖入时若文件类型与当前 DocumentType 不匹配，会自动切换类型。
/// </summary>
public partial class DocumentToPdfView : UserControl
{
    public DocumentToPdfView()
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

        var validFiles = DragDropHelper.FilterByExtension(files, DragDropHelper.AllDocumentExtensions);
        if (validFiles.Length == 0)
        {
            MessageBox.Show("请拖入文档文件（txt / rtf / html / md / csv / json / xml / log）", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
            return;
        }

        if (validFiles.Length > 1)
        {
            MessageBox.Show($"文档转 PDF 只接受单个文件，已使用第一个文件：\n{validFiles[0]}",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        if (DataContext is DocumentToPdfViewModel vm)
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

        var validFiles = DragDropHelper.FilterByExtension(files, DragDropHelper.AllDocumentExtensions);
        DragDropHelper.SetDragEffect(e, validFiles.Length > 0);
    }
}