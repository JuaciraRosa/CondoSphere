using CommunityToolkit.Mvvm.Messaging;
using CondoSphereMobile.Events;
using CondoSphereMobile.Messages;
using CondoSphereMobile.Services;
using System.Web;

namespace CondoSphereMobile.Views;

public partial class PaymentPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private string _clientSecret = "";
    private string _publishableKey = "";
    private string _intentId = "";
    private int _quotaId;

    public PaymentPage() => InitializeComponent();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api) _api = api;
        if (query.TryGetValue("ClientSecret", out var cs) && cs is string s1) _clientSecret = s1;
        if (query.TryGetValue("PublishableKey", out var pk) && pk is string s2) _publishableKey = s2;
        if (query.TryGetValue("IntentId", out var iid) && iid is string s3) _intentId = s3;
        if (query.TryGetValue("QuotaId", out var q) && q is int qi) _quotaId = qi;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadStripeHtml();
    }

    private void LoadStripeHtml()
    {
        // HTML mínimo com Stripe.js e CardElement.
        var html = $@"
<!doctype html>
<html>
<head>
  <meta name='viewport' content='width=device-width, initial-scale=1'>
  <script src='https://js.stripe.com/v3/'></script>
  <style>
    body {{ font-family: system-ui, -apple-system, Segoe UI, Roboto; padding:16px }}
    #card-element {{ padding:12px; border:1px solid #ddd; border-radius:8px; }}
    button {{ margin-top:16px; padding:12px 16px; border:0; border-radius:8px; }}
  </style>
</head>
<body>
  <h3>Pagamento com cartão</h3>
  <div id='card-element'></div>
  <div id='msg' style='margin-top:10px;color:#c00;'></div>
  <button id='pay'>Pagar</button>

  <script>
    const stripe = Stripe('{_publishableKey}');
    const elements = stripe.elements();
    const card = elements.create('card');
    card.mount('#card-element');

    document.getElementById('pay').onclick = async () => {{
      document.getElementById('msg').textContent = '';
      try {{
        const result = await stripe.confirmCardPayment('{_clientSecret}', {{
          payment_method: {{ card: card }}
        }});
        if (result.error) {{
          document.getElementById('msg').textContent = result.error.message;
        }} else {{
          if (result.paymentIntent && result.paymentIntent.status === 'succeeded') {{
            // devolve para o app MAUI via esquema customizado
            window.location = 'app://paid?intentId=' + encodeURIComponent(result.paymentIntent.id);
          }} else {{
            document.getElementById('msg').textContent = 'Status: ' + (result.paymentIntent?.status ?? 'desconhecido');
          }}
        }}
      }} catch (e) {{
        document.getElementById('msg').textContent = e.message || 'Erro inesperado';
      }}
    }};
  </script>
</body>
</html>";
        StripeWebView.Source = new HtmlWebViewSource { Html = html };
    }

    // Interceta o callback "app://paid?intentId=..."
    private async void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("app://paid"))
        {
            e.Cancel = true;
            var uri = new Uri(e.Url);
            var qs = HttpUtility.ParseQueryString(uri.Query);
            var intentId = qs.Get("intentId") ?? _intentId;

            if (_api is null) { await DisplayAlert("Erro", "Sessão inválida.", "OK"); return; }

            var (status, err) = await _api.ConfirmPaymentAsync(intentId);
            if (err != null)
            {
                await DisplayAlert("Erro", err, "OK");
                return;
            }

            AppEvents.RaiseQuotaPaid(_quotaId);

            await DisplayAlert("Pagamento recebido",
                "Pagamento submetido com sucesso. Aguarde avaliação do gestor.", "OK");

            await Shell.Current.GoToAsync(".."); // fecha o pagamento
        }
    }
}