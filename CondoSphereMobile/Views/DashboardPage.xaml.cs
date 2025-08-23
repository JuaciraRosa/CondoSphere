namespace CondoSphereMobile.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
     
    }
    private async void OnUsersClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///UsersPage");
    }

    private async void OnCompaniesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CompaniesPage");
    }

    private async void OnCondominiumsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CondominiumsPage");
    }

    private async void OnMeetingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MeetingsPage");
    }

    private async void OnExpensesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///ExpensesPage");
    }

    private async void OnUnitsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///UnitsPage");
    }

    private async void OnPaymentsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///PaymentsPage");
    }
}