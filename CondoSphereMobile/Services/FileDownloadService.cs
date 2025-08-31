using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Services
{
    public class FileDownloadService
    {
        private readonly HttpClient _http;

        public FileDownloadService(string baseApiUrl, string? bearerToken)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseApiUrl) };
            if (!string.IsNullOrWhiteSpace(bearerToken))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        public async Task<byte[]> GetBytesAsync(string relativeEndpoint)
        {
            var resp = await _http.GetAsync(relativeEndpoint);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsByteArrayAsync();
        }
    }
}
