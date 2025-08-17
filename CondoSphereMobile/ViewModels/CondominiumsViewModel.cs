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
    public class CondominiumsViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Condominium> Condominiums { get; set; } = new();

        public ICommand LoadCondominiumsCommand { get; }

        public CondominiumsViewModel()
        {
            _apiService = new ApiService();
            LoadCondominiumsCommand = new Command(async () => await LoadCondominiumsAsync());
        }

        private async Task LoadCondominiumsAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var condominiums = await _apiService.GetAsync<List<Condominium>>("condominiums");
                Condominiums.Clear();
                foreach (var condo in condominiums)
                    Condominiums.Add(condo);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
