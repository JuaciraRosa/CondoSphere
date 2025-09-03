using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class MeetingDocumentsPage : ContentPage
{
    private readonly CondoSphereMobile.ViewModels.MeetingDocumentsViewModel _vm = new();

    public MeetingDocumentsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var api = new ApiService();
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) api.SetAuthToken(token);

            int condominiumId = 0;

            // 1) Tenta como Resident (mais correto)
            try
            {
                var me = await api.GetAsync<ResidentMeDto>("residents/me");
                condominiumId = me.OwnedUnits.FirstOrDefault()?.CondominiumId ?? 0;
            }
            catch
            {
                // 2) Se não for Resident, pega o primeiro condomínio disponível
                var condos = await api.GetAsync<List<CondoShort>>("condominiums");
                condominiumId = condos.FirstOrDefault()?.Id ?? 0;
            }

            if (condominiumId == 0)
            {
                await DisplayAlert("Info", "Nenhum condomínio encontrado para este utilizador.", "OK");
                return;
            }

            await _vm.LoadAsync(condominiumId); // <- chama o VM com número real
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}