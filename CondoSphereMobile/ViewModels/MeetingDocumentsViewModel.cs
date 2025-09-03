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
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            var list = await _api.GetAsync<List<MeetingDocumentItem>>(
                $"meeting-documents?condominiumId={condominiumId}");

            Documents.Clear();
            foreach (var d in list) Documents.Add(d);
        }



    }
}

