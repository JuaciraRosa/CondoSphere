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
    public class UsersViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();

        public ICommand LoadUsersCommand { get; }

        public UsersViewModel()
        {
            _apiService = new ApiService();
            LoadUsersCommand = new Command(async () => await LoadUsersAsync());
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var users = await _apiService.GetAsync<List<User>>("users");

                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
