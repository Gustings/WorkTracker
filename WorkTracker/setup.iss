; Inno Setup Script for Work Tracker v1.5.3
[Setup]
AppId={{29A84BC0-D32A-4FBA-A951-40EA6464522A}}
AppName=Work Tracker
AppVersion=1.5.3
AppPublisher=Work Tracker
DefaultDirName={localappdata}\Programs\WorkTracker
DefaultGroupName=Work Tracker
DisableProgramGroupPage=yes
OutputDir=..\Installers
OutputBaseFilename=WorkTrackerSetup-1.5.3
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startonboot"; Description: "Start Work Tracker on Windows boot"; GroupDescription: "Startup options:"

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Work Tracker"; Filename: "{app}\WorkTracker.exe"
Name: "{autodesktop}\Work Tracker"; Filename: "{app}\WorkTracker.exe"; Tasks: desktopicon
Name: "{userstartup}\Work Tracker"; Filename: "{app}\WorkTracker.exe"; Tasks: startonboot

[Run]
Filename: "{app}\WorkTracker.exe"; Description: "{cm:LaunchProgram,Work Tracker}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\WorkTracker.exe"; Flags: nowait; Check: WizardSilent
