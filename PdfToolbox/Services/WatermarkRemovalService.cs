using System.Runtime.InteropServices;
using System.IO;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PdfToolbox.Services;

/// <summary>
/// 基于图像处理的 PDF 水印去除服务。
/// 当 pymupdf 矢量移除失败时作为后备方案使用。
/// </summary>
public class WatermarkRemovalService
{
    private const int RenderDpi = 300;

    private static byte[] _scratchPixels = Array.Empty<byte>();
    private static float[] _scratchAlphas = Array.Empty<float>();
    private static float[] _scratchDilated = Array.Empty<float>();

    public async Task<string?> RemoveWatermarkAsync(
        string inputPath, string outputPath,
        System.Drawing.Color removeColor, int tolerance = 100, double strength = 1.0,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var totalPages = 0;
                var pageImageBytes = new List<byte[]>();

                var file = await StorageFile.GetFileFromPathAsync(inputPath);
                var pdfDocument = await PdfDocument.LoadFromFileAsync(file);
                try
                {
                    totalPages = (int)pdfDocument.PageCount;
                    for (int i = 0; i < totalPages; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var page = pdfDocument.GetPage((uint)i);
                        var size = page.Size;
                        var width = (int)(size.Width / 96.0 * RenderDpi);
                        var height = (int)(size.Height / 96.0 * RenderDpi);

                        var options = new PdfPageRenderOptions
                        {
                            DestinationWidth = (uint)width,
                            DestinationHeight = (uint)height
                        };

                        var renderStream = new InMemoryRandomAccessStream();
                        await page.RenderToStreamAsync(renderStream, options);
                        renderStream.Seek(0);

                        using var rawBitmap = new System.Drawing.Bitmap(renderStream.AsStream());
                        using var processed = ProcessPageStatic(rawBitmap, removeColor, tolerance, strength);
                        processed.SetResolution(RenderDpi, RenderDpi);
                        using var ms = new MemoryStream();
                        processed.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        pageImageBytes.Add(ms.ToArray());

                        progress?.Report((int)((double)(i + 1) / totalPages * 50));
                    }
                }
                finally { pdfDocument = null; }

                QuestPDF.Settings.License = LicenseType.Community;
                var doc = Document.Create(container =>
                {
                    for (int i = 0; i < pageImageBytes.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using var bmp = new System.Drawing.Bitmap(new MemoryStream(pageImageBytes[i]));
                        var ptW = bmp.Width / (double)RenderDpi * 72.0;
                        var ptH = bmp.Height / (double)RenderDpi * 72.0;
                        var idx = i;
                        container.Page(page =>
                        {
                            page.Size((float)ptW, (float)ptH, Unit.Point);
                            page.Margin(0);
                            page.Content().Image(pageImageBytes[idx]).FitWidth();
                            progress?.Report(50 + (int)((double)(idx + 1) / totalPages * 50));
                        });
                    }
                });

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                doc.GeneratePdf(outputPath);
                return null;
            }
            catch (OperationCanceledException) { return "操作已取消"; }
            catch (Exception ex) { return $"去除水印失败: {ex.Message}"; }
        }, cancellationToken);
    }

    public static System.Drawing.Bitmap ProcessPageStatic(
        System.Drawing.Bitmap source,
        System.Drawing.Color targetColor,
        int tolerance,
        double strength)
    {
        return ProcessPage(source, targetColor, tolerance, strength);
    }

    private static System.Drawing.Bitmap ProcessPage(
        System.Drawing.Bitmap source,
        System.Drawing.Color targetColor,
        int tolerance,
        double strength)
    {
        int width = source.Width;
        int height = source.Height;
        int stride = width * 4;
        int bufferSize = stride * height;
        int pixelCount = width * height;

        EnsureBuffers(bufferSize, pixelCount);

        var srcData = source.LockBits(
            new System.Drawing.Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        int srcStride = srcData.Stride;
        Marshal.Copy(srcData.Scan0, _scratchPixels, 0, Math.Min(bufferSize, srcStride * height));
        source.UnlockBits(srcData);

        var background = EstimateBackground(_scratchPixels, width, height, srcStride);
        BuildAlphaMask(_scratchPixels, _scratchAlphas, width, height, srcStride,
            targetColor, background, tolerance, 0.05f);
        FilterIsolatedDetectionsInPlace(_scratchAlphas, width, height, minNeighborAlpha: 0.05f);
        DilateAlphaMaskInPlace(_scratchAlphas, _scratchDilated, width, height, iterations: 2);
        ReconstructBackground(_scratchPixels, _scratchAlphas, width, height, srcStride, background, strength);

        var result = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var resultData = result.LockBits(
            new System.Drawing.Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        int resultStride = resultData.Stride;
        int rowBytes = Math.Min(srcStride, resultStride);
        for (int y = 0; y < height; y++)
            Marshal.Copy(_scratchPixels, y * srcStride, resultData.Scan0 + y * resultStride, rowBytes);
        result.UnlockBits(resultData);
        return result;
    }

    private static void EnsureBuffers(int pixelBufferSize, int alphaCount)
    {
        if (_scratchPixels.Length < pixelBufferSize)
            _scratchPixels = new byte[pixelBufferSize];
        if (_scratchAlphas.Length < alphaCount)
            _scratchAlphas = new float[alphaCount];
        if (_scratchDilated.Length < alphaCount)
            _scratchDilated = new float[alphaCount];
    }

    private static void DilateAlphaMaskInPlace(float[] alphas, float[] dilated, int width, int height, int iterations = 2)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            Array.Copy(alphas, dilated, alphas.Length);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (dilated[idx] > 0.01f)
                    {
                        int yMin = y > 0 ? y - 1 : 0;
                        int yMax = y < height - 1 ? y + 1 : y;
                        int xMin = x > 0 ? x - 1 : 0;
                        int xMax = x < width - 1 ? x + 1 : x;
                        for (int ny = yMin; ny <= yMax; ny++)
                        {
                            int nRowStart = ny * width;
                            for (int nx = xMin; nx <= xMax; nx++)
                            {
                                int nIdx = nRowStart + nx;
                                if (alphas[nIdx] < dilated[idx])
                                    dilated[nIdx] = dilated[idx];
                            }
                        }
                    }
                }
            }
            Array.Copy(dilated, alphas, alphas.Length);
        }
    }

    private static void FilterIsolatedDetectionsInPlace(float[] alphas, int width, int height, float minNeighborAlpha)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                if (alphas[idx] <= 0.005f) continue;

                bool hasNeighbor = false;
                int yMin = y > 0 ? y - 1 : 0;
                int yMax = y < height - 1 ? y + 1 : y;
                int xMin = x > 0 ? x - 1 : 0;
                int xMax = x < width - 1 ? x + 1 : x;
                void CheckNeighbors()
                {
                    for (int ny = yMin; ny <= yMax; ny++)
                    {
                        int nRowStart = ny * width;
                        for (int nx = xMin; nx <= xMax; nx++)
                        {
                            if (nx == x && ny == y) continue;
                            if (alphas[nRowStart + nx] > minNeighborAlpha) { hasNeighbor = true; return; }
                        }
                    }
                }
                CheckNeighbors();
                if (!hasNeighbor) alphas[idx] = 0;
            }
        }
    }

    private static System.Drawing.Color EstimateBackground(byte[] pixels, int width, int height, int stride)
    {
        long sumR = 0, sumG = 0, sumB = 0;
        int count = 0;
        int step = Math.Max(1, Math.Min(width, height) / 100);

        for (int x = 0; x < width; x += step)
        {
            int idx = x * 4;
            sumB += pixels[idx]; sumG += pixels[idx + 1]; sumR += pixels[idx + 2]; count++;
            idx = (height - 1) * stride + x * 4;
            sumB += pixels[idx]; sumG += pixels[idx + 1]; sumR += pixels[idx + 2]; count++;
        }
        for (int y = 0; y < height; y += step)
        {
            int idx = y * stride;
            sumB += pixels[idx]; sumG += pixels[idx + 1]; sumR += pixels[idx + 2]; count++;
            idx = y * stride + (width - 1) * 4;
            sumB += pixels[idx]; sumG += pixels[idx + 1]; sumR += pixels[idx + 2]; count++;
        }

        byte avgR = (byte)(sumR / count);
        byte avgG = (byte)(sumG / count);
        byte avgB = (byte)(sumB / count);
        return (avgR >= 240 && avgG >= 240 && avgB >= 240)
            ? System.Drawing.Color.White
            : System.Drawing.Color.FromArgb(avgR, avgG, avgB);
    }

    private static void BuildAlphaMask(
        byte[] pixels, float[] alphas, int width, int height, int stride,
        System.Drawing.Color targetColor, System.Drawing.Color background,
        int fitTolerance, float minAlpha)
    {
        byte tr = targetColor.R, tg = targetColor.G, tb = targetColor.B;
        byte br = background.R, bg_g = background.G, bb = background.B;
        int denomR = br - tr, denomG = bg_g - tg, denomB = bb - tb;
        bool canR = denomR != 0, canG = denomG != 0, canB = denomB != 0;
        int validChannels = (canR ? 1 : 0) + (canG ? 1 : 0) + (canB ? 1 : 0);
        if (validChannels == 0) return;
        float fitToleranceSq = fitTolerance * fitTolerance;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * stride;
            for (int x = 0; x < width; x++)
            {
                int idx = rowStart + x * 4;
                byte b = pixels[idx], g = pixels[idx + 1], r = pixels[idx + 2];
                if (r == br && g == bg_g && b == bb) { alphas[y * width + x] = 0; continue; }

                float sumAlpha = 0;
                if (canR) sumAlpha += ComputeAlpha(br, tr, r);
                if (canG) sumAlpha += ComputeAlpha(bg_g, tg, g);
                if (canB) sumAlpha += ComputeAlpha(bb, tb, b);
                float alpha = sumAlpha / validChannels;
                if (alpha < minAlpha || alpha > 1.0f) { alphas[y * width + x] = 0; continue; }

                float predR = tr * alpha + br * (1 - alpha);
                float predG = tg * alpha + bg_g * (1 - alpha);
                float predB = tb * alpha + bb * (1 - alpha);
                float errSq = (r - predR) * (r - predR) + (g - predG) * (g - predG) + (b - predB) * (b - predB);
                alphas[y * width + x] = errSq <= fitToleranceSq ? alpha : 0;
            }
        }
    }

    private static float ComputeAlpha(int bg, int target, int pixel)
    {
        int denom = bg - target;
        if (denom == 0) return 0;
        float a = (float)(bg - pixel) / denom;
        return a < 0f ? 0f : (a > 1f ? 1f : a);
    }

    private static void ReconstructBackground(byte[] pixels, float[] alphas,
        int width, int height, int stride, System.Drawing.Color background, double strength)
    {
        byte br = background.R, bg_g = background.G, bb = background.B;
        float fStrength = (float)strength;
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * stride;
            for (int x = 0; x < width; x++)
            {
                int pixelIdx = y * width + x;
                float alpha = alphas[pixelIdx];
                if (alpha > 0.005f)
                {
                    float effectiveAlpha = (float)System.Math.Pow(alpha, 0.5);
                    float mix = fStrength * effectiveAlpha;
                    if (mix > 1.0f) mix = 1.0f;
                    float keep = 1.0f - mix;
                    int idx = rowStart + x * 4;
                    pixels[idx]     = (byte)(pixels[idx]     * keep + bb    * mix);
                    pixels[idx + 1] = (byte)(pixels[idx + 1] * keep + bg_g  * mix);
                    pixels[idx + 2] = (byte)(pixels[idx + 2] * keep + br    * mix);
                }
            }
        }
    }
}
