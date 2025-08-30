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
        // >>> Aponte para a SUA API (a que retorna JSON)
        public static readonly Uri BaseUri = new Uri("https://condospheresite.azurewebsites.net/api/");
        // Se publicar a API no Azure, troque aqui.

        private static readonly HttpClient _http = new HttpClient { BaseAddress = BaseUri };

        public static void SetToken(string? jwt)
        {
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(jwt) ? null : new AuthenticationHeaderValue("Bearer", jwt);
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<T> GetAsync<T>(string endpoint)
        {
            var resp = await _http.GetAsync(endpoint);
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();

            var body = await resp.Content.ReadAsStringAsync();
            var ct = resp.Content.Headers.ContentType?.MediaType;
            if (!resp.IsSuccessStatusCode || (ct != null && !ct.Contains("json")) || body.TrimStart().StartsWith("<"))
                throw new Exception($"GET {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public static async Task<TOut> PostAsync<TIn, TOut>(string endpoint, TIn payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var resp = await _http.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();

            var body = await resp.Content.ReadAsStringAsync();
            var ct = resp.Content.Headers.ContentType?.MediaType;
            if (!resp.IsSuccessStatusCode || (ct != null && !ct.Contains("json")) || body.TrimStart().StartsWith("<"))
                throw new Exception($"POST {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            return JsonSerializer.Deserialize<TOut>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public static async Task<TOut> PutAsync<TIn, TOut>(string endpoint, TIn payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var resp = await _http.PutAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();

            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception(body);
            return string.IsNullOrWhiteSpace(body)
                ? default! // NoContent
                : JsonSerializer.Deserialize<TOut>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public static async Task DeleteAsync(string endpoint)
        {
            var resp = await _http.DeleteAsync(endpoint);
            if ((int)resp.StatusCode == 401) throw new UnauthorizedAccessException();
            resp.EnsureSuccessStatusCode();
        }
    }
}