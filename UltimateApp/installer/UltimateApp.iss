; ============================================================
; UltimateApp v5 – Inno Setup Installer Script
; ============================================================
; Budowanie z CI (absolute paths):
;   ISCC.exe /DSourceDir="C:\path\to\publish" /DOutputDir="C:\path\to\out" installer\UltimateApp.iss
;
; Budowanie lokalne (po wcześniejszym publish -r win-x64 --self-contained):
;   ISCC.exe installer\UltimateApp.iss
;   (domyślne ścieżki względne od lokalizacji pliku .iss)
; ============================================================

; --- Ścieżki (nadpisywalne z linii poleceń przez /DSourceDir=... /DOutputDir=...) ---
#ifndef SourceDir
  #define SourceDir "..\..\artifacts\UltimateApp-win-x64-selfcontained"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts\installer"
#endif

; --- Metadane aplikacji ---
#define AppName      "UltimateApp v5"
#define AppVersion   "5.0.0"
#define AppPublisher "Andrzej Mikulski"
#define AppURL       "https://github.com/fotografandrzejmikulski-bit/ULTIMATE-APP"
#define AppExeName   "UltimateApp.Presentation.exe"

[Setup]
; Unikalny GUID aplikacji (nie zmieniać po pierwszym wydaniu!)
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}

; Katalog instalacji i menu Start
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; Uprawnienia – wymagane do zapisu w Program Files
PrivilegesRequired=admin

; Wyjście
OutputDir={#OutputDir}
OutputBaseFilename=UltimateApp-v5-Setup-x64

; Kompresja
Compression=lzma2/ultra64
SolidCompression=yes

; Wygląd kreatora
WizardStyle=modern

; Wymagana architektura i wersja systemu
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.17763

; Ikona w panelu dodawania/usuwania programów
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "polish";  MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Cała zawartość katalogu publish (self-contained, rekurencyjnie)
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";                        Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}";  Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";                  Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent

[Registry]
Root: HKLM; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
  ValueType: string; ValueName: "Version";     ValueData: "{#AppVersion}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
  ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"

[Code]
{ Sprawdzenie minimalnej wersji systemu Windows (1809, build 17763) }
function InitializeSetup(): Boolean;
var
  BuildStr: String;
  Build:    Integer;
begin
  Result := True;
  if RegQueryStringValue(HKLM,
      'SOFTWARE\Microsoft\Windows NT\CurrentVersion',
      'CurrentBuildNumber', BuildStr) then
  begin
    Build := StrToIntDef(BuildStr, 0);
    if Build < 17763 then
    begin
      MsgBox(
        'UltimateApp v5 wymaga systemu Windows 10 (wersja 1809, kompilacja 17763) lub nowszego.' +
        #13#10 + 'Wykryta kompilacja: ' + BuildStr,
        mbError, MB_OK);
      Result := False;
    end;
  end;
end;
