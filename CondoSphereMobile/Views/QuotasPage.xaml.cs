namespace CondoSphereMobile.Views;

public partial class QuotasPage : ContentPage
{
	public QuotasPage()
	{
		InitializeComponent();
	}

    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}