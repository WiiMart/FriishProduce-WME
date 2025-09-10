using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FriishProduce
{
    public class GoogleTrans
    {
        private static readonly HttpClient client = new();

        public static bool ContainsCJK(string input) {
            return !string.IsNullOrEmpty(input) && input.Any(c => (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF)
                || (c >= 0x3040 && c <= 0x309F) || (c >= 0x30A0 && c <= 0x30FF) || (c >= 0xAC00 && c <= 0xD7AF));
        }

        public static async Task<string> Translate(string text, string targetLang = "en") {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
            string result = await client.GetStringAsync(url);
            return string.Join("", JArray.Parse(result)[0].Select(t => t[0].ToString()));
        }

        /// <summary>
        ///     Translate the string to the selected language, or system language, else returns the given string
        /// </summary>
        public static async Task<string> PrefTranslate(string text) {
            if (string.IsNullOrWhiteSpace(text)) return text;

            string programLang = Program.Lang?.Current;
            if (!string.IsNullOrEmpty(programLang) && !programLang.StartsWith("en", StringComparison.OrdinalIgnoreCase)) {
                string translated = await Translate(text, new CultureInfo(programLang).TwoLetterISOLanguageName);
                if (!string.IsNullOrEmpty(translated) && !string.Equals(translated, text, StringComparison.OrdinalIgnoreCase))
                    return translated;
            }

            string systemLang = Program.Lang?.GetSystemLanguage() ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (!string.IsNullOrEmpty(systemLang) && !systemLang.StartsWith("en", StringComparison.OrdinalIgnoreCase)) {
                string translated = await Translate(text, new CultureInfo(systemLang).TwoLetterISOLanguageName);
                if (!string.IsNullOrEmpty(translated) && !string.Equals(translated, text, StringComparison.OrdinalIgnoreCase))
                    return translated;
            }

            return text;
        }
    }
}