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
    public class MeetingsViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Meeting> Meetings { get; set; } = new();

        public ICommand LoadMeetingsCommand { get; }

        public MeetingsViewModel()
        {
            _apiService = new ApiService();
            LoadMeetingsCommand = new Command(async () => await LoadMeetingsAsync());
        }

        private async Task LoadMeetingsAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var meetings = await _apiService.GetAsync<List<Meeting>>("meetings");
                Meetings.Clear();
                foreach (var meeting in meetings)
                    Meetings.Add(meeting);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
