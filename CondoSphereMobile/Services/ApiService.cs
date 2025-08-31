
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CondoSphereMobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService()
        {
            _http = new HttpClient { BaseAddress = new Uri(AppConstants.BaseApiUrl) };
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetAuthToken(string token)
        {
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var resp = await _http.GetAsync(endpoint);
            var body = await resp.Content.ReadAsStringAsync();

            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();

            var ct = resp.Content.Headers.ContentType?.MediaType;
            if (!resp.IsSuccessStatusCode || (ct != null && !ct.Contains("json")) || body.TrimStart().StartsWith("<"))
                throw new Exception($"GET {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TOut> PostAsync<TIn, TOut>(string endpoint, TIn payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var resp = await _http.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();

            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();

            var ct = resp.Content.Headers.ContentType?.MediaType;
            if (!resp.IsSuccessStatusCode || (ct != null && !ct.Contains("json")) || body.TrimStart().StartsWith("<"))
                throw new Exception($"POST {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            return JsonSerializer.Deserialize<TOut>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

}

