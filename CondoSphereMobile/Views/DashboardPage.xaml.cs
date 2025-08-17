namespace CondoSphereMobile.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        LoadDashboard();
    }

    private async void LoadDashboard()
    {
        var name = await SecureStorage.GetAsync("user_name");
        var role = await SecureStorage.GetAsync("user_role");

        lblUserName.Text = name ?? "User";
        lblRole.Text = role ?? "No role assigned";

        menuContainer.Children.Clear();

        if (role == "Administrator")
        {
            menuContainer.Children.Add(CreateMenuButton("Manage Users", async () =>
            {
                await DisplayAlert("Action", "Open user management page", "OK");
            }));

            menuContainer.Children.Add(CreateMenuButton("Manage Companies", async () =>
            {
                await DisplayAlert("Action", "Open company management page", "OK");
            }));
        }

        if (role == "Administrator" || role == "Manager")
        {
            menuContainer.Children.Add(CreateMenuButton("Condominiums", async () =>
            {
                await DisplayAlert("Action", "Open condominiums page", "OK");
            }));

            menuContainer.Children.Add(CreateMenuButton("Expenses", async () =>
            {
                await DisplayAlert("Action", "Open expenses page", "OK");
            }));
        }

        if (role == "Resident")
        {
            menuContainer.Children.Add(CreateMenuButton("My Unit", async () =>
            {
                await DisplayAlert("Action", "Open my unit page", "OK");
            }));

            menuContainer.Children.Add(CreateMenuButton("Payments", async () =>
            {
                await DisplayAlert("Action", "Open payments page", "OK");
            }));
        }
    }

    private Button CreateMenuButton(string text, Func<Task> onClick)
    {
        return new Button
        {
            Text = text,
            HeightRequest = 50,
            CornerRadius = 10,
            BackgroundColor = Colors.DarkBlue,
            TextColor = Colors.White,
            Command = new Command(async () => await onClick())
        };
    }
}