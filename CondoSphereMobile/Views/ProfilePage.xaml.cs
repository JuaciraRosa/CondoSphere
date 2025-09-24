using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class ProfilePage : ContentPage, IQueryAttributable
{
    private ApiService? _api;

    public ProfilePage()
    {
        InitializeComponent();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ApiService", out var apiObj) && apiObj is ApiService api)
            _api = api;
        //else if (Shell.Current is AppShell s && s.CurrentApi is not null)
        //    _api = s.CurrentApi;

        if (_api is null) return;

        await LoadProfile();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Fallback DI (mesma instância singleton que tem o Bearer)
        _api ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<ApiService>();
        await LoadProfile();
    }

    private async Task LoadProfile()
    {
        if (_api is null) return;

        var (profile, error) = await _api.GetProfileAsync();
        if (profile == null)
        {
            await DisplayAlert("Erro ao carregar perfil",
                string.IsNullOrWhiteSpace(error) ? "Sem detalhes." : error, "OK");
            return;
        }

        NameLabel.Text = profile.FullName;
        EmailLabel.Text = profile.Email;
        RoleLabel.Text = profile.Role;



        AvatarImage.Source = $"{_api.BaseUrl}{profile.ProfileImagePath}";
    }


    private async void OnAccountClicked(object sender, EventArgs e)
    {
        if (_api is null) return;
        await Shell.Current.GoToAsync(nameof(AccountPage), true, new Dictionary<string, object> { { "ApiService", _api } });
    }
}