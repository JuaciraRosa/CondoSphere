using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class Setup2FAPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private bool _loaded;

    public Setup2FAPage()
    {
        InitializeComponent();

        // ?? garante que o botão “voltar” funcione mesmo se não houver stack
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
        });
    }

    // (mantenha seu construtor com ApiService se você usa navegação manual)
    public Setup2FAPage(ApiService api) : this()
    {
        _api = api;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var apiObj) && apiObj is ApiService api)
            _api = api;
    }

    // ?? fallback: se a página abriu antes do _api chegar, carrega aqui
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded) _ = LoadSetup();
    }

    private async Task LoadSetup()
    {
        if (_api is null) return;
        _loaded = true;

        var setup = await _api.Setup2FAAsync();
        if (setup == null)
        {
            await DisplayAlert("Erro", "Não foi possível carregar o setup do 2FA (verifique login/permite 2FA).", "OK");
            return;
        }

        // aceita base64 puro ou data URL
        var b64 = setup.QrCodeBase64;
        if (!string.IsNullOrWhiteSpace(b64) &&
            b64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            b64 = b64[(b64.IndexOf(',') + 1)..];

        try
        {
            var bytes = Convert.FromBase64String(b64);
            QrImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (FormatException)
        {
            await DisplayAlert("Erro", "QR inválido recebido da API.", "OK");
        }

        KeyLabel.Text = setup.Key ?? "";
       

    }

    private async void OnEnableClicked(object sender, EventArgs e)
    {
        if (_api is null) { await DisplayAlert("Erro", "Serviço não inicializado.", "OK"); return; }

        var code = CodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert("Aviso", "Digite o código do app Authenticator.", "OK");
            return;
        }

        var (ok, recovery) = await _api.Enable2FAAsync(code);
        if (!ok)
        {
            await DisplayAlert("Erro", "Código inválido. Tente novamente.", "OK");
            return;
        }

        // opcional: mostrar códigos
        if (recovery?.Length > 0)
            await DisplayAlert("Códigos de recuperação", string.Join(Environment.NewLine, recovery), "OK");

        await DisplayAlert("2FA", "Ativado com sucesso.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}
