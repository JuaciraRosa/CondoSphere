namespace CondoSphereMobile.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
     
    }
    private async void OnUsersClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(UsersPage));
    }

    private async void OnCompaniesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CompaniesPage));
    }

    private async void OnCondominiumsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CondominiumsPage));
    }

    private async void OnMeetingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MeetingsPage));
    }

    private async void OnExpensesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ExpensesPage));
    }

    private async void OnUnitsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(UnitsPage));
    }

    private async void OnPaymentsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PaymentsPage));
    }
}