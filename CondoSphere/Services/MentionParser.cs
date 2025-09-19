using System.Text.RegularExpressions;

namespace CondoSphere.Services
{
    public static class MentionParser
    {
        // suporta @email e @username simples (letras, números, ., -, _)
        private static readonly Regex Rx = new(@"@([A-Za-z0-9._-]+@[A-Za-z0-9._-]+\.[A-Za-z]{2,}|[A-Za-z0-9._-]{3,})", RegexOptions.Compiled);

        public static IEnumerable<string> Extract(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            foreach (Match m in Rx.Matches(text))
                yield return m.Groups[1].Value;
        }
    }
}
