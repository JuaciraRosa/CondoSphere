namespace CondoSphereMobile.Views;

public partial class NotificationsPage : ContentPage
{
	public NotificationsPage()
	{
		InitializeComponent();
	}

    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}