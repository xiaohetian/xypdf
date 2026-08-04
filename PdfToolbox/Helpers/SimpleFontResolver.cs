using PdfSharp.Fonts;
using System.IO;
using System.Collections.Generic;

namespace PdfToolbox.Helpers;

/// <summary>
/// 简单字体解析器：处理常用系统字体，未知字体回退到 Arial
/// </summary>
public class SimpleFontResolver : IFontResolver
{
    private readonly Dictionary<string, string> _fontPaths = new();
    private readonly string _fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

    public SimpleFontResolver()
    {
        // 预加载常用字体
        RegisterFont("Microsoft YaHei", "msyh.ttc");
        RegisterFont("Microsoft YaHei UI", "msyh.ttc");
        RegisterFont("SimSun", "simsun.ttc");
        RegisterFont("SimHei", "simhei.ttf");
        RegisterFont("KaiTi", "simkai.ttf");
        RegisterFont("FangSong", "simfang.ttf");
        RegisterFont("Arial", "arial.ttf");
        RegisterFont("Times New Roman", "times.ttf");
        RegisterFont("Courier New", "cour.ttf");
        RegisterFont("Segoe UI", "segoeui.ttf");
        RegisterFont("Calibri", "calibri.ttf");
        RegisterFont("Verdana", "verdana.ttf");
        RegisterFont("Tahoma", "tahoma.ttf");
    }

    private void RegisterFont(string familyName, string fileName)
    {
        var path = Path.Combine(_fontsDir, fileName);
        if (File.Exists(path))
        {
            _fontPaths[familyName.ToLower()] = path;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var key = familyName.ToLower();

        // 1. 精确匹配
        if (_fontPaths.TryGetValue(key, out var fontPath))
        {
            return new FontResolverInfo(fontPath, mustSimulateBold: false, mustSimulateItalic: isItalic);
        }

        // 2. 模糊匹配
        foreach (var kvp in _fontPaths)
        {
            if (kvp.Key.Contains(key) || key.Contains(kvp.Key))
            {
                return new FontResolverInfo(kvp.Value, mustSimulateBold: false, mustSimulateItalic: isItalic);
            }
        }

        // 3. 回退到 Arial
        if (_fontPaths.TryGetValue("arial", out var arialPath))
        {
            return new FontResolverInfo(arialPath, mustSimulateBold: isBold, mustSimulateItalic: isItalic);
        }

        return null;
    }

    public byte[]? GetFont(string faceName)
    {
        // faceName 现在是完整的文件路径
        try
        {
            if (File.Exists(faceName))
            {
                return File.ReadAllBytes(faceName);
            }
        }
        catch { }

        return null;
    }
}