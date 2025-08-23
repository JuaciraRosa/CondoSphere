namespace CondoSphereMobile.Views;

public partial class MeetingsPage : ContentPage
{
	public MeetingsPage()
	{
		InitializeComponent();
	}

    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}