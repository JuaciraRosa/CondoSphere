using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class QuotasPage : ContentPage
{
    private readonly ApiService _api = new();

    public QuotasPage()
    {
        InitializeComponent();
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Busy.IsRunning = Busy.IsVisible = true;
            var data = await _api.GetAsync<List<QuotaDto>>("quotas/mine"); // <-- ajusta se necessário
            List.ItemsSource = data.OrderByDescending(q => q.DueDate).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
        finally
        {
            Busy.IsRunning = Busy.IsVisible = false;
        }
    }

    private async void OnReloadClicked(object sender, EventArgs e) => await LoadAsync();

    private async void OnDetailsClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not QuotaDto q) return;
        await DisplayAlert("Quota", $"Unidade: {q.UnitNumber}\nData: {q.DueDateStr}\nValor: {q.AmountPt}\nPago: {q.IsPaid}", "OK");
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not QuotaDto q) return;

        // Abre o fluxo de pagamento já existente no MVC (Stripe.js) dentro de um WebView modal
        var url = $"https://condosphere-web-app.somee.com/Quotas/Pay/{q.Id}";
        await Navigation.PushModalAsync(new WebViewPage(url));
    }
}