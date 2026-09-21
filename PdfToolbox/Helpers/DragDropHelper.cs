using System.IO;
using System.Linq;
using System.Windows;

namespace PdfToolbox.Helpers;

/// <summary>
/// 拖拽文件辅助类：集中处理文件提取、扩展名校验、可视化反馈。
/// 所有功能模块的拖拽逻辑都通过此类进行文件类型校验，确保一致性。
/// </summary>
public static class DragDropHelper
{
    // ============== 常用扩展名常量 ==============
    public static readonly string[] PdfExtensions = { ".pdf" };

    public static readonly string[] OfdExtensions = { ".ofd" };

    public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif" };

    // 各文档类型对应的扩展名
    public static readonly string[] TextExtensions = { ".txt" };
    public static readonly string[] RtfExtensions = { ".rtf" };
    public static readonly string[] HtmlExtensions = { ".html", ".htm" };
    public static readonly string[] MarkdownExtensions = { ".md", ".markdown" };
    public static readonly string[] CsvExtensions = { ".csv" };
    public static readonly string[] JsonExtensions = { ".json" };
    public static readonly string[] XmlExtensions = { ".xml" };
    public static readonly string[] LogExtensions = { ".log" };

    // 所有受支持的文档扩展名（用于 DocumentToPdf 的拖拽验证，按当前选中的 DocumentType 过滤）
    public static readonly string[] AllDocumentExtensions = TextExtensions
        .Concat(RtfExtensions)
        .Concat(HtmlExtensions)
        .Concat(MarkdownExtensions)
        .Concat(CsvExtensions)
        .Concat(JsonExtensions)
        .Concat(XmlExtensions)
        .Concat(LogExtensions)
        .ToArray();

    /// <summary>
    /// 检查拖拽事件的数据中是否包含文件。
    /// </summary>
    public static bool HasFiles(DragEventArgs e)
    {
        return e.Data.GetDataPresent(DataFormats.FileDrop);
    }

    /// <summary>
    /// 从拖拽事件中获取所有文件路径。
    /// </summary>
    public static bool TryGetFiles(DragEventArgs e, out string[] files)
    {
        files = Array.Empty<string>();
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] dropped)
        {
            // 只保留真实存在的文件（跳过目录）
            files = dropped.Where(File.Exists).ToArray();
            return files.Length > 0;
        }

        return false;
    }

    /// <summary>
    /// 校验文件扩展名是否在允许列表中。
    /// </summary>
    public static string[] FilterByExtension(string[] files, string[] allowedExtensions)
    {
        if (files == null || files.Length == 0)
        {
            return Array.Empty<string>();
        }

        var allowed = new HashSet<string>(allowedExtensions, StringComparer.OrdinalIgnoreCase);
        return files.Where(f => allowed.Contains(Path.GetExtension(f))).ToArray();
    }

    /// <summary>
    /// 根据 DocumentType 名称（与 DocumentToPdfViewModel.DocumentTypes 一致）获取对应的扩展名集合。
    /// </summary>
    public static string[] GetExtensionsForDocumentType(string documentType)
    {
        return documentType switch
        {
            "TXT" => TextExtensions,
            "RTF" => RtfExtensions,
            "HTML" => HtmlExtensions,
            "Markdown (MD)" => MarkdownExtensions,
            "CSV" => CsvExtensions,
            "JSON" => JsonExtensions,
            "XML" => XmlExtensions,
            "LOG" => LogExtensions,
            _ => TextExtensions
        };
    }

    /// <summary>
    /// 设置 DragEventArgs 的拖拽效果（用于 DragEnter/DragOver）。
    /// </summary>
    public static void SetDragEffect(DragEventArgs e, bool hasValidFiles)
    {
        e.Effects = hasValidFiles ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
}
