using System.Text.RegularExpressions;

namespace CondoSphere.Services
{
    public static class SimpleHtmlSanitizer
    {
        // permite tags inline comuns + links; remove atributos perigosos
        private static readonly Regex AllowedTags = new(@"</?(p|br|b|strong|i|em|u|ul|ol|li|blockquote|code|pre|span|a)(\s+[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Tags = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex OnEvents = new(@"\son\w+\s*=\s*(['""]).*?\1", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HrefJs = new(@"href\s*=\s*(['""])\s*javascript:.*?\1", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            // remove event handlers e href javascript:
            var cleaned = OnEvents.Replace(html, "");
            cleaned = HrefJs.Replace(cleaned, "href=\"#\"");
            // remove tags não permitidas (mantém texto)
            cleaned = Tags.Replace(cleaned, m => AllowedTags.IsMatch(m.Value) ? m.Value : "");
            return cleaned;
        }
    }
}
