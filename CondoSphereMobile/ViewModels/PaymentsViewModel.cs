using System.Collections.ObjectModel;
using System.Windows.Input;
using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.ViewModels
{
    public class PaymentsViewModel : BindableObject
    {
        private readonly ApiService _api;
        private bool _isBusy;

        public ObservableCollection<Payment> Payments { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand LoadPaymentsCommand { get; }

        public PaymentsViewModel()
        {
            _api = new ApiService();
            LoadPaymentsCommand = new Command(async () => await LoadAsync());
        }

        private async Task EnsureAuthAsync()
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                await EnsureAuthAsync();

                var list = await _api.GetAsync<List<Payment>>("payments");
                Payments.Clear();
                foreach (var p in list) Payments.Add(p);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }
    }
}
