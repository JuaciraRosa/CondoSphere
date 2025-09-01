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

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Condominium> Condominiums { get; } = new();

        public ICommand LoadCondominiumsCommand { get; }

        public CondominiumsViewModel()
        {
            _apiService = new ApiService();
            LoadCondominiumsCommand = new Command(async () => await LoadCondominiumsAsync(),
                                                  () => !IsBusy);
        }

        public async Task LoadCondominiumsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                // ⚠️ endpoint exatamente como no controller: "api/condominiums"
                // Como sua BaseApiUrl já termina com /api/, aqui é só "condominiums"
                var list = await _apiService.GetAsync<List<Condominium>>("condominiums");

                Condominiums.Clear();
                foreach (var condo in list)
                    Condominiums.Add(condo);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                (LoadCondominiumsCommand as Command)?.ChangeCanExecute();
            }
        }
    }
}
