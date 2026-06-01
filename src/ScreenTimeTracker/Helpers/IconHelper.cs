using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenTimeTracker.Helpers;

public static class IconHelper
{
    private static readonly Dictionary<string, string> IconCache = new();

    public static string? ExtractIconAsBase64(string processName)
    {
        // Check cache first
        if (IconCache.TryGetValue(processName, out var cached))
            return cached;

        try
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(processName);
            var procs = Process.GetProcessesByName(nameWithoutExt);
            if (procs.Length == 0)
                return GetDefaultIconForCategory(processName);

            var exePath = procs[0].MainModule?.FileName;
            if (exePath == null || !File.Exists(exePath))
                return GetDefaultIconForCategory(processName);

            using var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon == null)
                return GetDefaultIconForCategory(processName);

            using var bitmap = icon.ToBitmap();
            // Resize to 64x64 for consistent display
            using var resized = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, 64, 64);
            }

            using var ms = new MemoryStream();
            resized.Save(ms, ImageFormat.Png);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var result = $"data:image/png;base64,{base64}";

            // Cache
            IconCache[processName] = result;
            return result;
        }
        catch
        {
            var fallback = GetDefaultIconForCategory(processName);
            IconCache[processName] = fallback;
            return fallback;
        }
    }

    private static string GetDefaultIconForCategory(string processName)
    {
        // Return a simple colored circle placeholder
        // Categories: dev=blue, browser=teal, game=orange, media=purple, office=green, chat=red
        var color = GetCategoryColor(processName);
        return CreateColoredCircleSvg(color);
    }

    private static string GetCategoryColor(string processName)
    {
        var name = processName.ToLowerInvariant();
        if (name.Contains("code") || name.Contains("devenv") || name.Contains("idea") ||
            name.Contains("pycharm") || name.Contains("rider") || name.Contains("notepad++"))
            return "#007AFF"; // Blue - Development
        if (name.Contains("chrome") || name.Contains("edge") || name.Contains("firefox") ||
            name.Contains("opera") || name.Contains("browser"))
            return "#5AC8FA"; // Teal - Browsers
        if (name.Contains("game") || name.Contains("steam") || name.Contains("gta") ||
            name.Contains("elden") || name.Contains("cs2") || name.Contains("valorant"))
            return "#FF9500"; // Orange - Gaming
        if (name.Contains("vlc") || name.Contains("spotify") || name.Contains("netflix") ||
            name.Contains("bilibili") || name.Contains("music"))
            return "#AF52DE"; // Purple - Media
        if (name.Contains("word") || name.Contains("excel") || name.Contains("powerpnt") ||
            name.Contains("wps") || name.Contains("office"))
            return "#34C759"; // Green - Office
        if (name.Contains("wechat") || name.Contains("qq") || name.Contains("teams") ||
            name.Contains("slack") || name.Contains("discord") || name.Contains("telegram"))
            return "#FF3B30"; // Red - Communication
        return "#8E8E93"; // Gray - Other
    }

    private static string CreateColoredCircleSvg(string color)
    {
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><circle cx=\"50\" cy=\"50\" r=\"50\" fill=\"{color}\"/></svg>";
    }
}
