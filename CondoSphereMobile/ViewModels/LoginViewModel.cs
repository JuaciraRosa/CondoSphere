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

        private string _email = "";
        public string Email
        {
            get => _email;
            set { if (_email == value) return; _email = value; OnPropertyChanged(); }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set { if (_password == value) return; _password = value; OnPropertyChanged(); }
        }

        private bool _rememberMe;
        public bool RememberMe
        {
            get => _rememberMe;
            set { if (_rememberMe == value) return; _rememberMe = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand InitCommand { get; }

        public LoginViewModel()
        {
            _apiService = new ApiService();

            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            ForgotPasswordCommand = new Command(async () => await ForgotPasswordAsync(), () => !IsBusy);
            InitCommand = new Command(async () => await InitAsync());
        }

        /// <summary>Chama isto em OnAppearing da LoginPage.</summary>
        public async Task InitAsync()
        {
            // limpa token se o utilizador não marcou "remember me" antes
            await ClearTokenIfNotRememberAsync();

            // carrega preferências
            Email = Preferences.Get("saved_email", "");
            RememberMe = Preferences.Get("remember_me", false);

            // auto-login se remember=true + token presente
            if (RememberMe)
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetAuthToken(token);
                    await NavigateToHomeAsync();
                }
            }
        }

        private async Task LoginAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    await Application.Current.MainPage.DisplayAlert("Atenção", "Informe email e password.", "OK");
                    return;
                }

                var req = new LoginRequest { Email = Email.Trim(), Password = Password };

                LoginResponse resp = null;

                // 1) tenta /api/auth/login
                try
                {
                    resp = await _apiService.PostAsync<LoginRequest, LoginResponse>("auth/login", req);
                }
                catch (Exception ex) when (
                    ex.Message.Contains("404") || ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                {
                    // 2) fallback para /api/authn/login
                    resp = await _apiService.PostAsync<LoginRequest, LoginResponse>("authn/login", req);
                }
                catch (Exception ex) when (
                    ex.Message.Contains("401") || ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Credenciais inválidas.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(resp?.Token))
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Resposta sem token.", "OK");
                    return;
                }

                await SecureStorage.SetAsync("jwt_token", resp.Token);
                _apiService.SetAuthToken(resp.Token);

                Preferences.Set("remember_me", RememberMe);
                if (RememberMe) Preferences.Set("saved_email", Email);
                else Preferences.Remove("saved_email");

                Preferences.Set("user_role", resp.Role ?? "");
                Preferences.Set("user_fullname", resp.FullName ?? "");

                await Shell.Current.GoToAsync("//DashboardPage");
                Password = "";
            }
            catch (Exception ex)
            {
                // Se o backend enviar { "message": "Invalid login credentials" } num 401 “embrulhado”
                if (ex.Message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase))
                    await Application.Current.MainPage.DisplayAlert("Erro", "Credenciais inválidas.", "OK");
                else
                    await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                (LoginCommand as Command)?.ChangeCanExecute();
                (ForgotPasswordCommand as Command)?.ChangeCanExecute();
            }
        }


        private async Task ForgotPasswordAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                var email = (Email ?? "").Trim();
                if (string.IsNullOrWhiteSpace(email))
                {
                    await Application.Current.MainPage.DisplayAlert("Atenção", "Informe o email para recuperar a senha.", "OK");
                    return;
                }

                // Ajuste a rota se no teu backend for outra
                await _apiService.PostAsync<object, object>("auth/forgot-password", new { Email = email });

                // Mensagem genérica (segurança)
                await Application.Current.MainPage.DisplayAlert(
                    "Ok",
                    "Se o email existir, enviámos um link para redefinir a senha.",
                    "OK");
            }
            catch
            {
                // mesma mensagem (não revelar existência de contas)
                await Application.Current.MainPage.DisplayAlert(
                    "Ok",
                    "Se o email existir, enviámos um link para redefinir a senha.",
                    "OK");
            }
            finally
            {
                IsBusy = false;
                (LoginCommand as Command)?.ChangeCanExecute();
                (ForgotPasswordCommand as Command)?.ChangeCanExecute();
            }
        }

        private static async Task NavigateToHomeAsync()
        {
            // Se usas Shell com rota para o dashboard:
            await Shell.Current.GoToAsync("//DashboardPage");

            // Alternativa (com AppShell como MainPage):
            // Application.Current.MainPage = new AppShell();
        }

        /// <summary>Remove o token guardado se o utilizador não marcou “remember me”.</summary>
        private static async Task ClearTokenIfNotRememberAsync()
        {
            if (!Preferences.Get("remember_me", false))
            {
                try { SecureStorage.Remove("jwt_token"); } catch { /* ignore */ }
            }
            await Task.CompletedTask;
        }
    }
}
