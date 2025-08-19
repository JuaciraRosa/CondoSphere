using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CondoSphereMobile.ViewModels
{
    public class MaintenanceRequestsViewModel : BindableObject
    {
        private readonly ApiService _api;

        public ObservableCollection<MaintenanceRequest> Requests { get; } = new();

        // Campos do formulário de criação
        private string _title;
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

        private string _description;
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

        private int _condominiumId;
        public int CondominiumId { get => _condominiumId; set { _condominiumId = value; OnPropertyChanged(); } }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }

        public MaintenanceRequestsViewModel()
        {
            _api = new ApiService();
            LoadCommand = new Command(async () => await LoadAsync());
            CreateCommand = new Command(async () => await CreateAsync());
        }

        private async Task EnsureAuthAsync()
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _api.SetAuthToken(token);
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                await EnsureAuthAsync();
                // Ajuste o endpoint conforme sua rota final no servidor
                var list = await _api.GetAsync<List<MaintenanceRequest>>("maintenance-requests");
                Requests.Clear();
                foreach (var r in list) Requests.Add(r);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        public async Task CreateAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Description) || CondominiumId <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Validation", "Fill Title, Description and CondominiumId.", "OK");
                    return;
                }

                await EnsureAuthAsync();

                var token = await SecureStorage.GetAsync("jwt_token");
                var userId = JwtHelper.GetClaimFromToken(token, "nameid"); // ClaimTypes.NameIdentifier

                var payload = new MaintenanceRequest
                {
                    Title = Title,
                    Description = Description,
                    CondominiumId = CondominiumId,
                    SubmittedAt = DateTime.UtcNow,
                    Status = RequestStatus.InProgress,
                    SubmittedById = userId // se o servidor ignorar e definir por User, tudo bem
                };

                var created = await _api.PostAsync<MaintenanceRequest, MaintenanceRequest>(
     "maintenance-requests/create-my", payload);


                // Limpa form e atualiza lista
                Title = string.Empty;
                Description = string.Empty;
                CondominiumId = 0;

                await LoadAsync();
                await Application.Current.MainPage.DisplayAlert("Success", $"Request #{created?.Id} created.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
