using CondoSphereMobile.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace CondoSphereMobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _jwtToken;

        public string? CurrentUserEmail { get; private set; }

        public string BaseUrl { get; } = "https://condosphere-web-app.somee.com";

        public ApiService()
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            })
            {
                BaseAddress = new Uri($"{BaseUrl}/api/")
            };

          
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }


        // ===== Helpers JSON case-insensitive =====
        private static readonly JsonSerializerOptions _json =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };


        public void SetToken(string token)
        {
            _jwtToken = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
          
        }


     
        public void ClearToken()
        {
            _jwtToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        private StringContent CreateContent(object data)
        {
            var json = JsonSerializer.Serialize(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        // ---------- LOGIN ----------
   
        public async Task<(bool success, string token, bool requires2FA, string error)> LoginAsync(string email, string password)
        {
            var resp = await _httpClient.PostAsync("auth/login",
                CreateContent(new { Email = email, Password = password }));

            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, "", false, $"Login falhou ({(int)resp.StatusCode}): {body}");

            if (!LooksLikeJson(resp, body))
                return (false, "", false, "Resposta não-JSON do servidor (provável HTML). Verifique a BaseAddress/rota da API.");

            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("requires2FA", out var req2) && req2.GetBoolean())
                return (true, "", true, "");

            var token = doc.RootElement.GetProperty("token").GetString() ?? "";
            SetToken(token);

            CurrentUserEmail = email; 

            return (true, token, false, "");
        }


        // ApiService.cs
        public async Task<(bool? enabled, string? error)> GetTwoFactorStatusAsync()
        {
            var resp = await _httpClient.GetAsync("auth/twofactor/status");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");

            using var doc = JsonDocument.Parse(body);
            var enabled = doc.RootElement.GetProperty("enabled").GetBoolean();
            return (enabled, null);
        }


        // ---------- PROFILE ----------
        public async Task<(ProfileDto? profile, string? error)> GetProfileAsync()
        {
            var response = await _httpClient.GetAsync("auth/profile");
            var payload = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (null, $"{(int)response.StatusCode} {response.ReasonPhrase}: {payload}");

            var dto = JsonSerializer.Deserialize<ProfileDto>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return (dto, null);
        }

        public async Task<bool> UpdateProfileAsync(string fullName, string email, Stream? avatarStream = null, string? avatarFileName = null)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(fullName ?? ""), "FullName");
            content.Add(new StringContent(email ?? ""), "Email");

            if (avatarStream != null && avatarFileName != null)
            {
                content.Add(new StreamContent(avatarStream), "avatar", avatarFileName);
            }

            var response = await _httpClient.PutAsync("auth/profile", content);
            return response.IsSuccessStatusCode;
        }

        // ---------- PASSWORD ----------
        public async Task<bool> ChangePasswordAsync(string current, string newPass)
        {
            var response = await _httpClient.PostAsync("auth/change-password",
                CreateContent(new { CurrentPassword = current, NewPassword = newPass }));
            return response.IsSuccessStatusCode;
        }

        // ---------- 2FA ----------
        public async Task<TwoFactorSetupDto?> Setup2FAAsync()
        {
            var response = await _httpClient.GetAsync("auth/twofactor/setup");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TwoFactorSetupDto>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        public async Task<(bool success, string token)> Login2FAAsync(string email, string code)
        {
            var resp = await _httpClient.PostAsync(
                "auth/login-2fa",
                CreateContent(new { Email = email, Code = code })
            );

            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, "");

            if (!LooksLikeJson(resp, body))
                return (false, ""); // resposta inesperada (HTML etc.)

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("token").GetString() ?? "";

            SetToken(token);
            return (true, token);
        }


        public async Task<(bool success, string[] recoveryCodes)> Enable2FAAsync(string code)
        {
            var response = await _httpClient.PostAsync("auth/twofactor/enable", CreateContent(new { Code = code }));
            if (!response.IsSuccessStatusCode) return (false, Array.Empty<string>());

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var codes = doc.RootElement.GetProperty("recoveryCodes").EnumerateArray().Select(c => c.GetString()).ToArray();
            return (true, codes!);
        }

        public async Task<bool> Disable2FAAsync()
        {
            var response = await _httpClient.PostAsync("auth/twofactor/disable", null);
            return response.IsSuccessStatusCode;
        }


        // ---------- PASSWORD RESET ----------
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var response = await _httpClient.PostAsync("auth/forgot-password",
                CreateContent(new { Email = email }));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var response = await _httpClient.PostAsync("auth/reset-password",
                CreateContent(new { Email = email, Token = token, NewPassword = newPassword }));
            return response.IsSuccessStatusCode;
        }


        // ---------- ACCOUNT ----------
        public async Task<bool> DeactivateAccountAsync()
        {
            var response = await _httpClient.PostAsync("auth/deactivate", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAccountAsync()
        {
            var response = await _httpClient.DeleteAsync("auth/delete");
            return response.IsSuccessStatusCode;
        }

        private static bool LooksLikeJson(HttpResponseMessage resp, string body)
        {
            var ct = resp.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrWhiteSpace(ct) && ct.Contains("json", StringComparison.OrdinalIgnoreCase))
                return true;

            var s = body.AsSpan().TrimStart();
            return s.StartsWith("{", StringComparison.Ordinal) || s.StartsWith("[", StringComparison.Ordinal);
        }

        //------------- Chat bot ---------------------------------

        public async Task<(List<ChatThreadItem>? items, string? error)> ChatGetThreadsAsync(string? status = null)
        {
            var url = "chat/threads";
            if (!string.IsNullOrWhiteSpace(status)) url += $"?status={Uri.EscapeDataString(status)}";

            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, $"HTTP {(int)resp.StatusCode}: {body}");

            // podes trocar para JsonSerializer.Deserialize<List<ChatThreadItem>>(body, options)
            var items = System.Text.Json.JsonSerializer.Deserialize<List<ChatThreadItem>>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return (items, null);
        }

        public async Task<(int threadId, bool reused, string? error)> ChatCreateOrReuseThreadAsync(
     string subject = "Suporte", int? condominiumId = null)
        {
            var resp = await _httpClient.PostAsync("chat/threads", CreateContent(new { subject, condominiumId }));
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                using var doc = JsonDocument.Parse(body);
                var id = doc.RootElement.GetProperty("threadId").GetInt32();
                return (id, true, null);     
            }

            if (!resp.IsSuccessStatusCode)
                return (0, false, $"HTTP {(int)resp.StatusCode}: {body}");

            using var ok = JsonDocument.Parse(body);
            return (ok.RootElement.GetProperty("Id").GetInt32(), false, null);
        }



        public async Task<(bool ok, string? error)> ChatMarkReadAsync(int threadId)
        {
            var resp = await _httpClient.PostAsync($"chat/threads/{threadId}/read", new StringContent(""));
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return (false, $"HTTP {(int)resp.StatusCode}: {body}");
            }
            return (true, null);
        }

        // ========== CHAT MESSAGES ==========

        public async Task<(List<ChatMessageItem>? items, string? error)> ChatGetMessagesAsync(int threadId, int afterId = 0)
        {
            var url = $"chat/threads/{threadId}/messages";
            if (afterId > 0) url += $"?after={afterId}";

            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, $"HTTP {(int)resp.StatusCode}: {body}");

            var items = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessageItem>>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return (items, null);
        }

        public async Task<(ChatMessageItem? msg, string? error)> ChatSendMessageAsync(int threadId, string text)
        {
            var resp = await _httpClient.PostAsync($"chat/threads/{threadId}/messages",
                CreateContent(new { text }));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, $"HTTP {(int)resp.StatusCode}: {body}");

            var msg = System.Text.Json.JsonSerializer.Deserialize<ChatMessageItem>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return (msg, null);
        }




        // ================= CONDOMÍNIOS =================
        public async Task<(IEnumerable<CondominiumDto>? list, string? error)> GetCondominiumsAsync()
        {
            var resp = await _httpClient.GetAsync("condominiums");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, body);
            var data = JsonSerializer.Deserialize<IEnumerable<CondominiumDto>>(body, _json);
            return (data, null);
        }

        public async Task<(CondominiumDto? item, string? error)> GetCondominiumAsync(int id)
        {
            var resp = await _httpClient.GetAsync($"condominiums/{id}");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, body);
            var data = JsonSerializer.Deserialize<CondominiumDto>(body, _json);
            return (data, null);
        }

        // ================= QUOTAS/PAGAMENTOS =================
        public async Task<(IEnumerable<QuotaDto>? list, string? error)> GetQuotasAsync()
        {
            var resp = await _httpClient.GetAsync("quotas/list");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, body);
            var data = JsonSerializer.Deserialize<IEnumerable<QuotaDto>>(body, _json);
            return (data, null);
        }

        public async Task<(IEnumerable<QuotaDto>? list, string? error)> GetQuotasByUnitAsync(int unitId)
        {
            var resp = await _httpClient.GetAsync($"quotas/by-unit/{unitId}");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, body);
            var data = JsonSerializer.Deserialize<IEnumerable<QuotaDto>>(body, _json);
            return (data, null);
        }


        //public async Task<(PaymentIntentResp? intent, string? error)> CreateCardIntentAsync(int quotaId)
        //{
        //    var resp = await _httpClient.PostAsync("payments/card/intent", CreateContent(new { QuotaId = quotaId }));
        //    var body = await resp.Content.ReadAsStringAsync();

        //    if (!resp.IsSuccessStatusCode)
        //    {
        //        try
        //        {
        //            using var doc = JsonDocument.Parse(body);
        //            if (doc.RootElement.TryGetProperty("error", out var e))
        //                return (null, e.GetString());
        //        }
        //        catch { /* veio HTML */ }

        //        return (null, $"{(int)resp.StatusCode} {resp.ReasonPhrase}");
        //    }

        //    var data = JsonSerializer.Deserialize<PaymentIntentResp>(body, _json);
        //    return (data, null);
        //}

        //public async Task<(PaymentIntentResp? intent, string? error)> CreateCardIntentAsync(int quotaId, string? email = null)
        //{
        //    var payload = new { QuotaId = quotaId, Email = email };
        //    var resp = await _httpClient.PostAsync("payments/card/intent", CreateContent(payload));
        //    var body = await resp.Content.ReadAsStringAsync();
        //    if (!resp.IsSuccessStatusCode)
        //    {
        //        try
        //        {
        //            using var doc = JsonDocument.Parse(body);
        //            if (doc.RootElement.TryGetProperty("error", out var e))
        //                return (null, e.GetString());
        //        }
        //        catch { /* veio HTML */ }

        //        return (null, $"{(int)resp.StatusCode} {resp.ReasonPhrase}");
        //    }
        //    var data = JsonSerializer.Deserialize<PaymentIntentResp>(body, _json);
        //    return (data, null);
        //}
        public async Task<(PaymentIntentResp? intent, string? error)>
    CreateCardIntentAsync(int quotaId, string? email = null)
        {
            var resp = await _httpClient.PostAsync(
                "payments/card/intent",
                CreateContent(new { QuotaId = quotaId, Email = email }) // <= envia o email
            );

            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("error", out var e))
                        return (null, e.GetString());
                }
                catch { }
                return (null, $"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }

            var data = JsonSerializer.Deserialize<PaymentIntentResp>(body, _json);
            return (data, null);
        }



        // chama após confirmar no cliente (Stripe SDK). Em demo, podes chamar logo a seguir
        public async Task<(string? status, string? error)> ConfirmPaymentAsync(string intentId)
        {
            var resp = await _httpClient.PostAsync("payments/confirm", CreateContent(new PaymentConfirmReq { IntentId = intentId }));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return (null, body);
            using var doc = JsonDocument.Parse(body);
            return (doc.RootElement.GetProperty("status").GetString(), null);
        }

    
        // ================= MANUTENÇÃO (unificado) =================

        // GET /api/maintenance-requests/list
        public async Task<(IEnumerable<MaintenanceRequestDto>? list, string? error)>
            GetMaintenanceListAsync()
        {
            var resp = await _httpClient.GetAsync("maintenance-requests/list");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.Content.Headers.ContentType?.MediaType?.Contains("html") == true)
                    return (null, $"{(int)resp.StatusCode} Acesso negado.");
                return (null, body);
            }

            var data = JsonSerializer.Deserialize<IEnumerable<MaintenanceRequestDto>>(body, _json);
            return (data, null);
        }

        // POST /api/maintenance-requests
        public async Task<(MaintenanceRequestDto? created, string? error)>
            CreateMaintenanceAsync(string title, string description, int condominiumId)
        {
            var payload = new
            {
                Title = title?.Trim() ?? "",
                Description = description?.Trim() ?? "",
                CondominiumId = condominiumId
            };

            var resp = await _httpClient.PostAsync("maintenance-requests", CreateContent(payload));
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, body);

            var data = JsonSerializer.Deserialize<MaintenanceRequestDto>(body, _json);
            return (data, null);
        }

   
   


        // ================= OCORRÊNCIAS (JSON store) =================

        public async Task<(IEnumerable<OccurrenceDto>? list, string? error)>
            GetOccurrencesAsync(int? condominiumId = null)
        {
            var url = "occurrences";
            if (condominiumId.HasValue)
                url += $"?condominiumId={condominiumId.Value}";

            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, body);

            var data = JsonSerializer.Deserialize<IEnumerable<OccurrenceDto>>(body, _json);
            return (data, null);
        }

        public async Task<(OccurrenceDto? item, string? error)>
            GetOccurrenceByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return (null, "Id inválido.");

            var resp = await _httpClient.GetAsync($"occurrences/{Uri.EscapeDataString(id)}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, body);

            var data = JsonSerializer.Deserialize<OccurrenceDto>(body, _json);
            return (data, null);
        }

        // CRIAR — (no teu controller atual: Administrator, Manager e Resident podem criar)
        public async Task<(OccurrenceDto? created, string? error)>
            CreateOccurrenceAsync(string title, string description, int condominiumId, string unitNumber, string? createdBy = null)
        {
            var payload = new
            {
                Title = title?.Trim() ?? "",
                Description = description?.Trim() ?? "",
                CondominiumId = condominiumId,
                UnitNumber = unitNumber?.Trim() ?? "",
                CreatedBy = createdBy?.Trim().ToLowerInvariant() ?? ""
            };

            var resp = await _httpClient.PostAsync("occurrences", CreateContent(payload));
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, body);

            var data = JsonSerializer.Deserialize<OccurrenceDto>(body, _json);
            return (data, null);
        }

        // ALTERAR STATUS — (apenas Admin/Manager no controller)
        public async Task<(OccurrenceDto? updated, string? error)>
            ChangeOccurrenceStatusAsync(string id, string status)
        {
            if (string.IsNullOrWhiteSpace(id))
                return (null, "Id inválido.");
            if (string.IsNullOrWhiteSpace(status))
                return (null, "Status inválido.");

            var path = $"occurrences/{Uri.EscapeDataString(id)}/status?status={Uri.EscapeDataString(status)}";

            // conteúdo vazio é suficiente para o POST de status
            var resp = await _httpClient.PostAsync(path, new StringContent(""));
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, body);

            var data = JsonSerializer.Deserialize<OccurrenceDto>(body, _json);
            return (data, null);
        }


        // ================= UNIDADES =================
        public async Task<(IEnumerable<string>? list, string? error)> GetUnitNumbersAsync(int condominiumId)
        {
            var resp = await _httpClient.GetAsync($"units/by-condo/{condominiumId}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (null, $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {Short(body)}");

            try
            {
                var data = JsonSerializer.Deserialize<IEnumerable<string>>(body, _json);
                return (data, null);
            }
            catch (JsonException)
            {
                return (null, "Resposta inesperada ao ler unidades.");
            }
        }

        // helper opcional p/ mensagens curtas
        private static string Short(string s) => s?.Length > 300 ? s[..300] + "…" : s;







    }
}
