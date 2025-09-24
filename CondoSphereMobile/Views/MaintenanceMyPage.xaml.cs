using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class MaintenanceMyPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private List<CondominiumDto> _condos = new();

    public MaintenanceMyPage() => InitializeComponent();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api) _api = api;
    }

    // OnAppearing: troca GetMyMaintenanceAsync() por GetMaintenanceListAsync()
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // ✅ fallback DI
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        if (_api is null) return;

        // Lista (agora do endpoint /maintenance-requests/list)
        var (list, err) = await _api.GetMaintenanceListAsync();
        if (err != null) { ErrorLabel.IsVisible = true; ErrorLabel.Text = err; }
        else { ErrorLabel.IsVisible = false; ListView.ItemsSource = list?.OrderByDescending(x => x.SubmittedAt); }

        // Condos (mantém como já tens)
        var (condos, cErr) = await _api.GetCondominiumsAsync();
        _condos = condos?.ToList() ?? new List<CondominiumDto>();
        if (cErr != null && _condos.Count == 0)
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = cErr;
        }
    }

    // Botão "Novo" (OnNewClicked): troca CreateMyMaintenanceAsync(...) por CreateMaintenanceAsync(...)
    private async void OnNewClicked(object sender, EventArgs e)
    {
        if (_api is null) return;

        // escolhe condomínio (como já fazes)
        if (_condos.Count == 0)
        {
            await DisplayAlert("Aviso", "Não foi possível carregar os condomínios.", "OK");
            return;
        }

        string choice = null;
        if (_condos.Count == 1) choice = _condos[0].Id.ToString();
        else
        {
            var options = _condos.Select(c => c.Name ?? $"{c.Id}").ToArray();
            choice = await DisplayActionSheet("Condomínio", "Cancelar", null, options);
            if (string.IsNullOrWhiteSpace(choice)) return;
        }

        var condoId = _condos.First(c => (c.Name ?? $"{c.Id}") == choice).Id;

        var title = await DisplayPromptAsync("Novo pedido", "Título");
        if (string.IsNullOrWhiteSpace(title)) return;

        var desc = await DisplayPromptAsync("Detalhes", "Descreva o problema:");
        if (string.IsNullOrWhiteSpace(desc)) return;

        // chama o endpoint unificado POST /api/maintenance-requests
        var (created, err) = await _api.CreateMaintenanceAsync(title, desc, condoId);
        if (err != null) { await DisplayAlert("Erro", err, "OK"); return; }

        await DisplayAlert("Ok", "Pedido submetido.", "OK");

        // recarrega a lista da página
        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        var (list, err) = await _api.GetMaintenanceListAsync();
        if (err != null) { ErrorLabel.IsVisible = true; ErrorLabel.Text = err; return; }
        ErrorLabel.IsVisible = false;
        ListView.ItemsSource = list?.OrderByDescending(x => x.SubmittedAt);
    }

}