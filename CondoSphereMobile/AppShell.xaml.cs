




using CondoSphereMobile.Services;
using CondoSphereMobile.Views;

namespace CondoSphereMobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rotas
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(AccountPage), typeof(AccountPage));
            Routing.RegisterRoute(nameof(Setup2FAPage), typeof(Setup2FAPage));
            Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));
            Routing.RegisterRoute(nameof(TwoFactorPage), typeof(TwoFactorPage));
            Routing.RegisterRoute(nameof(ChatListPage), typeof(ChatListPage));
            Routing.RegisterRoute(nameof(ChatThreadPage), typeof(ChatThreadPage));
            Routing.RegisterRoute(nameof(CondominiumsPage), typeof(CondominiumsPage));
            Routing.RegisterRoute(nameof(QuotasPage), typeof(QuotasPage));
            Routing.RegisterRoute(nameof(MaintenanceMyPage), typeof(MaintenanceMyPage));
            Routing.RegisterRoute(nameof(OccurrencesPage), typeof(OccurrencesPage));
            Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));


            FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        private bool _built;
        public void BuildFlyout()
        {
            if (_built) return;
            _built = true;

            FlyoutBehavior = FlyoutBehavior.Flyout;
            Items.Clear();

            Items.Add(new FlyoutItem
            {
                Title = "Perfil",
                Route = "root",
                Items =
                {
                    new ShellContent
                    {
                        Route = nameof(ProfilePage),
                        ContentTemplate = new DataTemplate(typeof(ProfilePage))
                    }
                }
            });

            Items.Add(new MenuItem
            {
                Text = "Minha Conta",
                IconImageSource = "settings.png",
                Command = new Command(async () =>
                {
                    await GoToAsync(nameof(AccountPage), true);
                })
            });

            Items.Add(new MenuItem
            {
                Text = "Suporte (Chat)",
                IconImageSource = "chat.png",
                Command = new Command(async () =>
                {
                    await GoToAsync(nameof(ChatListPage), true);
                })
            });

            Items.Add(new MenuItem
            {
                Text = "Condomínios",
                Command = new Command(async () =>
                {
                    await GoToAsync(nameof(CondominiumsPage), true);
                })
            });

            Items.Add(new MenuItem
            {
                Text = "Quotas",
                Command = new Command(async () =>
                {
                    await GoToAsync(nameof(QuotasPage), true);
                })
            });

            Items.Add(new MenuItem
            {
                Text = "Manutenções",
                Command = new Command(async () =>
                {
                    await GoToAsync(nameof(MaintenanceMyPage), true);
                })
            });

            Items.Add(new MenuItem
            {
                Text = "Ocorrências",
                Command = new Command(async () =>
                {
                    await GoToAsync(nameof(OccurrencesPage), true);
                })
            });
            Items.Add(new MenuItem
            {
                Text = "Sair",
                IconImageSource = "logout.png",
                Command = new Command(async () =>
                {
                    try
                    {
                        var sp = Application.Current?.Handler?.MauiContext?.Services;
                        var api = sp?.GetService<ApiService>();
                        api?.ClearToken();

                        Application.Current.MainPage = new AppShell();
                        await Task.Delay(50);
                        await (Application.Current.MainPage as Shell)!.GoToAsync(nameof(LoginPage));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Logout erro: {ex}");
                    }
                })
            });

        }
    }
}
