; ============================================================
; Equipment Decharge Manager
; Inno Setup Installer
; ============================================================

#define MyAppName "Equipment Decharge Manager"
#define MyAppVersion "1.0"
#define MyAppPublisher "LKS"
#define MyAppExeName "EquipmentDechargeManager.exe"

#define PostgreSQLInstaller "postgresql-18.6-1-windows-x64.exe"
#define PostgreSQLPassword "Postgres123!"

[Setup]

AppId={{5CED7E4E-EA65-476F-A079-CA3BE5323C67}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}{#MyAppName}

OutputBaseFilename=EquipmentDechargeManager_Setup

PrivilegesRequired=admin

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

Compression=lzma
SolidCompression=yes

WizardStyle=modern dynamic

DisableProgramGroupPage=yes

[Languages]

Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]

Name: "desktopicon";
Description: "Create a desktop shortcut";
Flags: unchecked

[Files]

; ============================================================
; Application
; ============================================================

Source: "C:\Users\m_lak\OneDrive\Desktop\Equipment Decharge Manager\bin\Release\net10.0\win-x64\publish*";
DestDir: "{app}";
Flags: ignoreversion recursesubdirs createallsubdirs

; ============================================================
; PostgreSQL Installer
; ============================================================

Source: "C:\Users\m_lak\OneDrive\Desktop\Equipment Decharge Manager\installer{#PostgreSQLInstaller}";
DestDir: "{tmp}";
Flags: deleteafterinstall

[Icons]

; Start Menu
Name: "{autoprograms}{#MyAppName}";
Filename: "{app}{#MyAppExeName}"

; Desktop
Name: "{autodesktop}{#MyAppName}";
Filename: "{app}{#MyAppExeName}";
Tasks: desktopicon

[Run]

; ============================================================
; Install PostgreSQL
; ============================================================

Filename: "{tmp}{#PostgreSQLInstaller}";
Parameters: "--mode unattended --unattendedmodeui none --superpassword ""{#PostgreSQLPassword}"" --serverport 5432";
StatusMsg: "Installing PostgreSQL 18.6...";
Flags: waituntilterminated runhidden

; ============================================================
; Launch Application
; ============================================================

Filename: "{app}{#MyAppExeName}";
Description: "Launch Equipment Decharge Manager";
Flags: nowait postinstall skipifsilent

