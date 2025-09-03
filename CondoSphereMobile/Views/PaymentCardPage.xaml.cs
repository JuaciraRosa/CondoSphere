using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using System.Web;

namespace CondoSphereMobile.Views;

public partial class PaymentCardPage : ContentPage
{
    private readonly ApiService _api = new();
    private readonly QuotaDto _quota;

    public PaymentCardPage(QuotaDto quota)
    {
        InitializeComponent();
        _quota = quota;
        Appearing += async (_, __) => await LoadCheckoutAsync();
    }

    private async Task LoadCheckoutAsync()
    {
        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

        // Cria/obtém a intent para esta quota
        var intent = await _api.PostAsync<object, CreateIntentResp>(
            "payments/card/intent", new { QuotaId = _quota.Id });

        // HTML minimalista com Stripe.js + Card Element
        var html = $@"
<!DOCTYPE html>
<html><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<script src='https://js.stripe.com/v3'></script>
<style>
body {{ font-family: -apple-system, Roboto, Arial; padding:16px; }}
#card-element {{ padding:12px; border:1px solid #ccc; border-radius:8px; }}
button {{ margin-top:16px; padding:12px 16px; border-radius:8px; }}
#msg {{ margin-top:12px; color:#444; }}
</style></head>
<body>
<h3>Pagamento da quota #{_quota.Id}</h3>
<div id='card-element'></div>
<button id='pay'>Pagar</button>
<div id='msg'></div>
<script>
const stripe = Stripe('{intent.publishableKey}');
const elements = stripe.elements();
const card = elements.create('card');
card.mount('#card-element');

function setMsg(t){{document.getElementById('msg').innerText = t;}}

document.getElementById('pay').addEventListener('click', async () => {{
  setMsg('Processando...');
  try {{
    const result = await stripe.confirmCardPayment('{intent.clientSecret}', {{
      payment_method: {{ card: card }}
    }});
    if (result.error) {{
      setMsg('Erro: ' + result.error.message);
    }} else {{
      if (result.paymentIntent.status === 'succeeded') {{
        // comunica de volta ao app via URL custom
        window.location = 'app://paid?status=succeeded&pi=' + encodeURIComponent(result.paymentIntent.id);
      }} else {{
        setMsg('Estado: ' + result.paymentIntent.status);
      }}
    }}
  }} catch(e) {{
    setMsg('Erro inesperado: ' + e);
  }}
}});
</script>
</body></html>";

        // intercepta o callback app://
        Browser.Navigating += (s, e) =>
        {
            if (e.Url?.StartsWith("app://paid") == true)
            {
                e.Cancel = true;
                var uri = new Uri(e.Url);
                var q = HttpUtility.ParseQueryString(uri.Query);
                var status = q.Get("status");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Pagamento", $"Status: {status}", "OK");
                    await Navigation.PopAsync();
                });
            }
        };

        Browser.Source = new HtmlWebViewSource { Html = html };
    }

    private record CreateIntentResp(string clientSecret, string intentId, string publishableKey);
}