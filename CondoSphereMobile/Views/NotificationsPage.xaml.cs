using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NotificationsViewModel vm &&
            vm.LoadNotificationsCommand.CanExecute(null))
        {
            vm.LoadNotificationsCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}
