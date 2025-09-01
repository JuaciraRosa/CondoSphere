
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace CondoSphereMobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService()
        {
#if ANDROID
            // Handler com ajustes que evitam falhas de handshake/HTTP2
            var handler = new Xamarin.Android.Net.AndroidMessageHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
#else
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
#endif
            _http = new HttpClient(handler)
            {
                BaseAddress = new Uri(AppConstants.BaseApiUrl),
                Timeout = TimeSpan.FromSeconds(30),
                DefaultRequestVersion = HttpVersion.Version11,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };

            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // User-Agent “de navegador” evita bloqueios em alguns hosts gratuitos
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Android 12; CondoSphere-MAUI) AppleWebKit/537.36 Chrome/122 Mobile Safari/537.36");
        }

        public void SetAuthToken(string token)
        {
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> GetAsync<T>(string endpoint, CancellationToken ct = default)
        {
            var resp = await _http.GetAsync(endpoint, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"GET {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            // tenta JSON mesmo que o Content-Type seja estranho
            try { return JsonSerializer.Deserialize<T>(body, _json)!; }
            catch (Exception)
            {
                throw new Exception($"GET {resp.RequestMessage?.RequestUri} retornou conteúdo não-JSON:\n{body}");
            }
        }

        public async Task<TOut> PostAsync<TIn, TOut>(string endpoint, TIn payload, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync(endpoint, payload, _json, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"POST {resp.RequestMessage?.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            try { return JsonSerializer.Deserialize<TOut>(body, _json)!; }
            catch (Exception)
            {
                throw new Exception($"POST {resp.RequestMessage?.RequestUri} retornou conteúdo não-JSON:\n{body}");
            }
        }
    }
}
