using System.Runtime.InteropServices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Drawing.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PdfToolbox.Services;

/// <summary>
/// 基于图像处理的 PDF 水印去除服务（v2.0 优化版）
/// 原理：将 PDF 页面渲染为高分辨率位图 → 检测并去除水印 → 重建 PDF
///
/// 算法核心（alpha 反演 + 掩码膨胀 + 非线性重建）：
/// 1. 背景色估计：自动从页面边缘采样背景色（通常为白色）
/// 2. Alpha 反演：基于"像素 = 目标色 × α + 背景色 × (1-α)"反解每个像素的不透明度 α
/// 3. 拟合校验：检查像素是否符合目标色与背景色的混合关系
/// 4. 掩码膨胀：对检测到的水印掩码进行形态学膨胀，完全覆盖抗锯齿边缘
/// 5. 非线性重建：使用幂函数增强低 alpha 区域的替换力度，彻底去除水印痕迹
/// 6. 空间一致性过滤：去除孤立检测点（单像素误判），保留成片水印区域
/// </summary>
public class WatermarkRemovalService
{
    private const int RenderDpi = 300; // 从200提升到300，提高检测精度

    /// <summary>
    /// 去除 PDF 中的水印
    /// </summary>
    public async Task<string?> RemoveWatermarkAsync(
        string inputPath,
        string outputPath,
        System.Drawing.Color removeColor,
        int tolerance = 100,
        double strength = 1.0,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
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
                        var size = page.Size; // DIPs (1 DIP = 1/96 inch)
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
                        using var processed = ProcessPage(rawBitmap, removeColor, tolerance, strength);
                        processed.SetResolution(RenderDpi, RenderDpi);
                        using var ms = new MemoryStream();
                        processed.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        pageImageBytes.Add(ms.ToArray());

                        progress?.Report((int)((double)(i + 1) / totalPages * 50));
                    }
                }
                finally
                {
                    pdfDocument = null;
                }

                // 使用 QuestPDF 重建 PDF
                QuestPDF.Settings.License = LicenseType.Community;
                var doc = Document.Create(container =>
                {
                    for (int i = 0; i < pageImageBytes.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var bmpFromBytes = new System.Drawing.Bitmap(new MemoryStream(pageImageBytes[i]));
                        var ptW = bmpFromBytes.Width / (double)RenderDpi * 72.0;
                        var ptH = bmpFromBytes.Height / (double)RenderDpi * 72.0;

                        var currentBytes = pageImageBytes[i];
                        var currentPtW = ptW;
                        var currentPtH = ptH;
                        var currentIndex = i;

                        container.Page(page =>
                        {
                            page.Size((float)currentPtW, (float)currentPtH, Unit.Point);
                            page.Margin(0);
                            page.Content().Image(currentBytes).FitWidth();

                            progress?.Report(50 + (int)((double)(currentIndex + 1) / totalPages * 50));
                        });
                    }
                });

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                doc.GeneratePdf(outputPath);
                return null;
            }
            catch (OperationCanceledException)
            {
                return "操作已取消";
            }
            catch (Exception ex)
            {
                return $"去除水印失败: {ex.Message}";
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 处理单页位图（v2.0 优化版）
    /// 改进点：
    /// 1. 掩码膨胀 - 完全覆盖抗锯齿边缘
    /// 2. 非线性重建 - 增强低 alpha 替换力度
    /// 3. 更低的 minAlpha 阈值 - 捕获更浅的水印像素
    /// </summary>
    private static System.Drawing.Bitmap ProcessPage(
        System.Drawing.Bitmap source,
        System.Drawing.Color targetColor,
        int tolerance,
        double strength)
    {
        int width = source.Width;
        int height = source.Height;

        // 1. 获取像素数据
        var srcData = source.LockBits(
            new System.Drawing.Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        int stride = srcData.Stride;
        byte[] pixels = new byte[stride * height];
        Marshal.Copy(srcData.Scan0, pixels, 0, pixels.Length);
        source.UnlockBits(srcData);

        // 2. 估计背景色（从页面边缘采样）
        var background = EstimateBackground(pixels, width, height, stride);

        // 3. 构建水印 alpha 掩码（低阈值捕获浅水印）
        float[] alphas = new float[width * height];
        BuildAlphaMask(pixels, alphas, width, height, stride,
            targetColor, background, tolerance, 0.05f); // minAlpha从0.10降到0.05

        // 4. 空间一致性过滤：移除孤立检测点
        FilterIsolatedDetections(alphas, width, height, minNeighborAlpha: 0.05f);

        // 5. ⭐ 掩码膨胀：扩大检测区域覆盖抗锯齿边缘
        DilateAlphaMask(alphas, width, height, iterations: 2);

        // 6. 非线性重建：用幂函数增强低 alpha 替换力度
        ReconstructBackground(pixels, pixels, alphas, width, height, stride,
            background, strength);

        // 7. 创建结果位图
        var result = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var resultData = result.LockBits(
            new System.Drawing.Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        int resultStride = resultData.Stride;
        int rowBytes = Math.Min(stride, resultStride);
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(pixels, y * stride, resultData.Scan0 + y * resultStride, rowBytes);
        }
        result.UnlockBits(resultData);

        return result;
    }

    /// <summary>
    /// ⭐ 掩码膨胀：对检测到的水印掩码进行形态学膨胀
    /// 水印的抗锯齿边缘 alpha 值较低，可能在检测中被漏掉
    /// 膨胀可以覆盖这些边缘像素，确保水印被完整去除
    /// </summary>
    private static void DilateAlphaMask(float[] alphas, int width, int height, int iterations = 2)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            float[] dilated = new float[alphas.Length];
            Array.Copy(alphas, dilated, alphas.Length);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (dilated[idx] > 0.01f)
                    {
                        // 膨胀：将当前检测到的像素的 alpha 传播到 3x3 邻域
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                                int nIdx = ny * width + nx;
                                // 用当前像素的 alpha 值填充邻居（如果邻居的 alpha 更低）
                                if (alphas[nIdx] < dilated[idx])
                                {
                                    alphas[nIdx] = dilated[idx];
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 空间一致性过滤：仅保留与邻居相连的检测点
    /// </summary>
    private static void FilterIsolatedDetections(
        float[] alphas, int width, int height, float minNeighborAlpha)
    {
        float[] filtered = new float[alphas.Length];
        Array.Copy(alphas, filtered, alphas.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                if (filtered[idx] <= 0.005f) continue;

                bool hasNeighbor = false;
                for (int dy = -1; dy <= 1 && !hasNeighbor; dy++)
                {
                    for (int dx = -1; dx <= 1 && !hasNeighbor; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (filtered[ny * width + nx] > minNeighborAlpha)
                        {
                            hasNeighbor = true;
                        }
                    }
                }

                if (!hasNeighbor)
                {
                    alphas[idx] = 0;
                }
            }
        }
    }

    /// <summary>
    /// 估计页面背景色：从页面四条边缘采样像素
    /// </summary>
    private static System.Drawing.Color EstimateBackground(
        byte[] pixels, int width, int height, int stride)
    {
        long sumR = 0, sumG = 0, sumB = 0;
        int count = 0;

        int step = Math.Max(1, Math.Min(width, height) / 100);

        // 上边缘
        for (int x = 0; x < width; x += step)
        {
            int idx = x * 4;
            sumB += pixels[idx];
            sumG += pixels[idx + 1];
            sumR += pixels[idx + 2];
            count++;
        }

        // 下边缘
        for (int x = 0; x < width; x += step)
        {
            int idx = (height - 1) * stride + x * 4;
            sumB += pixels[idx];
            sumG += pixels[idx + 1];
            sumR += pixels[idx + 2];
            count++;
        }

        // 左边缘
        for (int y = 0; y < height; y += step)
        {
            int idx = y * stride;
            sumB += pixels[idx];
            sumG += pixels[idx + 1];
            sumR += pixels[idx + 2];
            count++;
        }

        // 右边缘
        for (int y = 0; y < height; y += step)
        {
            int idx = y * stride + (width - 1) * 4;
            sumB += pixels[idx];
            sumG += pixels[idx + 1];
            sumR += pixels[idx + 2];
            count++;
        }

        byte avgR = (byte)(sumR / count);
        byte avgG = (byte)(sumG / count);
        byte avgB = (byte)(sumB / count);

        // 如果边缘接近白色，认为是白底文档
        if (avgR >= 240 && avgG >= 240 && avgB >= 240)
        {
            return System.Drawing.Color.White;
        }

        return System.Drawing.Color.FromArgb(avgR, avgG, avgB);
    }

    /// <summary>
    /// 构建水印 alpha 掩码
    /// </summary>
    private static void BuildAlphaMask(
        byte[] pixels, float[] alphas,
        int width, int height, int stride,
        System.Drawing.Color targetColor, System.Drawing.Color background,
        int fitTolerance, float minAlpha)
    {
        byte tr = targetColor.R, tg = targetColor.G, tb = targetColor.B;
        byte br = background.R, bg_g = background.G, bb = background.B;

        int denomR = br - tr;
        int denomG = bg_g - tg;
        int denomB = bb - tb;

        bool canR = denomR != 0;
        bool canG = denomG != 0;
        bool canB = denomB != 0;
        int validChannels = (canR ? 1 : 0) + (canG ? 1 : 0) + (canB ? 1 : 0);
        if (validChannels == 0) return;

        float fitToleranceSq = fitTolerance * fitTolerance;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * stride;
            for (int x = 0; x < width; x++)
            {
                int idx = rowStart + x * 4;
                byte b = pixels[idx];
                byte g = pixels[idx + 1];
                byte r = pixels[idx + 2];

                // 跳过纯背景像素
                if (r == br && g == bg_g && b == bb)
                {
                    alphas[y * width + x] = 0;
                    continue;
                }

                // 计算每通道的 alpha 并取平均
                float sumAlpha = 0;
                if (canR) sumAlpha += ComputeAlpha(br, tr, r);
                if (canG) sumAlpha += ComputeAlpha(bg_g, tg, g);
                if (canB) sumAlpha += ComputeAlpha(bb, tb, b);
                float alpha = sumAlpha / validChannels;

                // alpha 必须在合理范围内
                if (alpha < minAlpha || alpha > 1.0f)
                {
                    alphas[y * width + x] = 0;
                    continue;
                }

                // 计算拟合误差
                float predR = tr * alpha + br * (1 - alpha);
                float predG = tg * alpha + bg_g * (1 - alpha);
                float predB = tb * alpha + bb * (1 - alpha);
                float errR = r - predR;
                float errG = g - predG;
                float errB = b - predB;
                float errSq = errR * errR + errG * errG + errB * errB;

                if (errSq > fitToleranceSq)
                {
                    alphas[y * width + x] = 0;
                    continue;
                }

                alphas[y * width + x] = alpha;
            }
        }
    }

    /// <summary>
    /// 计算单个通道的 alpha
    /// </summary>
    private static float ComputeAlpha(int bg, int target, int pixel)
    {
        int denom = bg - target;
        if (denom == 0) return 0;
        float a = (float)(bg - pixel) / denom;
        if (a < 0f) return 0f;
        if (a > 1f) return 1f;
        return a;
    }

    /// <summary>
    /// ⭐ 非线性重建（v2.0 优化版）
    ///
    /// 关键改进：使用幂函数增强低 alpha 区域的替换力度
    /// - 原公式：mix = strength × alpha
    ///   → 低 alpha（0.05）的替换比例只有 5%，水印几乎不变
    /// - 新公式：mix = pow(strength × alpha, 0.5)
    ///   → 低 alpha（0.05）的替换比例提升到 22%
    ///   → 高 alpha（1.0）的替换比例保持 100%
    ///
    /// 这种非线性映射确保：
    /// - 水印主体（高 alpha）被完全替换
    /// - 抗锯齿边缘（低 alpha）也被充分替换，不留残影
    /// - 误判像素（极低 alpha）受到的影响仍然极小
    /// </summary>
    private static void ReconstructBackground(
        byte[] source, byte[] output, float[] alphas,
        int width, int height, int stride,
        System.Drawing.Color background, double strength)
    {
        byte br = background.R, bg_g = background.G, bb = background.B;
        float fStrength = (float)strength;

        // 对 output 使用 source 的副本（直接修改 pixels 而不是 output）
        // 注意：这里我们直接修改 source（pixels）数组，因为在 ProcessPage 中
        // 传入的是 pixels，output 未使用
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * stride;
            for (int x = 0; x < width; x++)
            {
                int pixelIdx = y * width + x;
                float alpha = alphas[pixelIdx];

                if (alpha > 0.005f)
                {
                    // ⭐ 非线性映射：pow(alpha, 0.5) 增强低 alpha 区域
                    float effectiveAlpha = (float)System.Math.Pow(alpha, 0.5);
                    float mix = fStrength * effectiveAlpha;
                    if (mix > 1.0f) mix = 1.0f;
                    float keep = 1.0f - mix;

                    int idx = rowStart + x * 4;
                    source[idx]     = (byte)(source[idx]     * keep + bb    * mix);
                    source[idx + 1] = (byte)(source[idx + 1] * keep + bg_g  * mix);
                    source[idx + 2] = (byte)(source[idx + 2] * keep + br    * mix);
                }
            }
        }
    }
}
