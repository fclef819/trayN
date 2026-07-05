# trayN

trayN is a Windows task-tray quick memo app for temporary plain-text notes. It stays in the notification area, opens with the default hotkey `Ctrl + Alt + M`, and saves one memo automatically.

## Project Structure

```text
trayN.sln
TrayN/
  TrayN.csproj
  Program.cs
  TrayApplicationContext.cs
  MemoForm.cs
  MemoStore.cs
  SettingsStore.cs
  HotKeyManager.cs
  HotKeySettings.cs
  HotKeySettingsForm.cs
  HotKeyFormatter.cs
  HotKeyValidator.cs
  SingleInstanceManager.cs
  IpcSignal.cs
  IpcServer.cs
  UpdateService.cs
TrayN.Updater/
  TrayN.Updater.csproj
  Program.cs
  UpdaterOptions.cs
```

## Requirements

- Windows
- .NET 10 SDK
- No Visual Studio requirement
- No external NuGet packages

## Build

```powershell
dotnet build .\trayN.sln
```

## Run

```powershell
dotnet run --project .\TrayN\TrayN.csproj
```

The app starts in the task tray without showing the memo window.

## Publish

Publish both projects as Windows x64 self-contained single-file executables:

```powershell
dotnet publish .\TrayN\TrayN.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish .\TrayN.Updater\TrayN.Updater.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Expected executable names:

- `trayN.exe`
- `trayN.Updater.exe`

Place both files in the same writable directory. The built-in updater assumes both executables are next to each other and that the current user can replace `trayN.exe`.

## Data Files

trayN stores user data under:

```text
%LOCALAPPDATA%\trayn\
```

Files:

- `%LOCALAPPDATA%\trayn\memo.txt`
- `%LOCALAPPDATA%\trayn\settings.json`

The memo file is UTF-8. Settings include the memo window bounds, hotkey settings, and the last automatic update check time in UTC.

Hotkey settings are stored in a human-readable form:

```json
{
  "hotKey": {
    "control": true,
    "alt": true,
    "shift": false,
    "win": false,
    "key": "M"
  }
}
```

If `hotKey` is missing or invalid, trayN uses `Ctrl + Alt + M`.

## Shortcut and Tray Menu

- Default hotkey: `Ctrl + Alt + M`
- The hotkey shows or hides the memo window
- `Esc`: hide the memo window
- Window close button: hide the memo window
- Tray icon double-click: show the memo window
- Tray menu:
  - `メモを表示`
  - `ホットキー設定`
  - current hotkey display
  - `アップデートを確認`
  - `終了`

Choose `ホットキー設定` from the tray menu to change the global hotkey. The setting dialog captures the actual key combination you press, supports `Ctrl`, `Alt`, `Shift`, `Win`, and one normal key, and applies the new hotkey immediately after saving.

Remote Desktop, Amazon WorkSpaces, and similar environments may let a local app or the host OS capture global hotkeys before trayN receives them. If the hotkey does not work, choose another combination from the tray menu. If hotkey registration fails, trayN keeps running and the memo can still be opened from the tray icon.

Normal user exit is only through the tray menu `終了`.

## GitHub Releases

Update checking uses the GitHub latest release API:

```text
https://api.github.com/repos/<owner>/<repo>/releases/latest
```

Configure the repository constants in `TrayN/UpdateService.cs`:

```csharp
private const string GitHubOwner = "fclef819";
private const string GitHubRepository = "trayN";
```

Release tags must use this form:

```text
v1.2.3
```

Draft and prerelease releases are ignored.

This repository includes a GitHub Actions workflow at `.github/workflows/release.yml`.
When a tag matching `v*.*.*` is pushed, the workflow builds both projects, publishes Windows x64 self-contained single-file executables, generates `trayN.exe.sha256`, packages the two executables into `trayN.zip`, and creates a GitHub Release.

Create a release by pushing a version tag:

```powershell
git tag v1.2.3
git push origin v1.2.3
```

Attach these release assets:

- `trayN.exe`
- `trayN.exe.sha256`
- `trayN.Updater.exe`
- `trayN.zip`

The updater replaces only `trayN.exe`. Updating `trayN.Updater.exe` itself is intentionally not implemented.

## SHA-256 File

`trayN.exe.sha256` must contain:

```text
<SHA-256 hash>  trayN.exe
```

Generate it with PowerShell:

```powershell
$hash = (Get-FileHash .\trayN.exe -Algorithm SHA256).Hash
"$hash  trayN.exe" | Set-Content .\trayN.exe.sha256 -Encoding ascii
```

## Update Flow

1. trayN checks GitHub Releases in the background on startup.
2. Automatic startup checks run at most once every 24 hours.
3. Manual checks from the tray menu ignore the 24-hour limit.
4. If a newer version exists, trayN shows a tray balloon or confirmation dialog.
5. Update download starts only after user approval.
6. `trayN.exe` and `trayN.exe.sha256` are downloaded to a temporary directory.
7. SHA-256 is verified before applying the update.
8. trayN saves pending memo/settings data and starts `trayN.Updater.exe`.
9. The updater waits for trayN to exit, backs up the old `trayN.exe`, replaces it, starts the new `trayN.exe`, and removes temporary files.
10. If replacement fails, the updater restores the backup when possible.

## Manual Test Checklist

- Start trayN and confirm only the tray icon appears.
- Press the default `Ctrl + Alt + M` and confirm the memo window toggles on first launch.
- Open `ホットキー設定` and confirm the current value is shown.
- Change to a new hotkey and confirm it works immediately without restarting.
- Restart trayN and confirm the changed hotkey is restored.
- Cancel the hotkey dialog and confirm the setting is not changed.
- Use `既定値へ戻す`, save, and confirm `Ctrl + Alt + M` is restored.
- Try a hotkey already used by another app and confirm an error is shown.
- After a registration failure, confirm the previous hotkey still works.
- Confirm modifier-only input cannot be saved.
- Confirm reserved Windows shortcuts such as `Alt + Tab`, `Alt + F4`, `Win + L`, and `Win + R` cannot be saved.
- Confirm the tray menu still opens the memo when no hotkey is registered.
- Close the memo window and confirm trayN remains resident.
- Type text, wait briefly, restart, and confirm the memo is restored.
- Type text and exit immediately from the tray menu, then confirm text is not lost.
- Start a second `trayN.exe` and confirm the existing memo window is shown.
- Corrupt `%LOCALAPPDATA%\trayn\settings.json` and confirm startup still works.
- Remove `hotKey` from `%LOCALAPPDATA%\trayn\settings.json` and confirm `Ctrl + Alt + M` is used.
- Put an invalid `hotKey.key` value in `%LOCALAPPDATA%\trayn\settings.json` and confirm startup still works.
- Move monitors or edit saved bounds off-screen and confirm the window returns to a visible area.
- Confirm the tray icon disappears after exit.
- Confirm manual update check does not block memo editing.
- Confirm hash mismatch stops update and leaves the old `trayN.exe` intact.
- Confirm update failure restores the backup where possible.
