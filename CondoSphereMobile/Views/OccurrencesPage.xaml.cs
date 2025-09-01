using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class OccurrencesPage : ContentPage
{
    private readonly ApiService _api = new();

    public OccurrencesPage()
    {
        InitializeComponent();
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Busy.IsRunning = Busy.IsVisible = true;
            // usa o teu endpoint. Se ainda não tens API, podes apontar para MVC/JSON fake temporário
            var data = await _api.GetAsync<List<OccurrenceDto>>("occurrences/mine"); // <-- ajusta
            List.ItemsSource = data.OrderByDescending(o => o.CreatedAt).ToList();
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

    private async void OnNewClicked(object sender, EventArgs e)
    {
        var page = new OccurrenceCreatePage();
        page.Created += async () => { await Navigation.PopAsync(); await LoadAsync(); };
        await Navigation.PushAsync(page);
    }
}