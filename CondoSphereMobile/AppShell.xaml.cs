
using CondoSphereMobile.Services;
using CondoSphereMobile.Views;

namespace CondoSphereMobile
{
    public partial class AppShell : Shell
    {
        private bool _menuApplied;
        private readonly SessionService? _session;

        public AppShell()
        {
            InitializeComponent();
        }

        public AppShell(SessionService session) : this()  
        {
            _session = session;
           
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_menuApplied) return;
            _menuApplied = true;

            var role = await SecureStorage.GetAsync("user_role") ?? "";

            // Por padrão, mostra só Dashboard + Resident
            // Remove o que não se aplica
            if (role == "Administrator")
            {
                // Admin vê tudo (Manager + Admin + Resident se quiseres)
            }
            else if (role == "Manager")
            {
                // Manager não vê Admin
                if (Items.Contains(AdminFlyout)) Items.Remove(AdminFlyout);
            }
            else
            {
                // Resident não vê Manager nem Admin
                if (Items.Contains(ManagerFlyout)) Items.Remove(ManagerFlyout);
                if (Items.Contains(AdminFlyout)) Items.Remove(AdminFlyout);
            }

            // Define a página inicial (Dashboard)
            CurrentItem = DashboardFlyout;
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            SecureStorage.Remove("jwt_token");
            SecureStorage.Remove("user_role");
            SecureStorage.Remove("user_name");
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

    }
}
