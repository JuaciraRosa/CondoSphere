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
    public class PaymentsViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Payment> Payments { get; set; } = new();

        public ICommand LoadPaymentsCommand { get; }

        public PaymentsViewModel()
        {
            _apiService = new ApiService();
            LoadPaymentsCommand = new Command(async () => await LoadPaymentsAsync());
        }

        private async Task LoadPaymentsAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var payments = await _apiService.GetAsync<List<Payment>>("payments");
                Payments.Clear();
                foreach (var payment in payments)
                    Payments.Add(payment);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
