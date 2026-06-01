using System.Diagnostics;
using System.IO;

namespace ScreenTimeTracker.Helpers;

public static class ProcessHelper
{
    private static readonly Dictionary<string, string> KnownAppsMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Browsers
        ["chrome.exe"] = "Google Chrome",
        ["msedge.exe"] = "Microsoft Edge",
        ["firefox.exe"] = "Mozilla Firefox",
        ["opera.exe"] = "Opera",
        ["brave.exe"] = "Brave",

        // Development
        ["devenv.exe"] = "Visual Studio 2022",
        ["code.exe"] = "Visual Studio Code",
        ["msbuild.exe"] = "MSBuild",
        ["idea64.exe"] = "IntelliJ IDEA",
        ["pycharm64.exe"] = "PyCharm",
        ["webstorm64.exe"] = "WebStorm",
        ["rider64.exe"] = "JetBrains Rider",
        ["notepad++.exe"] = "Notepad++",
        ["sublime_text.exe"] = "Sublime Text",
        ["cursor.exe"] = "Cursor",
        ["trae.exe"] = "Trae",

        // Office / Productivity
        ["winword.exe"] = "Microsoft Word",
        ["excel.exe"] = "Microsoft Excel",
        ["powerpnt.exe"] = "Microsoft PowerPoint",
        ["outlook.exe"] = "Microsoft Outlook",
        ["onenote.exe"] = "Microsoft OneNote",
        ["notepad.exe"] = "Notepad",
        ["mspaint.exe"] = "Paint",

        // Communication
        ["wechat.exe"] = "WeChat",
        ["weixin.exe"] = "WeChat",
        ["qq.exe"] = "QQ",
        ["dingtalk.exe"] = "DingTalk",
        ["teams.exe"] = "Microsoft Teams",
        ["slack.exe"] = "Slack",
        ["discord.exe"] = "Discord",
        ["telegram.exe"] = "Telegram",
        ["feishu.exe"] = "Feishu",
        ["lark.exe"] = "Lark",

        // Media / Entertainment
        ["vlc.exe"] = "VLC Media Player",
        ["spotify.exe"] = "Spotify",
        ["netflix.exe"] = "Netflix",
        ["bilibili.exe"] = "Bilibili",

        // Gaming
        ["steam.exe"] = "Steam",
        ["epicgameslauncher.exe"] = "Epic Games Launcher",
        ["gta5.exe"] = "GTA V",
        ["cs2.exe"] = "Counter-Strike 2",
        ["eldenring.exe"] = "Elden Ring",
        ["minecraft.exe"] = "Minecraft",
        ["league of legends.exe"] = "League of Legends",
        ["valorant.exe"] = "Valorant",

        // Design
        ["photoshop.exe"] = "Adobe Photoshop",
        ["illustrator.exe"] = "Adobe Illustrator",
        ["afterfx.exe"] = "Adobe After Effects",
        ["premiere.exe"] = "Adobe Premiere Pro",

        // File Management
        ["explorer.exe"] = "File Explorer",
        ["totalcmd64.exe"] = "Total Commander",
        ["7zfm.exe"] = "7-Zip",

        // System
        ["taskmgr.exe"] = "Task Manager",
        ["cmd.exe"] = "Command Prompt",
        ["powershell.exe"] = "PowerShell",
        ["windowsterminal.exe"] = "Windows Terminal",
        ["regedit.exe"] = "Registry Editor",
        ["calc.exe"] = "Calculator",

        // Virtualization
        ["vmware.exe"] = "VMware",
        ["virtualbox.exe"] = "VirtualBox",
        ["androidstudio.exe"] = "Android Studio",

        // Chinese Software
        ["wps.exe"] = "WPS Office",
        ["sogouexplorer.exe"] = "Sogou Browser",
        ["360chrome.exe"] = "360 Browser",
        ["360se.exe"] = "360 Browser",
        ["qbrowser.exe"] = "QQ Browser",
        ["thunder.exe"] = "Thunder",
        ["baidunetdisk.exe"] = "Baidu NetDisk",
        ["cloudmusic.exe"] = "NetEase Cloud Music",
        ["kwmusic.exe"] = "Kuwo Music",
        ["douyin.exe"] = "Douyin",
        ["kuaishou.exe"] = "Kuaishou",
        ["huya.exe"] = "Huya Live",
        ["douyu.exe"] = "Douyu Live",
    };

    private static readonly HashSet<string> SystemProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "svchost.exe", "runtimebroker.exe", "applicationframehost.exe",
        "shellexperiencehost.exe", "searchhost.exe", "startmenuexperiencehost.exe",
        "textinputhost.exe", "systemsettings.exe", "taskmgr.exe",
        "sihost.exe", "ctfmon.exe", "dwm.exe", "csrss.exe",
        "smss.exe", "winlogon.exe", "services.exe", "lsass.exe",
        "spoolsv.exe", "audiodg.exe", "conhost.exe", "wlms.exe",
    };

    public static string GetFriendlyName(string processName)
    {
        if (KnownAppsMapping.TryGetValue(processName, out var name))
            return name;

        // Fallback: strip .exe and title-case
        var nameWithoutExt = Path.GetFileNameWithoutExtension(processName);
        return string.IsNullOrEmpty(nameWithoutExt) ? processName : nameWithoutExt;
    }

    public static bool IsSystemProcess(string processName)
    {
        return SystemProcesses.Contains(processName);
    }

    public static string GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return (process.ProcessName + ".exe").ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string? GetProcessPath(string processName)
    {
        try
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(processName);
            var procs = Process.GetProcessesByName(nameWithoutExt);
            if (procs.Length > 0)
            {
                return procs[0].MainModule?.FileName;
            }
        }
        catch
        {
            // Process access denied or process exited
        }

        return null;
    }
}
