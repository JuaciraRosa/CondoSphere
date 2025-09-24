using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class OccurrencesPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private List<CondominiumDto> _condos = new();
    private List<string> _units = new();

    public OccurrencesPage() => InitializeComponent();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api) _api = api;
        if (query.TryGetValue("CondoId", out var cid) && cid is int id)
            _presetCondoId = id;
    }
    private int? _presetCondoId;

    protected override async void OnAppearing()
    {

        base.OnAppearing();

      
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        if (_api is null) return;


        var (list, err) = await _api.GetOccurrencesAsync(_presetCondoId);
        if (err != null) { ErrorLabel.IsVisible = true; ErrorLabel.Text = err; }
        else { ErrorLabel.IsVisible = false; ListView.ItemsSource = list?.OrderByDescending(o => o.CreatedAt); }
    }

    // --------- Abrir overlay -----------
    private async void OnNewClicked(object sender, EventArgs e)
    {
        if (_api is null) return;

        // limpar campos
        TitleEntry.Text = "";
        DescEditor.Text = "";
        EmailEntry.Text = "";
        CreateCondoPicker.Items.Clear();
        CreateUnitPicker.Items.Clear();
        CreateUnitPicker.IsEnabled = false;

        // carregar condomínios quando o painel abre
        var (list, err) = await _api.GetCondominiumsAsync();
        if (list == null || err != null)
        {
            await DisplayAlert("Erro", err ?? "Não foi possível carregar condomínios.", "OK");
            return;
        }
        _condos = list.ToList();
        foreach (var c in _condos) CreateCondoPicker.Items.Add(c.Name ?? $"#{c.Id}");

        // se veio um condo predefinido, pré-seleciona
        if (_presetCondoId.HasValue)
        {
            var idx = _condos.FindIndex(c => c.Id == _presetCondoId.Value);
            if (idx >= 0) CreateCondoPicker.SelectedIndex = idx;
        }

        CreateOverlay.IsVisible = true;
    }

    // quando muda o condomínio dentro do overlay
    private async void OnCreateCondoChanged(object? sender, EventArgs e)
    {
        if (_api is null) return;

        CreateUnitPicker.Items.Clear();
        CreateUnitPicker.IsEnabled = false;

        var idx = CreateCondoPicker.SelectedIndex;
        if (idx < 0) return;

        var condo = _condos[idx];
        var (units, err) = await _api.GetUnitNumbersAsync(condo.Id);
        if (units == null || err != null)
        {
            await DisplayAlert("Erro", err ?? "Não foi possível carregar unidades.", "OK");
            return;
        }

        _units = units.ToList();
        foreach (var u in _units) CreateUnitPicker.Items.Add(u);
        CreateUnitPicker.IsEnabled = _units.Count > 0;
        if (_units.Count == 1) CreateUnitPicker.SelectedIndex = 0;
    }

    private void OnCreateCancel(object? s, EventArgs e) => CreateOverlay.IsVisible = false;

    private async void OnCreateOk(object? s, EventArgs e)
    {
        if (_api is null) return;

        // validações simples
        var cIdx = CreateCondoPicker.SelectedIndex;
        if (cIdx < 0) { await DisplayAlert("Aviso", "Selecione o condomínio.", "OK"); return; }
        var uIdx = CreateUnitPicker.SelectedIndex;
        if (uIdx < 0) { await DisplayAlert("Aviso", "Selecione a unidade.", "OK"); return; }

        var title = (TitleEntry.Text ?? "").Trim();
        var desc = (DescEditor.Text ?? "").Trim();
        var email = (EmailEntry.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(title)) { await DisplayAlert("Aviso", "Indique o título.", "OK"); return; }
        if (string.IsNullOrWhiteSpace(desc)) { await DisplayAlert("Aviso", "Descreva a ocorrência.", "OK"); return; }
        if (string.IsNullOrWhiteSpace(email)) { await DisplayAlert("Aviso", "Indique o email.", "OK"); return; }

        var condoId = _condos[cIdx].Id;
        var unit = _units[uIdx];

        var (created, err) = await _api.CreateOccurrenceAsync(title, desc, condoId, unit, email);
        if (err != null) { await DisplayAlert("Erro", err, "OK"); return; }

        CreateOverlay.IsVisible = false;
        await DisplayAlert("Ok", "Ocorrência criada.", "OK");

        // recarrega lista
        var (list, loadErr) = await _api.GetOccurrencesAsync(_presetCondoId);
        if (loadErr != null) { ErrorLabel.IsVisible = true; ErrorLabel.Text = loadErr; }
        else { ErrorLabel.IsVisible = false; ListView.ItemsSource = list?.OrderByDescending(o => o.CreatedAt); }
    }
}