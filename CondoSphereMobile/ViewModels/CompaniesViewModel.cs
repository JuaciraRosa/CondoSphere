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
    public class CompaniesViewModel : BindableObject
    {
        private readonly ApiService _apiService;
        public ObservableCollection<Company> Companies { get; set; } = new();

        public ICommand LoadCompaniesCommand { get; }

        public CompaniesViewModel()
        {
            _apiService = new ApiService();
            LoadCompaniesCommand = new Command(async () => await LoadCompaniesAsync());
        }

        private async Task LoadCompaniesAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var result = await _apiService.GetAsync<PagedResult<Company>>("companies");
                Companies.Clear();
                foreach (var c in result.Data)
                    Companies.Add(c);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }


    }
}
