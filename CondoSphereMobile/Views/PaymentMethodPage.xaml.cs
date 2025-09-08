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

    private async void OnPayCard(object sender, EventArgs e)
    {
        if (_quota.IsPaid)
        {
            await DisplayAlert("Pagamento", "Esta quota já está paga.", "OK");
            return;
        }
        await Navigation.PushAsync(new PaymentCardPage(_quota));
    }

    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();
}
