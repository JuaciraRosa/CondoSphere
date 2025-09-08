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
        private readonly ApiService _api = new();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); (LoadUnitsCommand as Command)?.ChangeCanExecute(); }
        }

        public ObservableCollection<UnitItem> Units { get; } = new();
        public ICommand LoadUnitsCommand { get; }

        public UnitsViewModel()
        {
            LoadUnitsCommand = new Command(async () => await LoadUnitsAsync(), () => !IsBusy);
        }

        public async Task LoadUnitsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                // garante header
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

                var role = Preferences.Get("user_role", "");
                List<ApiService.UnitDto> src;

                if (role == "Administrator" || role == "Manager")
                    src = await _api.GetUnitsAllAsync();
                else
                    src = await _api.GetUnitsMineAsync();

                Units.Clear();
                foreach (var u in src)
                {
                    Units.Add(new UnitItem
                    {
                        Id = u.Id,
                        Number = u.UnitNumber,
                        Area = u.Area,
                        CondominiumId = u.CondominiumId,
                        OwnerId = u.OwnerId
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }
    }

}

