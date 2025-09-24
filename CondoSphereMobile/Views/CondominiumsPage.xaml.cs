using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;
public partial class CondominiumsPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;

    public CondominiumsPage() => InitializeComponent();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api) _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        if (_api is null) { ErrorLabel.IsVisible = true; ErrorLabel.Text = "Serviço não inicializado."; return; }

        var (list, err) = await _api.GetCondominiumsAsync();
        if (err != null)
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = err;
            ListView.ItemsSource = null;
            return;
        }

        ErrorLabel.IsVisible = false;
        ListView.ItemsSource = list;
    }
}