# FolderMoverAppModern

This project is a modern WinForms app (no Visual Studio required) that watches a folder (A) and moves or copies files to folder (B).  
Features:
- Modern simple UI (WinForms)
- System tray (NotifyIcon) with context menu
- Start-with-Windows toggle (writes to HKCU Run)
- Activity log saved to %LocalAppData%\FolderMoverAppModern\logs\activity.log
- Settings saved to %LocalAppData%\FolderMoverAppModern\settings.json
- Inno Setup installer script included (build after publishing)

## Build and publish (self-contained single EXE)
1. Install .NET 8 SDK.
2. Restore packages:
   `dotnet restore`
3. Build & Run:
   `dotnet run`
4. Publish self-contained single-file EXE for x64 Windows:
   `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o publish`

## Create installer (Inno Setup)
1. After publishing, open `InnoSetupInstaller.iss` in Inno Setup and compile. The script expects the `publish` folder to contain the published EXE and files.

