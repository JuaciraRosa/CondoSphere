using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _api = new();

    public ForgotPasswordPage()
    {
        InitializeComponent();
        // pré-preenche com o email lembrado, se existir
        EmailEntry.Text = Preferences.Get("saved_email", "");
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Atenção", "Informe o email.", "OK");
            return;
        }

        try
        {
            // Ajuste a rota se no teu backend for diferente
            await _api.PostAsync<object, object>("auth/forgot-password", new { Email = email });
            await DisplayAlert("Ok", "Se o email existir, enviámos instruções para redefinir a senha.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            // Mesmo erro tratamos como mensagem genérica para segurança
            await DisplayAlert("Ok", "Se o email existir, enviámos instruções para redefinir a senha.", "OK");
        }
    }
}