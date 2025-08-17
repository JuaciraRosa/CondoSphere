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
    public class UnitsViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Unit> Units { get; set; } = new();

        public ICommand LoadUnitsCommand { get; }

        public UnitsViewModel()
        {
            _apiService = new ApiService();
            LoadUnitsCommand = new Command(async () => await LoadUnitsAsync());
        }

        private async Task LoadUnitsAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var units = await _apiService.GetAsync<List<Unit>>("units");
                Units.Clear();
                foreach (var unit in units)
                    Units.Add(unit);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
