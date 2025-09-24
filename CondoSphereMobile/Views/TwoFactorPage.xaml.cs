using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;
public partial class TwoFactorPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private string _email = "";

    public TwoFactorPage()
    {
        InitializeComponent();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        //  pega a MESMA instância registrada no DI (singleton)
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
    }


    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var apiObj) && apiObj is ApiService api)
            _api = api;

        if (query.TryGetValue("Email", out var emailObj) && emailObj is string email)
            _email = email;
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        if (_api is null)
        {
            await DisplayAlert("Erro", "Serviço não inicializado.", "OK");
            return;
        }

        var code = CodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert("Aviso", "Digite o código 2FA.", "OK");
            return;
        }

        var (ok, token) = await _api.Login2FAAsync(_email, code);
        if (!ok)
        {
            await DisplayAlert("Erro", "Código inválido. Tente novamente.", "OK");
            return;
        }

        _api.SetToken(token);

        if (Shell.Current is AppShell sh) sh.BuildFlyout();
        await Shell.Current.GoToAsync("///root", true);
    }

}