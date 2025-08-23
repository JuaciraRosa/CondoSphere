namespace CondoSphereMobile.Views;

public partial class UnitsPage : ContentPage
{
	public UnitsPage()
	{
		InitializeComponent();
	}

    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}