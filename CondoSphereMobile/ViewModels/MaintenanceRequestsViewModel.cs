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
        private readonly ApiService _apiService;

        public ObservableCollection<MaintenanceRequest> Requests { get; set; } = new();

        public ICommand LoadRequestsCommand { get; }

        public MaintenanceRequestsViewModel()
        {
            _apiService = new ApiService();
            LoadRequestsCommand = new Command(async () => await LoadRequestsAsync());
        }

        private async Task LoadRequestsAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var requests = await _apiService.GetAsync<List<MaintenanceRequest>>("maintenance-requests");
                Requests.Clear();
                foreach (var req in requests)
                    Requests.Add(req);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
