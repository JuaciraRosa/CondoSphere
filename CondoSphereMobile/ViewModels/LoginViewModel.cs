using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CondoSphereMobile.ViewModels
{
    public class LoginViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public string Email { get; set; }
        public string Password { get; set; }
        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            _apiService = new ApiService();
            LoginCommand = new Command(async () => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            try
            {
                var req = new LoginRequest { Email = Email, Password = Password };
                var resp = await _apiService.PostAsync<LoginRequest, LoginResponse>("auth/login", req);

                if (!string.IsNullOrEmpty(resp.Token))
                {
                    await SecureStorage.SetAsync("jwt_token", resp.Token);
                    await SecureStorage.SetAsync("user_role", resp.Role ?? "");
                    await SecureStorage.SetAsync("user_name", resp.FullName ?? "");
                    // segue com o token no ApiService se quiseres
                    _apiService.SetAuthToken(resp.Token);

                    // abre o Shell simples
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Resposta sem token.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.ToString(), "OK"); // mostra stack/body
            }
        }

    }
}
