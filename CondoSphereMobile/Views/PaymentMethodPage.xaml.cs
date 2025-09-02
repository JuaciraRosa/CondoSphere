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
        try
        {
            var intent = await CreateIntentAsync();
            await DisplayAlert("Cartão", $"Intent: {intent.intentId}\nClientSecret: {intent.clientSecret}", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
    }

    private async void OnPayMbWay(object sender, EventArgs e)
    {
        try
        {
            var intent = await CreateIntentAsync();
            await DisplayAlert("MB Way", $"Pedido enviado. Ref: {intent.intentId}", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
    }

    private async void OnPayMultibanco(object sender, EventArgs e)
    {
        try
        {
            var intent = await CreateIntentAsync();
            await DisplayAlert("Multibanco", $"Entidade/Ref (simulado): {intent.intentId}", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
    }

    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();

    private record CreateIntentResp(string clientSecret, string intentId, string publishableKey);
}