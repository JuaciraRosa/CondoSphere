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

                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

                // TIPADO (⚠️ não usar dynamic aqui)
                var me = await _api.GetAsync<ResidentMeDto>("residents/me");

                Units.Clear();
                foreach (var u in me.OwnedUnits)
                {
                    Units.Add(new UnitItem
                    {
                        Id = u.Id,
                        Number = u.UnitNumber,
                        Area = u.Area,
                        CondominiumId = u.CondominiumId,
                        OwnerId = me.Email
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

