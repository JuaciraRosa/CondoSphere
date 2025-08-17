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
                var loginRequest = new LoginRequest
                {
                    Email = Email,
                    Password = Password
                };

                var response = await _apiService.PostAsync<LoginRequest, LoginResponse>("auth/login", loginRequest);

                if (!string.IsNullOrEmpty(response.Token))
                {
                    await SecureStorage.SetAsync("jwt_token", response.Token);
                    await SecureStorage.SetAsync("user_role", response.Role);
                    await SecureStorage.SetAsync("user_name", response.FullName);


                    _apiService.SetAuthToken(response.Token);

                    // Ir para o dashboard
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Login inválido", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
        }
    }
}
