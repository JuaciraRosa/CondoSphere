using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CondoSphereWinForms.Services
{
    public static class ApiClient
    {
       
        public static readonly Uri BaseUri = new Uri("https://condosphere.azurewebsites.net/api/");

        private static readonly HttpClient _http = new HttpClient { BaseAddress = BaseUri };
        private static string _token;

        public static void SetToken(string jwt)
        {
            _token = jwt;
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(jwt)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", jwt);
        }

        public static async Task<T> GetAsync<T>(string endpoint)
        {
            var resp = await _http.GetAsync(endpoint);
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public static async Task<TOut> PostAsync<TIn, TOut>(string endpoint, TIn payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.PostAsync(endpoint, content);
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();
            resp.EnsureSuccessStatusCode();

            var resJson = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TOut>(resJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public static async Task PostAsync<TIn>(string endpoint, TIn payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.PostAsync(endpoint, content);
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();
            resp.EnsureSuccessStatusCode();
        }

        public static async Task DeleteAsync(string endpoint)
        {
            var resp = await _http.DeleteAsync(endpoint);
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();
            resp.EnsureSuccessStatusCode();
        }
    }
}
