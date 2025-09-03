using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;
using System.Collections.ObjectModel;

namespace CondoSphereMobile.Views;



public partial class QuotasPage : ContentPage
{
    private readonly ApiService _api = new();
    public ObservableCollection<QuotaDto> Quotas { get; } = new();

    public QuotasPage()
    {
        InitializeComponent();
        BindingContext = this;

        List.ItemsSource = Quotas;
        Appearing += async (_, __) => await LoadQuotasAsync();
    }

    private bool _isLoading;

    private async Task LoadQuotasAsync()
    {
        if (_isLoading) return;

        try
        {
            _isLoading = true;

            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            List<QuotaDto> list;
            try
            {
                // “Minhas” quotas (precisa estar logado como Resident)
                var me = await _api.GetAsync<ResidentMeResp>("residents/me");
                list = me.Units
                    .SelectMany(u => u.Quotas)
                    .Select(q => new QuotaDto
                    {
                        Id = q.Id,
                        UnitId = q.UnitId,
                        UnitNumber = q.UnitNumber,
                        Amount = q.Amount,
                        DueDate = q.DueDate,
                        IsPaid = q.IsPaid
                    })
                    .ToList();
            }
            catch
            {
                // Fallback (ex.: Admin/Manager)
                list = await _api.GetAsync<List<QuotaDto>>("quotas/list");
            }

            Quotas.Clear();
            foreach (var q in list) Quotas.Add(q);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Abre a página de pagamento a partir do botão "Pagar"
    private async void OnPayClicked(object sender, EventArgs e) => await OpenPaymentAsync(sender);

   

    // Lógica comum para obter a Quota e navegar
    private async Task OpenPaymentAsync(object sender)
    {
        var quota =
            (sender as Button)?.CommandParameter as QuotaDto ??
            (sender as BindableObject)?.BindingContext as QuotaDto;

        if (quota == null)
        {
            await DisplayAlert("Atenção", "Quota inválida.", "OK");
            return;
        }

        if (quota.IsPaid)
        {
            await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK");
            return;
        }

        // Se usas Shell: await Shell.Current.Navigation.PushAsync(new PaymentMethodPage(quota));
        await Navigation.PushAsync(new PaymentMethodPage(quota));
    }

    // === Handlers referenciados no XAML ===

    // XAML: Clicked="OnReloadClicked"
    private async void OnReloadClicked(object sender, EventArgs e) =>
        await LoadQuotasAsync();

    // XAML: Clicked="OnDetailsClicked"
    // Suporta tanto CommandParameter="{Binding}" quanto acessar o BindingContext do botão
    private async void OnDetailsClicked(object sender, EventArgs e)
    {
        QuotaDto? quota =
            (sender as Button)?.CommandParameter as QuotaDto ??
            (sender as BindableObject)?.BindingContext as QuotaDto;

        if (quota == null) return;

        // Abre a tela de métodos de pagamento (se não usa, pode trocar por outra navegação)
        await Navigation.PushAsync(new PaymentMethodPage(quota));
    }

    // === helpers para desserializar residents/me ===
    private class ResidentMeResp { public List<UnitX> Units { get; set; } = new(); }
 

}