using CondoSphereMobile.Services;
using CondoSphereMobile.Views;

namespace CondoSphereMobile
{
   
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Placeholder para não travar a UI
            MainPage = new ContentPage { Content = new ActivityIndicator { IsRunning = true, IsVisible = true } };

            // Decide a tela inicial sem bloquear o construtor
            Dispatcher.Dispatch(async () => await DecideLandingPageAsync());
        }

        private async Task DecideLandingPageAsync()
        {
            try
            {
                // se não marcou "remember me", remove token salvo
                var remember = Preferences.Get("remember_me", false);
                if (!remember)
                {
                    try { SecureStorage.Remove("jwt_token"); } catch { /* ignore */ }
                }

                var token = await SecureStorage.GetAsync("jwt_token");

                if (!string.IsNullOrWhiteSpace(token))
                {
                    // tem token: abre o Shell
                    MainPage = new AppShell();
                }
                else
                {
                    // sem token: vai para Login
                    MainPage = new NavigationPage(new LoginPage());
                }
            }
            catch
            {
                MainPage = new NavigationPage(new LoginPage());
            }
        }
    }


}
