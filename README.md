# ULTIMATE-APP

Windows Desktop App (WinUI 3, .NET 8, x64)

---

## Gdzie jest plik instalacyjny? / Where is the installer?

### 📦 Pobranie z GitHub Actions (zalecane)

Plik instalacyjny **`UltimateApp-v5-Setup-x64.exe`** jest budowany automatycznie przez CI i dostępny jako artefakt każdego przebiegu:

1. Przejdź do zakładki **Actions** tego repozytorium na GitHub.
2. Kliknij ostatni udany przebieg **"Build UltimateApp v5"**.
3. Na dole strony, w sekcji **Artifacts**, pobierz **`UltimateApp-v5-Setup-x64`**.
4. Rozpakuj ZIP – w środku znajdziesz `UltimateApp-v5-Setup-x64.exe`.

> Bezpośredni link do zakładki Actions:  
> `https://github.com/fotografandrzejmikulski-bit/ULTIMATE-APP/actions`
>  
> *(zastąp właściwą nazwą repozytorium jeśli je forkujesz)*

---

### 🛠️ Budowanie lokalnie (Windows)

Wymagania wstępne:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) ≥ 6.0.0 (tylko do tworzenia pliku `.exe`)

#### PowerShell

```powershell
# Zbuduj i uruchom testy
.\build.ps1

# Zbuduj + utwórz plik instalacyjny Setup.exe
.\build.ps1 -Installer
```

#### Wiersz poleceń (CMD)

```cmd
build.bat
build.bat --installer
```

Po zakończeniu plik instalacyjny pojawi się w:

```
artifacts\installer\UltimateApp-v5-Setup-x64.exe
```

---

## Struktura projektu

```
UltimateApp/
  installer/
    UltimateApp.iss          ← skrypt Inno Setup (źródło instalatora)
  src/
    UltimateApp.Application/ ← logika biznesowa
    UltimateApp.Infrastructure/ ← providery AI, model manager
    UltimateApp.Presentation/   ← WinUI 3 (GUI)
  tests/
    UltimateApp.Tests/       ← testy jednostkowe
```

## Artefakty CI

| Artefakt | Zawartość |
|---|---|
| `UltimateApp-win-x64` | Pliki aplikacji (framework-dependent, wymaga .NET 8) |
| `UltimateApp-v5-Setup-x64` | **Plik instalacyjny Setup.exe** (self-contained, nie wymaga .NET) |
