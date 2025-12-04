; Inno Setup script for SadaFileSyncer
[Setup]
AppName=SadaFileSyncer
AppVersion=1.0
DefaultDirName={autopf}\SadaFileSyncer
DisableProgramGroupPage=yes
OutputBaseFilename=SadaFileSyncer-Installer
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\SadaFileSyncer.exe
Compression=lzma
SolidCompression=yes

[Files]
; The publish folder from `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o publish`
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SadaFileSyncer"; Filename: "{app}\SadaFileSyncer.exe"
Name: "{group}\Uninstall SadaFileSyncer"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\SadaFileSyncer.exe"; Description: "Launch SadaFileSyncer"; Flags: nowait postinstall skipifsilent
