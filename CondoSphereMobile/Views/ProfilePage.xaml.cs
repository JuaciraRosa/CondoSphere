
using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ApiService _api = new();

    public ProfilePage()
    {
        InitializeComponent();
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            // tenta Resident
            try
            {
                var me = await _api.GetAsync<ResidentMeDto>("residents/me");
                BindingContext = new ProfileVm
                {
                    FullName = me.FullName ?? "",
                    Email = me.Email,
                    Role = "Resident",
                    OwnedUnits = me.OwnedUnits.Select(u => new ProfileUnitVm
                    {
                        UnitNumber = u.UnitNumber,
                        Area = u.Area,
                        CondominiumId = u.CondominiumId
                    }).ToList()
                };
                return;
            }
            catch { /* não é Resident */ }

            // fallback: info básica (admin/manager)
            var auth = await _api.GetAsync<AuthInfo>("auth/me"); // se existir
            BindingContext = new ProfileVm
            {
                FullName = auth?.FullName ?? "",
                Email = auth?.Email ?? "",
                Role = auth?.Role ?? "User",
                OwnedUnits = new List<ProfileUnitVm>()
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    private class AuthInfo { public string Email { get; set; } = ""; public string FullName { get; set; } = ""; public string Role { get; set; } = ""; }

}