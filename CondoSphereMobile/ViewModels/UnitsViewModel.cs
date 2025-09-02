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
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                (LoadUnitsCommand as Command)?.ChangeCanExecute();
            }
        }

        public ObservableCollection<UnitItem> Units { get; } = new();

        public ICommand LoadUnitsCommand { get; }

        public UnitsViewModel()
        {
            LoadUnitsCommand = new Command(
                execute: async () => await LoadUnitsAsync(),
                canExecute: () => !IsBusy
            );
        }

        public async Task LoadUnitsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // JWT
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _api.SetAuthToken(token);

                // Precisa estar logado como RESIDENT
                dynamic me = await _api.GetAsync<dynamic>("residents/me");

                // A API pode devolver "ownedUnits" ou "OwnedUnits"
                var ownedUnits = me?.ownedUnits ?? me?.OwnedUnits;

                Units.Clear();

                if (ownedUnits != null)
                {
                    foreach (var u in ownedUnits)
                    {
                        Units.Add(new UnitItem
                        {
                            Id = (int)(u.id ?? u.Id ?? 0),
                            Number = (string)(u.number ?? u.Number ?? ""),
                            Area = Convert.ToDecimal(u.area ?? u.Area ?? 0m),
                            CondominiumId = (int)(u.condominiumId ?? u.CondominiumId ?? 0),
                            OwnerId = (string)(u.ownerId ?? u.OwnerId ?? "")
                        });
                    }
                }
                else
                {
                    // Se não for Resident (Admin/Manager), não há "minhas" unidades para listar.
                    // Opcional: mostrar aviso.
                    await Application.Current.MainPage.DisplayAlert(
                        "Info", "Esta lista mostra as unidades do residente autenticado.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // DTO usado só para o XAML desta página
        public class UnitItem
        {
            public int Id { get; set; }
            public string Number { get; set; } = "";
            public decimal Area { get; set; }
            public int CondominiumId { get; set; }
            public string OwnerId { get; set; } = "";
        }
    }
}

