using System.Diagnostics;
using System.Text.Json;

namespace CopilotRemap;

public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHook _hook;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _currentLabel;
    private readonly List<ToolStripMenuItem> _actionItems = [];

    private AppAction? _action;

    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopilotRemap");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public TrayApp()
    {
        _action = LoadAction();

        _currentLabel = new ToolStripMenuItem(_action?.DisplayName ?? "No action configured")
        {
            Enabled = false,
            Font = new Font(SystemFonts.MenuFont!, FontStyle.Bold)
        };

        _startupItem = new ToolStripMenuItem("Run at Startup")
        {
            Checked = IsInStartup(),
            CheckOnClick = true
        };
        _startupItem.Click += (_, _) => ToggleStartup(_startupItem.Checked);

        // Presets
        var claudeCodeItem = MakeMenuItem("Claude Code (Terminal)", () =>
            SetAction(AppAction.ClaudeCode()));

        var claudeDesktopItem = MakeMenuItem("Claude Desktop", () =>
            SetAction(AppAction.ClaudeDesktop()));
        if (!AppAction.IsClaudeDesktopInstalled())
        {
            claudeDesktopItem.Text += " (not found)";
            claudeDesktopItem.Enabled = false;
        }

        var claudeWebItem = MakeMenuItem("claude.ai (Browser)", () =>
            SetAction(AppAction.ClaudeWeb()));

        // Custom options
        var customAppItem = MakeMenuItem("Custom Application...", OnCustomApp);
        var customCmdItem = MakeMenuItem("Custom Command...", OnCustomCommand);
        var customUrlItem = MakeMenuItem("Custom URL...", OnCustomUrl);

        _actionItems.AddRange([claudeCodeItem, claudeDesktopItem, claudeWebItem,
                               customAppItem, customCmdItem, customUrlItem]);

        UpdateCheckmarks();

        _trayIcon = new NotifyIcon
        {
            Icon = IconHelper.CreateTrayIcon(),
            Text = "CopilotRemap",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip
            {
                Items =
                {
                    _currentLabel,
                    new ToolStripSeparator(),
                    claudeCodeItem,
                    claudeDesktopItem,
                    claudeWebItem,
                    new ToolStripSeparator(),
                    customAppItem,
                    customCmdItem,
                    customUrlItem,
                    new ToolStripSeparator(),
                    _startupItem,
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("Exit", null, (_, _) => Exit())
                }
            }
        };

        _hook = new KeyboardHook();
        _hook.CopilotKeyPressed += OnCopilotKeyPressed;
        _hook.Install();
    }

    private ToolStripMenuItem MakeMenuItem(string text, Action onClick)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => onClick();
        return item;
    }

    private void SetAction(AppAction action)
    {
        _action = action;
        SaveAction(action);
        _currentLabel.Text = action.DisplayName;
        UpdateCheckmarks();
        _trayIcon.ShowBalloonTip(2000, "CopilotRemap",
            $"Copilot key → {action.DisplayName}", ToolTipIcon.Info);
    }

    private void UpdateCheckmarks()
    {
        foreach (var item in _actionItems)
            item.Checked = false;

        if (_action == null) return;

        // Match by display name for presets
        var match = _actionItems.FirstOrDefault(i =>
            (i.Text ?? "").Replace(" (not found)", "") == _action!.DisplayName);
        if (match != null)
            match.Checked = true;
    }

    // --- Custom action handlers ---

    private void OnCustomApp()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Application",
            Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        SetAction(new AppAction
        {
            Type = ActionType.LaunchApp,
            Target = dialog.FileName,
            DisplayName = Path.GetFileNameWithoutExtension(dialog.FileName)
        });
    }

    private void OnCustomCommand()
    {
        using var dialog = new InputDialog(
            "Custom Command",
            "Command to run in terminal (e.g. python, node, wsl):",
            _action?.Type == ActionType.RunInTerminal ? _action.Target : "");

        if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Value)) return;

        SetAction(new AppAction
        {
            Type = ActionType.RunInTerminal,
            Target = dialog.Value.Trim(),
            DisplayName = $"{dialog.Value.Trim()} (Terminal)"
        });
    }

    private void OnCustomUrl()
    {
        using var dialog = new InputDialog(
            "Custom URL",
            "URL to open in browser:",
            _action?.Type == ActionType.OpenUrl ? _action.Target : "https://");

        if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Value)) return;

        var url = dialog.Value.Trim();
        SetAction(new AppAction
        {
            Type = ActionType.OpenUrl,
            Target = url,
            DisplayName = new Uri(url).Host
        });
    }

    // --- Key handler ---

    private void OnCopilotKeyPressed()
    {
        if (_action == null || string.IsNullOrEmpty(_action.Target))
        {
            _trayIcon.ShowBalloonTip(3000, "CopilotRemap",
                "No action configured. Right-click the tray icon to set one.",
                ToolTipIcon.Warning);
            return;
        }

        if (_action.Type == ActionType.LaunchApp && !File.Exists(_action.Target))
        {
            _trayIcon.ShowBalloonTip(3000, "CopilotRemap",
                $"Target not found: {_action.Target}", ToolTipIcon.Error);
            return;
        }

        try
        {
            _action.Execute();
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(3000, "CopilotRemap",
                $"Failed: {ex.Message}", ToolTipIcon.Error);
        }
    }

    // --- Lifecycle ---

    private void Exit()
    {
        _hook.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // --- Startup shortcut via shell:startup ---

    private static string StartupShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "CopilotRemap.lnk");

    private static bool IsInStartup() => File.Exists(StartupShortcutPath);

    private static void ToggleStartup(bool enable)
    {
        if (enable)
        {
            string targetPath;
            string arguments = "";
            var processPath = Environment.ProcessPath ?? "";

            if (processPath.EndsWith("CopilotRemap.exe", StringComparison.OrdinalIgnoreCase))
            {
                // Published exe — shortcut points directly to it
                targetPath = processPath;
            }
            else
            {
                // Dev mode (dotnet run) — shortcut points to dotnet + dll
                targetPath = processPath;
                arguments = $"\"{Path.Combine(AppContext.BaseDirectory, "CopilotRemap.dll")}\"";
            }

            CreateShortcut(StartupShortcutPath, targetPath, arguments);
        }
        else if (File.Exists(StartupShortcutPath))
        {
            File.Delete(StartupShortcutPath);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments)
    {
        // Write a temp .ps1 script to avoid inline quoting issues
        var script = Path.Combine(Path.GetTempPath(), "CopilotRemap_mklink.ps1");
        File.WriteAllText(script,
            $"$ws = New-Object -ComObject WScript.Shell\n" +
            $"$s = $ws.CreateShortcut('{shortcutPath.Replace("'", "''")}')\n" +
            $"$s.TargetPath = '{targetPath.Replace("'", "''")}'\n" +
            $"$s.Arguments = '{arguments.Replace("'", "''")}'\n" +
            $"$s.Save()\n");

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
        proc?.WaitForExit();

        try { File.Delete(script); } catch { }
    }

    // --- Config persistence ---

    private static AppAction? LoadAction()
    {
        if (!File.Exists(ConfigFile)) return null;
        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppAction>(json);
        }
        catch { return null; }
    }

    private static void SaveAction(AppAction action)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(action, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
    }
}
