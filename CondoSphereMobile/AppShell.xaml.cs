
using CondoSphereMobile.Services;
using CondoSphereMobile.Views;

namespace CondoSphereMobile
{
    public partial class AppShell : Shell
    {
        bool _menuApplied;

        public AppShell()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_menuApplied) return;
            _menuApplied = true;

            var role = await SecureStorage.GetAsync("user_role") ?? "";

            // exemplo simples:
            if (role == "Administrator")
            {
                // vê tudo
            }
            else if (role == "Manager")
            {
                if (Items.Contains(AdminFlyout)) Items.Remove(AdminFlyout);
            }
            else
            {
                if (Items.Contains(ManagerFlyout)) Items.Remove(ManagerFlyout);
                if (Items.Contains(AdminFlyout)) Items.Remove(AdminFlyout);
            }

            CurrentItem = DashboardFlyout;
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                SecureStorage.Remove("jwt_token");
                SecureStorage.Remove("user_role");
                SecureStorage.Remove("user_fullname");
            }
            catch { /* ignore */ }

            // volta para o login
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }

}
