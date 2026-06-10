#ifndef SourceDir
  #error SourceDir must be provided to ISCC (/DSourceDir=...)
#endif

#ifndef OutputDir
  #error OutputDir must be provided to ISCC (/DOutputDir=...)
#endif

#ifndef OutputBase
  #error OutputBase must be provided to ISCC (/DOutputBase=...)
#endif

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef Arch
  #error Arch must be provided to ISCC (/DArch=x86|x64|arm64)
#endif

#define MyAppName "Chip8"
#define MyPublisher "vitimiti"
#define MyAppExeName "Chip8.exe"

[Setup]
AppId={{3C6F3B7C-2D67-4A90-B4E4-92AB3A5F9E4E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBase}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

#if "{#Arch}" == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

#if "{#Arch}" == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
