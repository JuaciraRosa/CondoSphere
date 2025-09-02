using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class NotificationsPage : ContentPage
{
 

    public NotificationsPage()
    {
        InitializeComponent();
        Appearing += (_, __) =>
        {
            DisplayAlert("Info", "Ainda não há endpoint para listar notificações. Esta página apenas ilustra o layout.", "OK");
        };
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}