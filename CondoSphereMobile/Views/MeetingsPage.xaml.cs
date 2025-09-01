using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class MeetingsPage : ContentPage
{
    public MeetingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MeetingsViewModel vm &&
            vm.LoadMeetingsCommand.CanExecute(null))
        {
            vm.LoadMeetingsCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}