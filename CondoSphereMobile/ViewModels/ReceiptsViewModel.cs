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
    public class ReceiptsViewModel : BaseViewModel
    {
        public ObservableCollection<Payment> Payments { get; } = new ObservableCollection<Payment>();
        public string ReceiptSavedPath { get; private set; }

        private readonly ApiService _api;
        private const string BaseApiUrl = "http://condosphere.somee.com/";

        public ReceiptsViewModel()
        {
            _api = new ApiService();
        }

        public async Task LoadPaymentsAsync()
        {
            var data = await _api.GetAsync<Payment[]>("api/payments"); // ajustar se o endpoint for diferente
            Payments.Clear();
            foreach (var p in data) Payments.Add(p);
        }

        public async Task<string> DownloadReceiptAsync(int paymentId)
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            var downloader = new FileDownloadService(BaseApiUrl, token);
            var bytes = await downloader.GetBytesAsync($"api/payment-receipts/{paymentId}");

            var fileName = $"recibo_{paymentId:D6}.pdf";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllBytes(path, bytes);
            ReceiptSavedPath = path;
            return path;
        }
    }
}
