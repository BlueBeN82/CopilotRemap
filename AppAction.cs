using System.Diagnostics;
using System.Text.Json.Serialization;

namespace CopilotRemap;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionType
{
    LaunchApp,
    LaunchStoreApp,
    RunInTerminal,
    OpenUrl
}

public sealed class AppAction
{
    public ActionType Type { get; init; }
    public string Target { get; init; } = "";
    public string Arguments { get; init; } = "";
    public string DisplayName { get; init; } = "";

    public void Execute()
    {
        switch (Type)
        {
            case ActionType.LaunchApp:
                Process.Start(new ProcessStartInfo
                {
                    FileName = Target,
                    Arguments = Arguments,
                    UseShellExecute = true
                });
                break;

            case ActionType.LaunchStoreApp:
                // Launch MSIX/Store apps via shell:AppsFolder\{AppUserModelId}
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"shell:AppsFolder\\{Target}",
                    UseShellExecute = false
                });
                break;

            case ActionType.RunInTerminal:
                LaunchInTerminal(Target, Arguments);
                break;

            case ActionType.OpenUrl:
                Process.Start(new ProcessStartInfo
                {
                    FileName = Target,
                    UseShellExecute = true
                });
                break;
        }
    }

    private static void LaunchInTerminal(string command, string args)
    {
        var fullCommand = string.IsNullOrEmpty(args) ? command : $"{command} {args}";

        try
        {
            // Windows Terminal (ships with Windows 11)
            Process.Start(new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = $"new-tab -- {fullCommand}",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fall back to PowerShell
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"& {fullCommand}\"",
                UseShellExecute = true
            });
        }
    }

    // --- Presets ---

    public static AppAction ClaudeCode() => new()
    {
        Type = ActionType.RunInTerminal,
        Target = "claude",
        DisplayName = "Claude Code (Terminal)"
    };

    public static AppAction ClaudeDesktop()
    {
        var appId = FindClaudeDesktopAppId();
        return new AppAction
        {
            Type = appId != null ? ActionType.LaunchStoreApp : ActionType.LaunchApp,
            Target = appId ?? "",
            DisplayName = "Claude Desktop"
        };
    }

    public static AppAction ClaudeWeb() => new()
    {
        Type = ActionType.OpenUrl,
        Target = "https://claude.ai",
        DisplayName = "claude.ai (Browser)"
    };

    public static bool IsClaudeDesktopInstalled() => FindClaudeDesktopAppId() != null;

    private static string? FindClaudeDesktopAppId()
    {
        try
        {
            // Query for the MSIX package
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"(Get-AppxPackage *Claude*).PackageFamilyName\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();

            if (!string.IsNullOrEmpty(output))
                return $"{output}!Claude";
        }
        catch { }

        return null;
    }
}
