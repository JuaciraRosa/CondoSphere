using CommunityToolkit.Mvvm.Messaging;
using CondoSphereMobile.Events;
using CondoSphereMobile.Messages;
using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;
public partial class QuotasPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private List<QuotaVM> _items = new();
    private bool _subscribed;

    public QuotasPage() => InitializeComponent();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api)
            _api = api;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_subscribed)
        {
            AppEvents.QuotaPaid += OnQuotaPaid;
            _subscribed = true;
        }

        //// evita múltiplos handlers
        //AppEvents.QuotaPaid -= OnQuotaPaid;
        //AppEvents.QuotaPaid += OnQuotaPaid;

        // pega do DI se vier nulo
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        if (_api is null)
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = "Serviço não inicializado. Faça login novamente.";
            return;
        }

        await LoadAsync();
    }

    //protected override void OnDisappearing()
    //{
    //    base.OnDisappearing();

    //    if (!_subscribed)
    //    {
    //        AppEvents.QuotaPaid += OnQuotaPaid;
    //        _subscribed = true;
    //    }
    //    AppEvents.QuotaPaid -= OnQuotaPaid;
    //}

    //private void OnQuotaPaid(int quotaId)
    //{
    //    // marca a quota como "Pendente" imediatamente
    //    var vm = _items.FirstOrDefault(x => x.Quota.Id == quotaId);
    //    if (vm != null)
    //    {
    //        vm.IsPending = true;

    //        // (opcional) reordenar: em aberto/pendente no topo
    //        _items = _items
    //            .OrderBy(v => v.IsPaid)
    //            .ThenBy(v => v.DueDate)
    //            .ToList();

    //        // garante refresh mesmo se o VM não notificar
    //        Rebind();
    //    }
    //}


    private void OnQuotaPaid(int quotaId)
    {
        // Garante atualização no UI thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var vm = _items.FirstOrDefault(x => x.Quota.Id == quotaId);
            if (vm != null)
            {
                vm.IsPending = true; // muda status agora

                // (opcional) reordenar
                _items = _items
                    .OrderBy(v => v.IsPaid)
                    .ThenBy(v => v.DueDate)
                    .ToList();

                Rebind(); // força refresh
            }
        });
    }


    private async Task LoadAsync()
    {
        var (list, err) = await _api!.GetQuotasAsync();

        if (err != null)
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = err;
            ListView.ItemsSource = null;
            return;
        }

        ErrorLabel.IsVisible = false;
        _items = (list ?? Enumerable.Empty<QuotaDto>())
     .Select(q =>
     {
         var vm = new QuotaVM(q);          // já usa o status do servidor

         // garante que continua PENDENTE mesmo após recarregar
         if (AppEvents.IsPending(q.Id))
             vm.IsPending = true;

         //  se o servidor marcou Pago, podem se limpar dos pendentes locais:
         if (q.IsPaid)
             AppEvents.ClearPending(q.Id);

         return vm;
     })
     .OrderBy(vm => vm.IsPaid)    // em aberto/pendente primeiro
     .ThenBy(vm => vm.DueDate)
     .ToList();


        Rebind();
    }

    private void Rebind()
    {
        ListView.ItemsSource = null;
        ListView.ItemsSource = _items;
    }
    //private async Task LoadAsync()
    //{
    //    var (list, err) = await _api!.GetQuotasAsync();

    //    if (err != null)
    //    {
    //        ErrorLabel.IsVisible = true;
    //        ErrorLabel.Text = err;
    //        ListView.ItemsSource = null;
    //        return;
    //    }

    //    ErrorLabel.IsVisible = false;
    //    _items = (list ?? Enumerable.Empty<QuotaDto>())
    //        .Select(q => new QuotaVM(q))
    //        .OrderBy(vm => vm.IsPaid)    // “em aberto/pendente” primeiro
    //        .ThenBy(vm => vm.DueDate)
    //        .ToList();

    //    Rebind();
    //}

    //private void Rebind()
    //{
    //    ListView.ItemsSource = null;
    //    ListView.ItemsSource = _items;
    //}


    //private async void OnPayClicked(object sender, EventArgs e)
    //{
    //    if (_api is null) return;
    //    if (sender is not Button btn || btn.BindingContext is not QuotaVM vm) return;
    //    if (!vm.CanPay) return;

    //    var ok = await DisplayAlert("Pagamento", $"Pagar {vm.Amount:C}?", "Sim", "Não");
    //    if (!ok) return;

        
    //    // cria o PaymentIntent
    //    var (intent, err) = await _api.CreateCardIntentAsync(vm.Quota.Id);
    //    if (intent is null)
    //    {
    //        await DisplayAlert("Erro", err ?? "Não foi possível iniciar o pagamento.", "OK");
    //        return;
    //    }

    //    // abre a PaymentPage (WebView com Stripe.js)
    //    await Shell.Current.GoToAsync(nameof(PaymentPage), new Dictionary<string, object>
    //    {
    //        { "ApiService", _api },
    //        { "QuotaId", vm.Quota.Id },
    //        { "ClientSecret", intent.ClientSecret },
    //        { "PublishableKey", intent.PublishableKey },
    //        { "IntentId", intent.IntentId }
    //    });
    //}


    private async void OnPayClicked(object sender, EventArgs e)
    {
        if (_api is null) return;
        if (sender is not Button btn || btn.BindingContext is not QuotaVM vm) return;
        if (!vm.CanPay) return;

        var ok = await DisplayAlert("Pagamento", $"Pagar {vm.Amount:C}?", "Sim", "Não");
        if (!ok) return;

       
        var email = _api.CurrentUserEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            var (profile, _) = await _api.GetProfileAsync();
            email = profile?.Email;
        }

      
        var (intent, err) = await _api.CreateCardIntentAsync(vm.Quota.Id, email);
        if (intent is null)
        {
            await DisplayAlert("Erro", err ?? "Não foi possível iniciar o pagamento.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(PaymentPage), new Dictionary<string, object>
    {
        { "ApiService", _api },
        { "QuotaId", vm.Quota.Id },
        { "ClientSecret", intent.ClientSecret },
        { "PublishableKey", intent.PublishableKey },
        { "IntentId", intent.IntentId }
    });
    }

}