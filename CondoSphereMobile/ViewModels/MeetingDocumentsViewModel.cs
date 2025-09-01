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
    public class MeetingDocumentsViewModel : BaseViewModel
    {
        public ObservableCollection<MeetingDocumentItem> Items { get; } = new ObservableCollection<MeetingDocumentItem>();
        private readonly ApiService _api;

        public MeetingDocumentsViewModel()
        {
            _api = new ApiService();
        }

        public async Task LoadAsync(int condominiumId)
        {
            var data = await _api.GetAsync<MeetingDocumentItem[]>($"api/meeting-documents?condominiumId={condominiumId}");
            Items.Clear();
            foreach (var it in data) Items.Add(it);
        }
    }
}
