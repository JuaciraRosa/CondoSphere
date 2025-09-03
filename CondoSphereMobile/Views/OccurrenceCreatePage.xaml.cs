using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class OccurrenceCreatePage : ContentPage
{
    private readonly ApiService _api = new();
    public event Func<Task>? Created;
    // listas em memória (para os Pickers)
    private List<CondoShort> _condos = new();
    private List<UnitOption> _allUnits = new();

    public OccurrenceCreatePage()
    {
        InitializeComponent();
        // carrega dropdowns quando a página abrir
        Appearing += async (_, __) => await LoadLookupsAsync();
    }

    // ====== carregamento dos dropdowns ======
    private async Task EnsureAuthAsync()
    {
        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);
    }
    private async Task LoadLookupsAsync()
    {
        try
        {
            await EnsureAuthAsync();

            // 1) condomínios
            _condos = await _api.GetAsync<List<CondoShort>>("condominiums");
            CondoPicker.ItemsSource = _condos;

            // 2) unidades do residente (se for Resident)
            _allUnits.Clear();
            try
            {
                var me = await _api.GetAsync<ResidentMeDto>("residents/me");
                if (!string.IsNullOrWhiteSpace(me.Email))
                    EmailEntry.Text = me.Email;

                foreach (var u in me.OwnedUnits)
                {
                    _allUnits.Add(new UnitOption
                    {
                        Id = u.Id,
                        CondominiumId = u.CondominiumId,
                        UnitNumber = u.UnitNumber
                    });
                }
            }
            catch
            {
                // se não for Resident, deixa sem unidades (ou podes implementar fallback depois)
            }

            // aplica filtro inicial (se já existir um condomínio pré-selecionado)
            ApplyUnitsFilter();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", "Não foi possível carregar os dados.\n" + ex.Message, "OK");
        }
    }

    // chamado pelo Picker (XAML já aponta para este handler)
    private void OnCondoChanged(object? sender, EventArgs e) => ApplyUnitsFilter();

    private void ApplyUnitsFilter()
    {
        var selected = CondoPicker.SelectedItem as CondoShort;
        if (selected == null)
        {
            UnitPicker.ItemsSource = _allUnits;
            UnitPicker.SelectedItem = null;
            return;
        }

        // filtra unidades pelo CondominiumId (quando houver)
        var filtered = _allUnits.Any(u => u.CondominiumId != 0)
            ? _allUnits.Where(u => u.CondominiumId == selected.Id).ToList()
            : _allUnits;

        UnitPicker.ItemsSource = filtered;
        UnitPicker.SelectedItem = filtered.FirstOrDefault();
    }

    // ====== criar ocorrência ======
    private async void OnCreateClicked(object sender, EventArgs e)
    {
        var selectedCondo = CondoPicker.SelectedItem as CondoShort;
        var selectedUnit = UnitPicker.SelectedItem as UnitOption;

        if (selectedCondo == null)
        {
            await DisplayAlert("Atenção", "Selecione o condomínio.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleEntry.Text) ||
            string.IsNullOrWhiteSpace(DescEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            await DisplayAlert("Atenção", "Preenche Título, Descrição e Email.", "OK");
            return;
        }

        try
        {
            await EnsureAuthAsync();

            var payload = new
            {
                CondominiumId = selectedCondo.Id,                // ? do dropdown
                UnitNumber = selectedUnit?.UnitNumber ?? "",  // ? do dropdown (opcional)
                Title = TitleEntry.Text?.Trim(),
                Description = DescEntry.Text?.Trim(),
                CreatedBy = EmailEntry.Text?.Trim()
            };

            await _api.PostAsync<object, object>("occurrences", payload);
            await DisplayAlert("Sucesso", "Ocorrência criada.", "OK");

            if (Created != null) await Created();
            await Navigation.PopAsync(); // volta à lista, se quiseres
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}