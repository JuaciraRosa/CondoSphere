using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using System.Collections.ObjectModel;

namespace CondoSphereMobile.Views;

public partial class OccurrencesPage : ContentPage
{
    private readonly ApiService _api = new();
    public ObservableCollection<OccurrenceDto> Items { get; } = new();

    public OccurrencesPage()
    {
        InitializeComponent();
        List.ItemsSource = Items;
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Busy.IsRunning) return;
        try
        {
            Busy.IsVisible = Busy.IsRunning = true;

            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            var data = await _api.GetAsync<List<OccurrenceDto>>("occurrences"); // ?
            Items.Clear();
            foreach (var o in data) Items.Add(o);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
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