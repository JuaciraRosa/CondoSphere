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
    public class QuotasViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Quota> Quotas { get; set; } = new();

        public ICommand LoadQuotasCommand { get; }

        public QuotasViewModel()
        {
            _apiService = new ApiService();
            LoadQuotasCommand = new Command(async () => await LoadQuotasAsync());
        }

        private async Task LoadQuotasAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var quotas = await _apiService.GetAsync<List<Quota>>("quotas");
                Quotas.Clear();
                foreach (var quota in quotas)
                    Quotas.Add(quota);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
