using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _api;

    public ForgotPasswordPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            await DisplayAlert("Aviso", "Digite um e-mail.", "OK");
            return;
        }

        var ok = await _api.ForgotPasswordAsync(EmailEntry.Text);
        await DisplayAlert(ok ? "Enviado" : "Erro",
            ok ? "Se o e-mail existir, você receberá instruções para redefinir sua senha."
               : "Não foi possível enviar o pedido.",
            "OK");
    }
}