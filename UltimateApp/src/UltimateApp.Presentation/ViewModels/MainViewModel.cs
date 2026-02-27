using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UltimateApp.Presentation.ViewModels;

/// <summary>ViewModel dla głównego okna – lista 8 modułów aplikacji.</summary>
public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<ModuleItemViewModel> Modules { get; } = new();

    public MainViewModel()
    {
        InitializeModules();
    }

    private void InitializeModules()
    {
        var modules = new[]
        {
            new ModuleItemViewModel("asystent",      "Asystent AI",             "Inteligentny asystent oparty o GGUF i Gemini",    "\uE8D4"),
            new ModuleItemViewModel("edytor",        "Edytor dokumentów",       "Tworzenie i edycja dokumentów z pomocą AI",       "\uE8A5"),
            new ModuleItemViewModel("analiza",       "Analiza danych",          "Analiza i wizualizacja zbiorów danych",           "\uE9D2"),
            new ModuleItemViewModel("zdjecia",       "Obróbka zdjec",           "Edycja i analiza fotografii",                     "\uEB9F"),
            new ModuleItemViewModel("kod",           "Generator kodu",          "Generowanie i refaktoring kodu zródlowego",       "\uE943"),
            new ModuleItemViewModel("tlumacz",       "Tłumacz",                 "Tłumaczenie tekstu i dokumentów",                 "\uF2B7"),
            new ModuleItemViewModel("modele",        "Menedzer modeli",         "Pobieranie i zarządzanie modelami GGUF",          "\uE896"),
            new ModuleItemViewModel("ustawienia",    "Ustawienia",              "Konfiguracja aplikacji i dostawców AI",           "\uE713"),
        };

        foreach (var m in modules)
            Modules.Add(m);
    }
}

/// <summary>Element listy modułów.</summary>
public sealed class ModuleItemViewModel
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string IconGlyph { get; }

    public ModuleItemViewModel(string id, string name, string description, string iconGlyph)
    {
        Id = id;
        Name = name;
        Description = description;
        IconGlyph = iconGlyph;
    }
}
