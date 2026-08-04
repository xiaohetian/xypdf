using System.Windows;
using System.Windows.Media;

namespace PdfToolbox.Services;

public class ThemeService
{
    private static readonly Lazy<ThemeService> _instance = new(() => new());
    public static ThemeService Instance => _instance.Value;

    private ThemeService() { }

    private static readonly Dictionary<string, ThemePalette> Themes = new()
    {
        ["默认蓝"] = new(
            Primary: "#4A90D9", PrimaryHover: "#357ABD", PrimaryPressed: "#2A5F9E",
            SidebarStart: "#1E2A3A", SidebarEnd: "#151D29", SidebarHover: "#2C3E50", SidebarSelected: "#2A3F54", SidebarActive: "#4A90D9",
            SidebarBorder: "#2C3E50", SidebarFooter: "#1A2332", SidebarText: "#B0BEC5",
            Background: "#F0F2F5", Card: "#FFFFFF", Border: "#E1E5EB",
            TextPrimary: "#2C3E50", TextSecondary: "#7F8C9B",
            InputBackground: "#F8F9FA", Icon: "#B0BEC5", IconActive: "#FFFFFF"
        ),
        ["翡翠绿"] = new(
            Primary: "#27AE60", PrimaryHover: "#219A52", PrimaryPressed: "#1E8449",
            SidebarStart: "#1A3A2A", SidebarEnd: "#12291E", SidebarHover: "#234F38", SidebarSelected: "#285440", SidebarActive: "#27AE60",
            SidebarBorder: "#234F38", SidebarFooter: "#142820", SidebarText: "#A0C0B0",
            Background: "#F0F7F3", Card: "#FFFFFF", Border: "#D5E5DD",
            TextPrimary: "#1C3A2A", TextSecondary: "#6B8F7A",
            InputBackground: "#F5FAF7", Icon: "#A0C0B0", IconActive: "#FFFFFF"
        ),
        ["典雅紫"] = new(
            Primary: "#8E44AD", PrimaryHover: "#7D3C98", PrimaryPressed: "#6C3483",
            SidebarStart: "#2D1B3A", SidebarEnd: "#1F1328", SidebarHover: "#3D2550", SidebarSelected: "#3D2550", SidebarActive: "#8E44AD",
            SidebarBorder: "#3D2550", SidebarFooter: "#1E1130", SidebarText: "#B8A0C8",
            Background: "#F5F0F7", Card: "#FFFFFF", Border: "#DDD5E5",
            TextPrimary: "#2D1B3A", TextSecondary: "#8B7A9F",
            InputBackground: "#FAF5FC", Icon: "#B8A0C8", IconActive: "#FFFFFF"
        ),
        ["暗色"] = new(
            Primary: "#5B7A9E", PrimaryHover: "#4A6A8E", PrimaryPressed: "#3A5A7E",
            SidebarStart: "#1A1A1A", SidebarEnd: "#111111", SidebarHover: "#2A2A2A", SidebarSelected: "#2A2A2A", SidebarActive: "#5B7A9E",
            SidebarBorder: "#2A2A2A", SidebarFooter: "#151515", SidebarText: "#808080",
            Background: "#1E1E1E", Card: "#2D2D2D", Border: "#3A3A3A",
            TextPrimary: "#E0E0E0", TextSecondary: "#909090",
            InputBackground: "#383838", Icon: "#808080", IconActive: "#E0E0E0"
        )
    };

    public void ApplyTheme(string themeName)
    {
        if (!Themes.TryGetValue(themeName, out var theme))
            theme = Themes["默认蓝"];

        var resources = Application.Current.Resources;

        void SetBrush(string key, string color)
        {
            if (ColorConverter.ConvertFromString(color) is Color c)
                resources[key] = new SolidColorBrush(c);
        }

        SetBrush("PrimaryBrush", theme.Primary);
        SetBrush("PrimaryHoverBrush", theme.PrimaryHover);
        SetBrush("PrimaryPressedBrush", theme.PrimaryPressed);
        SetBrush("SidebarStartBrush", theme.SidebarStart);
        SetBrush("SidebarEndBrush", theme.SidebarEnd);
        SetBrush("SidebarHoverBrush", theme.SidebarHover);
        SetBrush("SidebarSelectedBrush", theme.SidebarSelected);
        SetBrush("SidebarActiveBrush", theme.SidebarActive);
        SetBrush("SidebarBorderBrush", theme.SidebarBorder);
        SetBrush("SidebarFooterBrush", theme.SidebarFooter);
        SetBrush("SidebarTextBrush", theme.SidebarText);
        SetBrush("BackgroundBrush", theme.Background);
        SetBrush("CardBrush", theme.Card);
        SetBrush("BorderBrush", theme.Border);
        SetBrush("TextPrimaryBrush", theme.TextPrimary);
        SetBrush("TextSecondaryBrush", theme.TextSecondary);
        SetBrush("InputBackgroundBrush", theme.InputBackground);
        SetBrush("IconBrush", theme.Icon);
        SetBrush("IconActiveBrush", theme.IconActive);

        // sidebar gradient — creates a new LinearGradientBrush each time
        if (ColorConverter.ConvertFromString(theme.SidebarStart) is Color startColor &&
            ColorConverter.ConvertFromString(theme.SidebarEnd) is Color endColor)
        {
            resources["SidebarBackgroundBrush"] = new LinearGradientBrush(startColor, endColor, 90.0);
        }
    }

    private readonly record struct ThemePalette(
        string Primary, string PrimaryHover, string PrimaryPressed,
        string SidebarStart, string SidebarEnd, string SidebarHover, string SidebarSelected, string SidebarActive,
        string SidebarBorder, string SidebarFooter, string SidebarText,
        string Background, string Card, string Border,
        string TextPrimary, string TextSecondary,
        string InputBackground, string Icon, string IconActive
    );
}
