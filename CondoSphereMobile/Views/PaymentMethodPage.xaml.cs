using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class PaymentMethodPage : ContentPage
{
    private readonly ApiService _api = new();
    private readonly QuotaDto _quota;

    public PaymentMethodPage(QuotaDto quota)
    {
        InitializeComponent();
        _quota = quota;
        QuotaLabel.Text = $"Quota #{quota.Id} — {quota.AmountPt} (vence {quota.DueDateStr})";
    }

    private async Task<CreateIntentResp> CreateIntentAsync()
    {
        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

        var payload = new { QuotaId = _quota.Id };
        return await _api.PostAsync<object, CreateIntentResp>("payments/card/intent", payload);
    }

    private async void OnPayCard(object sender, EventArgs e)
    {
        if (_quota.IsPaid)
        {
            await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK");
            return;
        }
        await Navigation.PushAsync(new PaymentCardPage(_quota));
    }

    private async void OnPayMbWay(object sender, EventArgs e)
    {
        if (_quota.IsPaid) { await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK"); return; }

        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

        try
        {
            var resp = await _api.PostAsync<object, MbWayResp>("payments/mbway/request", new { QuotaId = _quota.Id, Phone = "9XXXXXXXX" });
            await DisplayAlert("MB Way", $"Pedido enviado.\nRef: {resp.Reference}\nEstado: {resp.Status}", "OK");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("already paid", StringComparison.OrdinalIgnoreCase))
                await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK");
            else
                await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    private async void OnPayMultibanco(object sender, EventArgs e)
    {
        if (_quota.IsPaid) { await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK"); return; }

        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

        try
        {
            var resp = await _api.PostAsync<object, MultibancoResp>("payments/multibanco/reference", new { QuotaId = _quota.Id });
            await DisplayAlert("Multibanco", $"Entidade: {resp.Entity}\nReferência: {resp.Reference}\nValor: {resp.AmountPt}", "OK");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("already paid", StringComparison.OrdinalIgnoreCase))
                await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK");
            else
                await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    private record MbWayResp(string Reference, string Status);
    private record MultibancoResp(string Entity, string Reference, string AmountPt);


    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();

    private record CreateIntentResp(string clientSecret, string intentId, string publishableKey);
}