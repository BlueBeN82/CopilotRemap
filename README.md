# CopilotRemap

A lightweight Windows system tray utility that remaps the Copilot key on your keyboard to launch whatever you want.

No bloated apps, no PowerToys, no AutoHotkey — just a single, tiny .NET app that sits in your system tray and does exactly one thing: intercepts the Copilot key and runs your chosen action.

## Features

- **Intercepts the Copilot key** — handles both `VK_LAUNCH_APP1` and `Win+Shift+F23` key mappings used by different keyboards
- **Built-in presets** — one-click setup for Claude Code, Claude Desktop, or claude.ai
- **Fully customizable** — launch any application, run any terminal command, or open any URL
- **System tray app** — runs silently in the background with a right-click menu
- **Run at startup** — optional toggle to launch automatically when you log in
- **Single instance** — prevents duplicate copies from running
- **Zero dependencies** — just .NET (already on Windows 11)

## Quick Start

### Install

```
dotnet build -c Release
```

The built executable will be at `bin/Release/net9.0-windows/CopilotRemap.exe`.

### Run

```
dotnet run
```

Or launch `CopilotRemap.exe` directly after building.

### Publish as a standalone exe

```
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

This produces a small `publish/CopilotRemap.exe` that you can put anywhere.

## Usage

1. Run the app — an indigo keycap icon appears in your system tray
2. Right-click the icon to open the menu
3. Pick a preset or configure a custom action:

| Menu Option | What it does |
|---|---|
| **Claude Code (Terminal)** | Opens `claude` in Windows Terminal |
| **Claude Desktop** | Launches the Claude Desktop app (auto-detects MSIX install) |
| **claude.ai (Browser)** | Opens claude.ai in your default browser |
| **Custom Application...** | File picker — choose any `.exe` |
| **Custom Command...** | Run any command in a terminal (e.g. `python`, `wsl`, `node`) |
| **Custom URL...** | Open any URL in your default browser |
| **Run at Startup** | Toggle auto-launch on Windows login |

4. Press the Copilot key on your keyboard — your chosen action fires

## How It Works

CopilotRemap installs a low-level keyboard hook (`SetWindowsHookEx` with `WH_KEYBOARD_LL`) that intercepts key events before they reach any application. When it detects the Copilot key, it suppresses the original event and executes your configured action instead.

The Copilot key on Windows keyboards sends one of two signals depending on the manufacturer:
- **`VK_LAUNCH_APP1`** (0xB6) — used by some keyboards as a direct virtual key
- **`Win+Shift+F23`** — used by others as a key combination

CopilotRemap handles both.

## Configuration

Settings are stored as JSON at:

```
%APPDATA%\CopilotRemap\config.json
```

Example config:
```json
{
  "Type": "RunInTerminal",
  "Target": "claude",
  "Arguments": "",
  "DisplayName": "Claude Code (Terminal)"
}
```

## Building from Source

### Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or Visual Studio 2022 with .NET desktop workload)

### Build

```
git clone https://github.com/Zorrobyte/CopilotRemap.git
cd CopilotRemap
dotnet build
```

### Run

```
dotnet run
```

## Project Structure

```
CopilotRemap/
├── Program.cs          Entry point, single-instance mutex
├── TrayApp.cs          System tray icon, context menu, config
├── KeyboardHook.cs     Low-level keyboard hook (Win32 P/Invoke)
├── AppAction.cs        Action model, presets, Execute logic
├── InputDialog.cs      Minimal text input dialog
├── IconHelper.cs       Generates the tray icon at runtime via GDI+
└── CopilotRemap.csproj .NET 9 WinForms project
```

## License

[MIT](LICENSE)
