using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    // DI
    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    // opcional, para XAML resolver via DI:
    public LoginPage()
       : this(Application.Current?.Handler?.MauiContext?.Services?.GetRequiredService<ApiService>())
    { }



    private bool _loggingIn;

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (_loggingIn) return;
        _loggingIn = true;

        try
        {
            ErrorLabel.IsVisible = false;

            var email = EmailEntry.Text?.Trim() ?? "";
            var pass  = PasswordEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                ErrorLabel.Text = "Preencha e-mail e senha.";
                ErrorLabel.IsVisible = true;
                return;
            }

            var (success, token, requires2FA, error) = await _api.LoginAsync(email, pass);
            if (!success)
            {
                ErrorLabel.Text = string.IsNullOrWhiteSpace(error) ? "Falha no login." : error;
                ErrorLabel.IsVisible = true;
                return;
            }

            if (requires2FA)
            {
                await Shell.Current.GoToAsync(nameof(TwoFactorPage), true,
                    new Dictionary<string, object> { { "Email", email } });
                return;
            }

            // fixa o Bearer no HttpClient desta instância (singleton)
            _api.SetToken(token);

            // constrói o menu e navega para a raiz
            if (Shell.Current is AppShell sh) sh.BuildFlyout();
            await Shell.Current.GoToAsync("///root", true);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Erro ao conectar: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally { _loggingIn = false; }
    }
}
