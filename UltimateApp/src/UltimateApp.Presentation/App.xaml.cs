using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using UltimateApp.Application.Contracts;
using UltimateApp.Application.Models;
using UltimateApp.Application.Orchestration;
using UltimateApp.Infrastructure.AI;
using UltimateApp.Infrastructure.Models;
using UltimateApp.Infrastructure.Secrets;
using UltimateApp.Presentation.ViewModels;

namespace UltimateApp.Presentation;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private Window? _window;

    public App()
    {
        InitializeComponent();
        Services = BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddHttpClient();

        services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();

        services.AddTransient<GgufProvider>(sp =>
            new GgufProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("gguf"),
                sp.GetRequiredService<ILogger<GgufProvider>>()));

        services.AddTransient<OllamaProvider>(sp =>
            new OllamaProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama"),
                sp.GetRequiredService<ILogger<OllamaProvider>>()));

        services.AddTransient<GeminiProvider>(sp =>
            new GeminiProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("gemini"),
                sp.GetRequiredService<ILogger<GeminiProvider>>(),
                sp.GetRequiredService<ISecretProvider>()));

        services.AddSingleton<AIOrchestrator>(sp =>
        {
            var providers = new IAIProvider[]
            {
                sp.GetRequiredService<GgufProvider>(),
                sp.GetRequiredService<OllamaProvider>(),
                sp.GetRequiredService<GeminiProvider>()
            };
            return new AIOrchestrator(providers, sp.GetRequiredService<ILogger<AIOrchestrator>>());
        });

        services.AddSingleton<IModelManager>(sp =>
        {
            var llm = new ModelInfo("LLM", "https://example.com/llm.gguf", "llm.gguf");
            var spec = new ModelInfo("Specialist", "https://example.com/specialist.gguf", "specialist.gguf");
            return new ModelManager(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("models"),
                sp.GetRequiredService<ILogger<ModelManager>>(),
                llm, spec);
        });

        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider();
    }
}
