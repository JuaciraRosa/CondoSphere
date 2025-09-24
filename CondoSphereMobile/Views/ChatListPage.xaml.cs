using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;
public partial class ChatListPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private List<ChatThreadItem> _all = new();

    public ChatListPage() => InitializeComponent();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api)
            _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Garante a instância do DI (singleton)
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        if (_api is null)
        {
            await DisplayAlert("Erro", "Serviço não inicializado. Faça login novamente.", "OK");
            ThreadsView.ItemsSource = null; // dispara EmptyView
            return;
        }

        await LoadThreadsAsync();
    }

    // Seleção ? ir para o thread
    private async void OnThreadSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_api is null) return;

        var item = e.CurrentSelection?.FirstOrDefault() as ChatThreadItem;
        if (item is null) return;

        ((CollectionView)sender).SelectedItem = null; // limpa seleção

        await Shell.Current.GoToAsync(nameof(ChatThreadPage), true,
            new Dictionary<string, object>
            {
                    { "ApiService", _api },
                    { "ThreadId", item.Id }
            });
    }

    private async Task LoadThreadsAsync()
    {
        if (_api is null) return;

        var (items, error) = await _api.ChatGetThreadsAsync();
        if (!string.IsNullOrWhiteSpace(error))
        {
            await DisplayAlert("Erro", error, "OK");
            ThreadsView.ItemsSource = null; // EmptyView
            return;
        }

        _all = (items ?? new()).OrderByDescending(t => t.LastActivityAt).ToList();

        // só 1 item: o “Open” mais recente; se não houver, o mais recente em geral
        var open = _all.FirstOrDefault(t =>
            string.Equals(t.Status, "Open", StringComparison.OrdinalIgnoreCase));

        var toShow = open != null
            ? new List<ChatThreadItem> { open }
            : _all.Take(1).ToList();

        ThreadsView.ItemsSource = toShow; // se vazio, EmptyView aparece
    }

    private async void OnNewChatClicked(object sender, EventArgs e)
    {
        if (_api is null) return;

        // cria ou reutiliza (409)
        var (id, reused, err) = await _api.ChatCreateOrReuseThreadAsync("Suporte", null);
        if (!string.IsNullOrWhiteSpace(err) || id <= 0)
        {
            await DisplayAlert("Erro", err ?? "Falha ao abrir o chat.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(ChatThreadPage), true,
            new Dictionary<string, object>
            {
                    { "ApiService", _api },
                    { "ThreadId", id }
            });
    }
}
