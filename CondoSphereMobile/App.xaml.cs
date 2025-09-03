using CondoSphereMobile.Services;
using CondoSphereMobile.Views;

namespace CondoSphereMobile
{
    // App.xaml.cs
    public partial class App : Application
    {
        private readonly SessionService _session;

        public App(SessionService session)
        {
            InitializeComponent();
            _session = session;
            MainPage = new NavigationPage(new LoginPage()); // arranque

            // carrega sessão armazenada e decide o shell
            _ = StartAsync();
            _ = ClearTokenIfNotRememberAsync();
        }

        private async Task StartAsync()
        {
            await _session.LoadAsync();
            if (_session.IsAuthenticated)
                Application.Current.MainPage = new AppShell(_session);
        }

        private static async Task ClearTokenIfNotRememberAsync()
        {
            if (!Preferences.Get("remember_me", false))
            {
                try { SecureStorage.Remove("jwt_token"); } catch { }
            }
            await Task.CompletedTask;
        }
    }

}
