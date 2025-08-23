namespace CondoSphereMobile.Views;

public partial class ExpensesPage : ContentPage
{
	public ExpensesPage()
	{
		InitializeComponent();
	}


    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}