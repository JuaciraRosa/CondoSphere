using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;
public partial class AccountPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;

    public AccountPage() // ? Shell precisa desse
    {
        InitializeComponent();
    }

    // ? Mantém seu construtor (navegação manual, se usar)
    public AccountPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        _ = RefreshTwoFaUIAsync();
    }

    private async Task RefreshTwoFaUIAsync()
    {
        if (_api is null)
        {
            TwoFaStatusLabel.Text = "Erro: serviço não inicializado.";
            Activate2FAButton.IsEnabled = false;
            Disable2FAButton.IsEnabled = false;
            return;
        }

        TwoFaStatusLabel.Text = "Verificando 2FA...";
        Activate2FAButton.IsEnabled = false;
        Disable2FAButton.IsEnabled = false;

        var (enabled, error) = await _api.GetTwoFactorStatusAsync();

        if (enabled is null)
        {
            TwoFaStatusLabel.Text = $"Erro ao obter estado do 2FA: {error}";
            return;
        }

        if (enabled.Value)
        {
            TwoFaStatusLabel.Text = "2FA: ATIVO";
            Activate2FAButton.IsVisible = false;
            Disable2FAButton.IsVisible = true;
            Disable2FAButton.IsEnabled = true;
        }
        else
        {
            TwoFaStatusLabel.Text = "2FA: DESATIVADO";
            Activate2FAButton.IsVisible = true;
            Activate2FAButton.IsEnabled = true;
            Disable2FAButton.IsVisible = false;
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api)
            _api = api;
        //else if (Shell.Current is AppShell s && s.CurrentApi is not null)
        //    _api = s.CurrentApi;
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        if (_api is null) { await DisplayAlert("Erro", "Serviço não inicializado.", "OK"); return; }

        var current = CurrentPasswordEntry.Text;
        var newPass = NewPasswordEntry.Text;
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPass))
        {
            await DisplayAlert("Erro", "Preencha todos os campos.", "OK");
            return;
        }

        var ok = await _api.ChangePasswordAsync(current, newPass);
        if (!ok)
        {
            await DisplayAlert("Erro", "Não foi possível alterar a senha.", "OK");
            return;
        }

        await DisplayAlert("Sucesso", "Senha alterada. Faça login novamente.", "OK");
        _api.ClearToken();


        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private async void OnSetup2FAClicked(object sender, EventArgs e)
    {
        if (_api is null) { await DisplayAlert("Erro", "Serviço não inicializado.", "OK"); return; }
        await Shell.Current.GoToAsync(nameof(Setup2FAPage), true,
            new Dictionary<string, object> { { "ApiService", _api } });
    }

    private async void OnDisable2FAClicked(object sender, EventArgs e)
    {
        if (_api is null) { await DisplayAlert("Erro", "Serviço não inicializado.", "OK"); return; }
        var ok = await _api.Disable2FAAsync();
        await DisplayAlert(ok ? "Sucesso" : "Erro",
                           ok ? "2FA desativado." : "Não foi possível desativar o 2FA.",
                           "OK");
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_api is null) { await DisplayAlert("Erro", "Serviço não inicializado.", "OK"); return; }

        var confirm = await DisplayAlert("Confirmação",
            "Deseja desativar sua conta? Você não poderá entrar até ser reativada.",
            "Sim", "Não");
        if (!confirm) return;

        var ok = await _api.DeactivateAccountAsync();
        if (ok)
        {
            await DisplayAlert("Conta desativada", "Sua conta foi desativada.", "OK");
            Application.Current.MainPage = new AppShell(); // volta ao login
        }
        else
        {
            await DisplayAlert("Erro", "Não foi possível desativar a conta.", "OK");
        }
        await RefreshTwoFaUIAsync(); 
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_api is null) { await DisplayAlert("Erro", "Serviço não inicializado.", "OK"); return; }

        var confirm = await DisplayAlert("Confirmação",
            "Tem certeza que deseja APAGAR sua conta? Esta ação é irreversível.",
            "Sim", "Não");
        if (!confirm) return;

        var ok = await _api.DeleteAccountAsync();
        if (ok)
        {
            await DisplayAlert("Conta apagada", "Sua conta foi removida do sistema.", "OK");
            Application.Current.MainPage = new AppShell(); // volta ao login
        }
        else
        {
            await DisplayAlert("Erro", "Não foi possível apagar a conta.", "OK");
        }
    }
}