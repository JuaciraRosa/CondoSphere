using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CondoSphereMobile.Services
{
    public static class JwtHelper
    {
        public static string GetClaimFromToken(string jwt, string claimKey)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return null;
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            string payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(claimKey, out var val))
                    return val.GetString();

                // tenta chaves alternativas comuns
                if (doc.RootElement.TryGetProperty("nameid", out var nameId))
                    return nameId.GetString();
                if (doc.RootElement.TryGetProperty("sub", out var sub))
                    return sub.GetString();
            }
            catch { /* ignore */ }

            return null;
        }
    }
}
