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
            Role = await SecureStorage.GetAsync("user_role") ?? "";
            UserName = await SecureStorage.GetAsync("user_name") ?? "";
            _api.SetAuthToken(Token);
            Changed?.Invoke();
        }

        public async Task SignInAsync(LoginResponse r)
        {
            Token = r.Token; Role = r.Role; UserName = r.FullName ?? "";
            await SecureStorage.SetAsync("jwt_token", Token);
            await SecureStorage.SetAsync("user_role", Role);
            await SecureStorage.SetAsync("user_name", UserName);
            _api.SetAuthToken(Token);
            Changed?.Invoke();
        }

        public async Task SignOutAsync()
        {
            Token = Role = UserName = "";
            _api.SetAuthToken("");
            SecureStorage.Remove("jwt_token");
            SecureStorage.Remove("user_role");
            SecureStorage.Remove("user_name");
            Changed?.Invoke();
        }
    }
}
