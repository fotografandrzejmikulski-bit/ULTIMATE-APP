namespace UltimateApp.Application.Models;

/// <summary>Informacje o module aplikacji wyświetlanym w UI.</summary>
public record ModuleInfo(
    string Id,
    string Name,
    string Description,
    string IconGlyph
);
