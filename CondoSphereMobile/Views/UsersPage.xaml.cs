using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class UsersPage : ContentPage
{
    public UsersPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is UsersViewModel vm &&
            vm.LoadUsersCommand.CanExecute(null))
        {
            vm.LoadUsersCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}