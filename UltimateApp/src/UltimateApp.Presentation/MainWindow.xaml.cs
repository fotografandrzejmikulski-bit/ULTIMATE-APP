using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UltimateApp.Presentation.ViewModels;

namespace UltimateApp.Presentation;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
        Title = "UltimateApp v5";
    }
}
