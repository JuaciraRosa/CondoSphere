
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
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppConstants.BaseApiUrl)
            };
        }

        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var resp = await _httpClient.GetAsync(endpoint);
            var body = await resp.Content.ReadAsStringAsync();

            // se a API mandar HTML (erro 401/403/500), evita “< inválido no JSON”
            var ct = resp.Content.Headers.ContentType?.MediaType;
            if (!resp.IsSuccessStatusCode || (ct != null && !ct.Contains("json")) || body.TrimStart().StartsWith("<"))
                throw new Exception($"GET {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }


        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync(endpoint, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");

            return JsonSerializer.Deserialize<TResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

    }
}
