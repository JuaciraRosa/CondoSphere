using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.ViewModels
{
    public class MeetingDocumentsViewModel : BindableObject
    {
        private readonly ApiService _api = new();
        public ObservableCollection<MeetingDocumentItem> Documents { get; } = new();

        public async Task LoadAsync(int condominiumId)
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

                var url = $"meeting-documents?condominiumId={condominiumId}"; // ✅ sem {id} literal
                var list = await _api.GetAsync<List<MeetingDocumentItem>>(url);

                Documents.Clear();
                foreach (var d in list) Documents.Add(d);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    public class MeetingDocumentItem
    {
        public int Id { get; set; }
        public int CondominiumId { get; set; }
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime PublishedAt { get; set; }
    }
}

