using CondoSphereMobile.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Services
{
    public class SessionService
    {
        private readonly ApiService _api;

        public string Token { get; private set; } = "";
        public string Role { get; private set; } = "";
        public string UserName { get; private set; } = "";
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

        public event Action? Changed;

        public SessionService(ApiService api) => _api = api;

        public async Task LoadAsync()
        {
            Token = await SecureStorage.GetAsync("jwt_token") ?? "";
            Role = await SecureStorage.GetAsync("user_role") ?? "";   // pode trocar por Preferences se quiser
            UserName = await SecureStorage.GetAsync("user_name") ?? "";   // idem

            _api.SetAuthToken(Token);
            RaiseChanged();
        }

        public async Task SignInAsync(LoginResponse r)
        {
            Token = r.Token;
            Role = r.Role;
            UserName = r.FullName ?? "";

            await SecureStorage.SetAsync("jwt_token", Token);
            await SecureStorage.SetAsync("user_role", Role);
            await SecureStorage.SetAsync("user_name", UserName);

            _api.SetAuthToken(Token);
            RaiseChanged();
        }

        // <<< Remove o 'async' (some o CS1998)
        public Task SignOutAsync()
        {
            Token = Role = UserName = "";
            _api.SetAuthToken("");

            try { SecureStorage.Remove("jwt_token"); } catch { /* ignore */ }
            try { SecureStorage.Remove("user_role"); } catch { /* ignore */ }
            try { SecureStorage.Remove("user_name"); } catch { /* ignore */ }

            RaiseChanged();
            return Task.CompletedTask;
        }

        private void RaiseChanged()
        {
            if (MainThread.IsMainThread) Changed?.Invoke();
            else MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke());
        }
    }
}
