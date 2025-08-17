
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
    public class NotificationsViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Notification> Notifications { get; set; } = new();

        public ICommand LoadNotificationsCommand { get; }

        public NotificationsViewModel()
        {
            _apiService = new ApiService();
            LoadNotificationsCommand = new Command(async () => await LoadNotificationsAsync());
        }

        private async Task LoadNotificationsAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var notifications = await _apiService.GetAsync<List<Notification>>("notifications");
                Notifications.Clear();
                foreach (var notification in notifications)
                    Notifications.Add(notification);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
