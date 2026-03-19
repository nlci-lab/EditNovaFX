; EditNovaFX Installer Script
; Requires Inno Setup 6 — https://jrsoftware.org/isdl.php

#define MyAppName "EditNovaFX"
#define MyAppVersion "0.0.1-alpha"
#define MyAppPublisher "NLCI Lab"
#define MyAppExeName "EditNovaFX.exe"
#define MyAppURL "https://github.com/nlci-lab/EditNovaFX"

[Setup]
AppId={{E7D1F0A3-8C4B-4F2E-9A1D-5B3C7E9F0A2D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output directory and filename for the compiled installer
OutputDir=..\InstallerOutput
OutputBaseFilename=EditNovaFX_Setup_v{#MyAppVersion}
; Use the app icon
SetupIconFile=..\Assets\app_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
; Compression settings
Compression=lzma2/ultra64
SolidCompression=yes
; Require admin for Program Files install
PrivilegesRequired=admin
; UI
WizardStyle=modern
WizardSizePercent=120
; Minimum Windows version (Windows 10)
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application files (published .NET 8 self-contained output)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Create empty folders for external tools — user configures paths in Settings
Name: "{app}\ffmpeg"; Flags: uninsneveruninstall
Name: "{app}\whisper-bin-x64"; Flags: uninsneveruninstall
Name: "{app}\subtitle-parser"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Messages]
FinishedLabel=Setup has finished installing [name] on your computer.%n%nIMPORTANT: To use all features, please configure the paths to FFmpeg, Whisper, and Subtitle Parser in File → Settings.
