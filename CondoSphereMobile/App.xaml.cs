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
        }

        private async Task StartAsync()
        {
            await _session.LoadAsync();
            if (_session.IsAuthenticated)
                Application.Current.MainPage = new AppShell(_session);
        }
    }

}
