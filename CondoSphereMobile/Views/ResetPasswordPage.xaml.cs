using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class ResetPasswordPage : ContentPage
{
    private readonly ApiService _api;

    public ResetPasswordPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(TokenEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Aviso", "Preencha todos os campos.", "OK");
            return;
        }

        var ok = await _api.ResetPasswordAsync(
            EmailEntry.Text, TokenEntry.Text, PasswordEntry.Text);

        await DisplayAlert(ok ? "Sucesso" : "Erro",
            ok ? "Senha redefinida com sucesso! Agora pode fazer login."
               : "Não foi possível redefinir a senha.",
            "OK");

        if (ok)
            Application.Current.MainPage = new NavigationPage(new LoginPage(_api));

    }
}