; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{1AAA3319-FDD8-49D9-828E-BB33378E67F3}
AppName=Quick Converter
AppVersion=0.5
AppPublisher=Maximilien Noal
AppPublisherURL=https://github.com/maximilien-noal
DefaultDirName={pf64}\Quick Converter
DefaultGroupName=\Quick Converter
InfoBeforeFile=LICENSE.txt
OutputDir=.\
OutputBaseFilename=setup - Quick Converter
SetupIconFile=Quick.ico
WizardImageFile=Installer.bmp
SolidCompression=True
DisableProgramGroupPage=True

[Icons]
Name: "{group}\Quick Converter"; Filename: "{app}\QuickConvert.exe";
Name: "{group}\{cm:UninstallProgram,Quick Converter}"; Filename: "{uninstallexe}";
Name: "{commondesktop}\Quick Converter"; Filename: "{app}\QuickConvert.exe";
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\Quick Converter"; Filename: "{app}\QuickConvert.exe";

[Run]
Filename: "{app}\QuickConvert.exe"; Flags: shellexec postinstall skipifsilent; Description: "{cm:LaunchProgram,Quick Converter}"

[Languages]
Name: french; MessagesFile: compiler:Languages\French.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons};
Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
Source: "Quick.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "RelPub\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Imported Registry File: "C:\Users\noalm\source\repos\QuickConvert\Installer\Folders.reg"
Root: "HKCU"; Subkey: "Software\Classes\directory\shell\QuickConvert"; ValueType: string; ValueData: "Convertir tout le dossier en MP3"; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\directory\shell\QuickConvert"; ValueType: string; ValueName: "icon"; ValueData: "C:\Program Files\Quick Converter\Quick.ico"; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\directory\shell\QuickConvert\command"; ValueType: string; ValueData: "C:\Program Files\Quick Converter\QuickConvert.exe ""%1"""; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\*\shell\QuickConvert"; ValueType: string; ValueData: "Convertir en MP3"; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\*\shell\QuickConvert"; ValueType: string; ValueName: "icon"; ValueData: "C:\Program Files\Quick Converter\Quick.ico"; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\*\shell\QuickConvert\command"; ValueType: string; ValueData: "C:\Program Files\Quick Converter\QuickConvert.exe ""%1"""; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\.flac\shell\QuickConvert"; ValueType: string; ValueData: "Convertir en MP3"; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\.flac\shell\QuickConvert"; ValueType: string; ValueName: "icon"; ValueData: "C:\Program Files\Quick Converter\Quick.ico"; Flags: uninsdeletekey
Root: "HKCU"; Subkey: "Software\Classes\.flac\shell\QuickConvert\command"; ValueType: string; ValueData: "C:\Program Files\Quick Converter\QuickConvert.exe ""%1"""; Flags: uninsdeletekey
