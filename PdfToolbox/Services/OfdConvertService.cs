using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PdfToolbox.Services;

/// <summary>
/// OFD → PDF 转换服务：调用随程序部署的内置命令行转换引擎 ofd-convert.exe（Java 打包，子进程方式）。
/// 引擎路径：engine\ofd-convert\ofd-convert.exe（相对程序目录）。
/// 转换规则：输出到原文件同目录「原名.pdf」，已存在则依次加「-转换」「-转换-2」…后缀，不覆盖原文件。
/// 退出码 0 = 成功；非 0 = 读取 stderr 作为错误信息；单文件超过 60 秒自动终止转换进程。
/// </summary>
public static class OfdConvertService
{
    private const int TimeoutSeconds = 60;

    /// <summary>转换引擎完整路径。</summary>
    public static string EnginePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "engine", "ofd-convert", "ofd-convert.exe");

    /// <summary>严格 UTF-8（遇非法字节抛异常，用于引擎输出解码失败回退判断）。</summary>
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    static OfdConvertService()
    {
        // 注册代码页提供程序：引擎 stderr 若为 GBK 等非 UTF-8 编码时也能正确解码
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); }
        catch { /* 提供程序不可用时走 Decode 的宽松回退 */ }
    }

    /// <summary>校验转换引擎已随程序部署。缺失时抛出 <see cref="InvalidOperationException"/>。</summary>
    public static void EnsureEngine()
    {
        if (!File.Exists(EnginePath))
            throw new InvalidOperationException(
                "未找到转换引擎 engine\\ofd-convert\\ofd-convert.exe，请确认已随程序一起部署");
    }

    /// <summary>转换单个 OFD 文件为 PDF。成功返回输出 PDF 完整路径，失败抛出异常。outputDir 指定输出目录（不存在会自动创建），为 null 时输出到源文件同目录。</summary>
    public static async Task<string> ConvertAsync(string inputPath, Action<string> log, string? outputDir = null)
    {
        if (!File.Exists(inputPath)) throw new FileNotFoundException("文件不存在", inputPath);
        EnsureEngine();

        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir); // 指定输出目录不存在时先创建

        var outPath = ResolveOutputPath(inputPath, outputDir);
        log($"正在转换: {Path.GetFileName(inputPath)}");
        log($"输出: {outPath}");

        var psi = new ProcessStartInfo
        {
            FileName = EnginePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(inputPath);  // 引擎约定：参数 1 = 输入 OFD 完整路径
        psi.ArgumentList.Add(outPath);    // 引擎约定：参数 2 = 输出 PDF 完整路径

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动转换进程");

        // 立即开始异步读取 stdout/stderr，防止输出缓冲填满导致子进程阻塞
        var stdoutTask = ReadAllAsync(proc.StandardOutput.BaseStream);
        var stderrTask = ReadAllAsync(proc.StandardError.BaseStream);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        try
        {
            await proc.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* 进程可能已自行退出 */ }
            throw new InvalidOperationException($"转换超时（超过 {TimeoutSeconds} 秒），已终止转换进程");
        }

        var stderr = await stderrTask;
        await stdoutTask;

        if (proc.ExitCode != 0)
        {
            var msg = string.IsNullOrWhiteSpace(stderr) ? "转换失败（引擎未返回错误信息）" : stderr.Trim();
            throw new InvalidOperationException(msg);
        }
        return outPath;
    }

    /// <summary>计算输出路径：outputDir 内「原名.pdf」；已存在则依次尝试「原名-转换.pdf」「原名-转换-2.pdf」…，不覆盖原文件。outputDir 为 null 时用源文件目录。</summary>
    private static string ResolveOutputPath(string inputPath, string? outputDir)
    {
        var dir = string.IsNullOrWhiteSpace(outputDir) ? Path.GetDirectoryName(inputPath) ?? "" : outputDir;
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        var first = Path.Combine(dir, $"{baseName}.pdf");
        if (!File.Exists(first)) return first;
        var suffixed = Path.Combine(dir, $"{baseName}-转换.pdf");
        if (!File.Exists(suffixed)) return suffixed;
        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{baseName}-转换-{i}.pdf");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    /// <summary>读取子进程输出流原始字节并按 UTF-8（必要时回退系统 ANSI/宽松 UTF-8）解码。</summary>
    private static async Task<string> ReadAllAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        int n;
        while ((n = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            ms.Write(buffer, 0, n);
        return Decode(ms.ToArray());
    }

    /// <summary>先按严格 UTF-8 解码；非法字节时回退系统 ANSI（中文环境为 GBK），再失败按宽松 UTF-8。</summary>
    private static string Decode(byte[] raw)
    {
        try { return StrictUtf8.GetString(raw); }
        catch (DecoderFallbackException)
        {
            try { return Encoding.GetEncoding(0).GetString(raw); }
            catch (ArgumentException) { return Encoding.UTF8.GetString(raw); }
        }
    }
}