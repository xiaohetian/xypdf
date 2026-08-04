using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PdfToolbox.Services;

public class PdfRenderService : IDisposable
{
    private Windows.Data.Pdf.PdfDocument? _document;
    private string? _currentFilePath;
    private bool _disposed;

    public int PageCount => (int)(_document?.PageCount ?? 0);
    public string? CurrentFilePath => _currentFilePath;

    public bool LoadDocument(string filePath, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            CloseDocument();

            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                errorMessage = "文件不存在";
                System.Diagnostics.Debug.WriteLine($"文件不存在: {filePath}");
                return false;
            }

            // 使用 Windows.Data.Pdf 加载 PDF
            var file = StorageFile.GetFileFromPathAsync(filePath).AsTask().GetAwaiter().GetResult();
            _document = PdfDocument.LoadFromFileAsync(file).AsTask().GetAwaiter().GetResult();

            // 检查是否有页面
            if (_document.PageCount == 0)
            {
                _document = null;
                errorMessage = $"PDF 文件 [{filePath}] 没有可读页面。";
                return false;
            }

            _currentFilePath = filePath;
            return PageCount > 0;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"加载PDF失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            _document = null;
            _currentFilePath = null;
            return false;
        }
    }

    public void CloseDocument()
    {
        _document = null;
        _currentFilePath = null;
    }

    public BitmapSource? RenderPage(int pageIndex, double scale, int dpi = 96)
    {
        if (_document == null || pageIndex < 0 || pageIndex >= PageCount)
            return null;

        try
        {
            var page = _document.GetPage((uint)pageIndex);
            // page.Size 单位为 DIPs (1 DIP = 1/96 英寸)
            var size = page.Size;
            var width = (int)(size.Width / 96.0 * dpi * scale);
            var height = (int)(size.Height / 96.0 * dpi * scale);

            if (width < 1) width = 1;
            if (height < 1) height = 1;

            var options = new PdfPageRenderOptions
            {
                DestinationWidth = (uint)width,
                DestinationHeight = (uint)height
            };

            var stream = new InMemoryRandomAccessStream();
            page.RenderToStreamAsync(stream, options).AsTask().GetAwaiter().GetResult();
            stream.Seek(0);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream.AsStream();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"渲染页面失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 渲染指定页面为 System.Drawing.Bitmap（用于保存到文件）
    /// </summary>
    public System.Drawing.Bitmap? RenderPageAsBitmap(int pageIndex, int dpi = 150)
    {
        if (_document == null || pageIndex < 0 || pageIndex >= PageCount)
            return null;

        try
        {
            var page = _document.GetPage((uint)pageIndex);
            var size = page.Size; // DIPs (1 DIP = 1/96 英寸)
            var width = (int)(size.Width / 96.0 * dpi);
            var height = (int)(size.Height / 96.0 * dpi);

            if (width < 1) width = 1;
            if (height < 1) height = 1;

            var options = new PdfPageRenderOptions
            {
                DestinationWidth = (uint)width,
                DestinationHeight = (uint)height
            };

            var stream = new InMemoryRandomAccessStream();
            page.RenderToStreamAsync(stream, options).AsTask().GetAwaiter().GetResult();
            stream.Seek(0);

            return new System.Drawing.Bitmap(stream.AsStream());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"渲染页面失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取指定页面的实际尺寸（点单位，1点=1/72英寸）
    /// </summary>
    public System.Drawing.SizeF GetPageSize(int pageIndex)
    {
        if (_document == null || pageIndex < 0 || pageIndex >= PageCount)
            return System.Drawing.SizeF.Empty;

        var page = _document.GetPage((uint)pageIndex);
        // page.Size 单位为 DIPs (1 DIP = 1/96 英寸)
        // 转换为点 (1 点 = 1/72 英寸)
        var size = page.Size;
        return new System.Drawing.SizeF(
            (float)(size.Width * 72.0 / 96.0),
            (float)(size.Height * 72.0 / 96.0));
    }

    public void Dispose()
    {
        if (_disposed) return;
        CloseDocument();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
