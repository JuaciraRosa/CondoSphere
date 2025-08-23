namespace CondoSphereMobile.Views;

public partial class CondominiumsPage : ContentPage
{
	public CondominiumsPage()
	{
		InitializeComponent();
	}

    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}