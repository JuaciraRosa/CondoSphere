using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class ChatThreadPage : ContentPage, IQueryAttributable
{
    private ApiService? _api;
    private int _threadId;
    private readonly List<ChatMessageItem> _messages = new();
    private int _lastId = 0;
    private bool _polling;
    public static int? CurrentThreadId { get; private set; }

    public ChatThreadPage()
    {
        InitializeComponent();

        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
        });
    }

    //public void ApplyQueryAttributes(IDictionary<string, object> query)
    //{
    //    if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api) _api = api;
    //    else if (Shell.Current is AppShell s && s.CurrentApi is not null) _api = s.CurrentApi;

    //    if (query.TryGetValue("ThreadId", out var idObj) && idObj is int tid) _threadId = tid;
    //}

    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();
    //    _ = InitAsync();
    //}

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var obj) && obj is ApiService api)
            _api = api;

        if (query.TryGetValue("ThreadId", out var idObj) && idObj is int tid)
            _threadId = tid;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CurrentThreadId = _threadId;
        // Fallback: pega a MESMA instância registrada no MauiProgram (singleton)
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        if (_api is null) return;

        _ = InitAsync();
    }

    protected override void OnDisappearing()
    {
        _polling = false;
        CurrentThreadId = null;
        base.OnDisappearing();
    }

    private async Task InitAsync()
    {
        if (_api is null || _threadId <= 0) return;

        // marca lido
        _ = _api.ChatMarkReadAsync(_threadId);

        // carrega mensagens iniciais
        var (items, error) = await _api.ChatGetMessagesAsync(_threadId, afterId: 0);
        if (error != null) { await DisplayAlert("Erro", error, "OK"); return; }

        _messages.Clear();
        if (items != null) _messages.AddRange(items);
        _lastId = _messages.Count == 0 ? 0 : _messages[^1].Id;

        MessagesView.ItemsSource = null;
        MessagesView.ItemsSource = _messages;
        MessagesView.ScrollTo(_messages.LastOrDefault(), position: ScrollToPosition.End, animate: false);

        // inicia polling leve
        _polling = true;
        _ = PollLoopAsync();
    }

    private async Task PollLoopAsync()
    {
        while (_polling)
        {
            try
            {
                if (_api is null) break;
                var (items, error) = await _api.ChatGetMessagesAsync(_threadId, _lastId);
                if (error == null && items != null && items.Count > 0)
                {
                    foreach (var m in items)
                    {
                        _messages.Add(m);
                        _lastId = m.Id;
                    }
                    MessagesView.ItemsSource = null;
                    MessagesView.ItemsSource = _messages;
                    MessagesView.ScrollTo(_messages.LastOrDefault(), position: ScrollToPosition.End, animate: true);
                }
            }
            catch { /* swallow polling exceptions */ }

            await Task.Delay(2500);
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (_api is null || _threadId <= 0) return;

        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var (msg, error) = await _api.ChatSendMessageAsync(_threadId, text);
        if (error != null) { await DisplayAlert("Erro", error, "OK"); return; }

        MessageEntry.Text = "";
        // msg também chegará no poll; mas já podemos mostrar
        if (msg != null)
        {
            _messages.Add(msg);
            _lastId = Math.Max(_lastId, msg.Id);
            MessagesView.ItemsSource = null;
            MessagesView.ItemsSource = _messages;
            MessagesView.ScrollTo(_messages.LastOrDefault(), position: ScrollToPosition.End, animate: true);
        }
    }
}