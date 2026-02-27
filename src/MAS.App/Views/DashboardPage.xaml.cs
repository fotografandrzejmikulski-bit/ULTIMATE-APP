using MAS.App.ViewModels;

namespace MAS.App.Views;

/// <summary>
/// Code-behind dashboardu MAS. Minimalna logika – cała logika w ViewModelu.
/// </summary>
public sealed partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DashboardViewModel vm && !vm.IsConnected)
            await vm.ConnectAsync().ConfigureAwait(false);
    }
}
