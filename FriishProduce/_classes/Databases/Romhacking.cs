using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FriishProduce
{
    public class RomhackEntry {
        public string PageUrl { get; set; }
        public string ThumbUrl { get; set; }
        public string Title { get; set; }
        public string LowerTitle { get; set; }
        public string UpperTitle { get; set; }
        public string Genre { get; set; }
        public string Released { get; set; }
        public string FirstCrc32 { get; set; }
        public List<string> AllDLUrls { get; set; } = new();
    }

    public static class Romhacking {
        private const RegexOptions RX_SI = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
        private static readonly Regex TagsRegx = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex CrcRegx = new(@"CRC32:\s*([A-Fa-f0-9]{8})", RegexOptions.Compiled);
        private static readonly Regex FuzzCrcRegx = new(@"\b([A-Fa-f0-9]{8})\b", RegexOptions.Compiled);
        private static readonly Regex GenreRegx = new(@"<th>\s*Genre\s*</th>\s*<td[^>]*>\s*(?<val>.*?)\s*</td>", RX_SI);
        private static readonly Regex ReleaseRegx = new(@"<th>\s*Release Date\s*</th>\s*<td[^>]*>\s*(?<val>.*?)\s*</td>", RX_SI);
        private static readonly Regex GalleryRegx = new(@"<div\s+class\s*=\s*[""']imageGallery[""'][^>]*>(?<content>.*?)</div>", RX_SI);
        private static readonly Regex DivCenterRegx = new(@"<div\s+class\s*=\s*[""']center[""'][^>]*>(?<val>.*?)</div>", RX_SI);
        private static readonly Regex ImgHrefRegx = new(@"<a[^>]+href\s*=\s*[""'](?<href>https?://[^""']+)[""'][^>]*>\s*<img[^>]+src\s*=\s*[""'](?<src>https?://[^""']+)[""'][^>]*>", RX_SI);
        private static readonly Regex ImgWrapRegx = new(@"<img[^>]+src\s*=\s*[""'](?<src>https?://[^""']+)[""'][^>]*>", RX_SI);
        private static readonly Regex LinksListRegx = new(@"<h3>\s*Links:\s*</h3>\s*<ul>(?<ul>.*?)</ul>", RX_SI);
        private static readonly Regex AnchorRegx = new(@"<a[^>]+href\s*=\s*[""'](?<href>[^""'#>]+)[""'][^>]*>(?<text>.*?)</a>", RX_SI);
        private static readonly StringComparison IGCASE = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        ///     Fetches and parses a hack/translation page from Romhacking.net
        /// </summary>
        public static RomhackEntry GetEntry(string pageUrl) {
            if (string.IsNullOrWhiteSpace(pageUrl)) 
                throw new ArgumentNullException(nameof(pageUrl));

            var entry = new RomhackEntry {
                PageUrl = pageUrl.Trim()
            };
            if (!pageUrl.StartsWith("http://", IGCASE) && !pageUrl.StartsWith("https://", IGCASE))
                pageUrl = "https://" + pageUrl.Trim();

            // use existing $Web #Get() method
            byte[] bytes = Web.Get(pageUrl, $"\nFetching from:\n\"{pageUrl}\"\n");
            bool isHack = pageUrl.IndexOf("/hacks/", StringComparison.OrdinalIgnoreCase) >= 0;
            string html = GetEncString(bytes);

            // title screen inside imageGallery
            var igMatch = GalleryRegx.Match(html);
            if (igMatch.Success) {
                string content = igMatch.Groups["content"].Value;
                if (ImgHrefRegx.Match(content).Success || ImgWrapRegx.Match(content).Success)
                    entry.ThumbUrl = HtmlDecode(ImgHrefRegx.Match(content).Groups[ImgHrefRegx.Match(content).Success ? "href" : "src"].Value.Trim());
            }

            // match #main
            var mainMatch = Regex.Match(html, @"<div id=""main""\s*>(.*?)</div>\s*</div>", RX_SI);
            if (mainMatch.Success) {
                string mainHtml = mainMatch.Groups[1].Value;
                // match h2 inside topbar within #main
                var h2Match = Regex.Match(mainHtml, @"<div class=""topbar"">\s*<h2>(?<val>.*?)</h2>", RX_SI);
                if (h2Match.Success) {
                    entry.UpperTitle = CleanInner(h2Match.Groups["val"].Value);
                }
            }

            // title after the image, find the center div that appears after imageGallery
            if (igMatch.Success) {
                int posAfterIg = igMatch.Index + igMatch.Length;
                foreach (Match m in DivCenterRegx.Matches(html))
                    if (m.Index > posAfterIg) {
                        entry.LowerTitle = CleanInner(m.Groups["val"].Value);
                        break; 
                    }
            }
            else if (DivCenterRegx.Match(html).Success)
                entry.LowerTitle = CleanInner(DivCenterRegx.Match(html).Groups["val"].Value);

            entry.Title = GetTitleFor(entry, isHack);

            if (GenreRegx.Match(html).Success)
                entry.Genre = NormalizeGenre(HtmlDecode(StripTags(GenreRegx.Match(html).Groups["val"].Value).Trim()));

            if (ReleaseRegx.Match(html).Success)
                entry.Released = HtmlDecode(StripTags(ReleaseRegx.Match(html).Groups["val"].Value).Trim());

            // CRC32, prefers rom_info block
            string romInfoBlock = null;
            var romInfoIdx = html.IndexOf("id=\"rom_info\"", IGCASE);
            if (romInfoIdx >= 0) {
                var sub = html.Substring(romInfoIdx);
                var endDiv = sub.IndexOf("</div>", IGCASE);
                romInfoBlock = endDiv > 0 ? sub.Substring(0, endDiv + 6) : sub;
            }

            if (!string.IsNullOrEmpty(romInfoBlock) && (CrcRegx.Match(romInfoBlock).Success || FuzzCrcRegx.Match(romInfoBlock).Success)) 
                entry.FirstCrc32 = (CrcRegx.Match(romInfoBlock).Success ? CrcRegx.Match(romInfoBlock) : FuzzCrcRegx.Match(romInfoBlock)).Groups[1].Value.ToUpperInvariant();

            else if (CrcRegx.Match(html).Success || FuzzCrcRegx.Match(html).Success)
                entry.FirstCrc32 = (CrcRegx.Match(html).Success ? CrcRegx.Match(html) : FuzzCrcRegx.Match(html)).Groups[1].Value.ToUpperInvariant();

            // store URLs
            if (LinksListRegx.Match(html).Success) {
                string linksHtml = LinksListRegx.Match(html).Groups["ul"].Value;
                foreach (Match a in AnchorRegx.Matches(linksHtml)) {
                    string href = a.Groups["href"].Value.Trim();
                    string full = GetAbsolute(pageUrl, href);
                    entry.AllDLUrls.Add(full);
                }
            }
            return entry;
        }

        /// <summary>
        ///     Prompt to open the first valid Romhacking download link from our list of URLs
        /// </summary>
        public static void PromptDLPatch(List<string> urls) {
            if (urls == null) return;
            // Find first URL that starts with the download prefix
            var dlUrl = urls.FirstOrDefault(u => u.StartsWith("https://www.romhacking.net/download/", StringComparison.OrdinalIgnoreCase));
            if (dlUrl == null) return;

            //string msg = $"Download patch/redirect in your default browser?\n\n{dlUrl}";
            if (MessageBox.Show(Program.Lang.String("fetch_patch_msg") + dlUrl, MessageBox.Buttons.YesNo, MessageBox.Icons.Custom) == MessageBox.Result.Yes) {
                try {
                    Process.Start(new ProcessStartInfo { FileName = dlUrl, UseShellExecute = true });
                }
                catch (Exception ex) {
                    Logger.ERROR($"Failed to open browser: {ex.Message}");
                }
            }
        }

        private static string GetTitleFor(RomhackEntry entry, bool isHack) {
            return isHack ? entry.UpperTitle ?? entry.LowerTitle : entry.LowerTitle ?? entry.UpperTitle;
        }

        private static string GetEncString(byte[] bytes) {
            try {
                return (bytes != null || bytes.Length != 0) ? Encoding.UTF8.GetString(bytes) : string.Empty;
            } catch {
                return Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            }
        }

        private static string GetAbsolute(string baseUrl, string href) {
            if (string.IsNullOrWhiteSpace(href))
                return href;

            href = href.Trim();
            try {
                if (href.StartsWith("//") || href.StartsWith("/")) {
                    var uri = new Uri(baseUrl);
                    return href.StartsWith("//") ? "https:" + href : $"{uri.Scheme}://{uri.Host}{href}";
                }
                Uri.TryCreate(href, UriKind.Absolute, out var abs);
                return abs != null && !string.IsNullOrEmpty(abs.ToString()) ? abs.ToString() : new Uri(new Uri(baseUrl), href).ToString();
            }
            catch { return href; }
        }

        private static string NormalizeGenre(string raw) {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var cleaned = raw.Replace(">", ",").Replace("-", ",").Replace("|", ",");
            var parts = cleaned.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0);
            return string.Join(", ", parts);
        }

        private static string HtmlDecode(string html) => WebUtility.HtmlDecode(html ?? string.Empty);

        private static string StripTags(string tag) => TagsRegx.Replace(tag ?? string.Empty, string.Empty);

        private static string CleanInner(string inner) => string.IsNullOrEmpty(inner) ? inner : HtmlDecode(StripTags(inner)).Trim();
    }
}