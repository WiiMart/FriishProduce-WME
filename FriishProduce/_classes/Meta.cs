using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using libWiiSharp;

namespace FriishProduce
{
    /// <summary>
    ///     Object for storing platform name and controller html from MetaLocalizations
    /// </summary>
    public class PlatformInfo {
        // Platform name
        public string Name { get; set; }
        // Controller html
        public string Controllers { get; set; }
    }

    /// <summary>
    ///     Object for storing lang, players count, date and $PlatformInfo info for a region
    /// </summary>
    public class LocalInfo {
        // Language
        public string Lang { get; set; }
        // Formatted players
        public string Players { get; set; }
        public string DateFormat { get; set; }
        // Contains the normalized Platform tag ("snes", "nes") and PlatformInfo for said tag
        public Dictionary<string, PlatformInfo> PlatformDict { get; set; }
    }

    public class Meta {
        public string TID { get; set; }
        public string HexTID { get; set; }
        public string Platform { get; set; }
        public string Publisher { get; set; }
        public string WadBase { get; set; }
        public string ChannelTitle { get; set; }
        public string[] BannerTitle { get; set; }
        public string[] SaveTitles { get; set; }
        public string Released { get; set; }
        public string Players { get; set; }
        public string Genre { get; set; }
        public string BannerBmp { get; set; }
        public string VideoMode { get; set; }
        public string WadRegion { get; set; }
        public string ChannelRegion { get; set; }
        public string BannerRegion { get; set; }
        public string WadVersion { get; set; }
        public string IOS { get; set; }
        public string Blocks { get; set; }
        public IDictionary<string, string> InputSettings { get; set; }
        public IDictionary<string, string> InputKeymap { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("oss.common.js.games")]
        public List<Dictionary<string, object>> RegionalBlocks { get; set; }


        /// <summary>
        ///     Used to determine which WAD inject conflicts exists
        /// </summary>
        public static readonly string 
            BNR_REG_WARN = "$BNRREG", BNR_IMG_WARN = "$BNRIMG", CHL_REG_WARN = "$CHLREG", VDM_WARN = "$VDM", SVT_WARN = "$SVT";

        /// <summary>
        ///     Video mode array for 'pretty' printing
        /// </summary>
        public static readonly string[] VidModes = {
            Program.Lang.String("original"), "NTSC", "MPAL", "PAL60", "PAL50", "NTSC+MPAL", "PAL60+PAL50", "NTSC+PAL60", "MPAL+PAL50"
        };
        
        /// <summary>
        ///     Gets the formal name from the VidModes array for the WadVideoMode selected index
        /// </summary>
        public static string GetVidModeFor(int index) {
            return index < 0 || index >= VidModes.Length ? "Unknown" : VidModes[index];
        }

        /// <summary>
        ///     Gets the expected video mode for the region
        /// </summary>
        public static string[] GetVidModeFor(Region region) {
            string[] ntsc = new[] { Program.Lang.String("original"), "NTSC" };
            return region switch {
                Region.USA => ntsc, Region.Japan => ntsc, Region.Korea => ntsc,
                Region.Europe => new[] { Program.Lang.String("original"), "PAL50", "PAL60" },
                _ => new[] { Program.Lang.String("original") }
            };
        }

        /// <summary>
        ///     Gets the byte value of the max blocks a WAD occupies
        /// </summary>
        static long BlocksToBytes(string blocks) =>
            int.TryParse(blocks?.Split('-').LastOrDefault()?.Trim(), out var c) ? c * 128L * 1024 : 0;

        /// <summary>
        ///     Convert to hex manually... Since I can't seem to find a hex TID from LWS
        /// </summary>
        public static string HexifyTID(string tid) => 
            $"{(tid.StartsWith("H") ? "00010002" : "00010001")}{string.Concat(tid.Select(c => ((int)c).ToString("X2")))}";

        public static Region IntToRegion(int val) => val switch {
            0 => Region.Japan, 2 => Region.Europe, 3 => Region.Korea, _ => Region.USA
        };

        public static int RegToInt(Region region) => region switch {
            Region.Japan => 0, Region.Europe => 2, Region.Korea => 3, _ => 1
        };

        public static int ChRegToInt(string region) => region switch {
            var reg when reg == Program.Lang.String("original") => 0,
            var reg when reg == Program.Lang.String("region_rf") => 1,
            var reg when reg == Program.Lang.String("region_u") => 2,// || reg == "USA" => 2,
            var reg when reg == Program.Lang.String("region_e") => 3,
            var reg when reg == Program.Lang.String("region_j") => 4,
            var reg when reg == Program.Lang.String("region_k") => 5,
            _ => 0
        };

        public static string IntToChReg(int val) => val switch {
            0 => Program.Lang.String("original"),
            1 => Program.Lang.String("region_rf"),
            2 => Program.Lang.String("region_u"),
            3 => Program.Lang.String("region_e"),
            4 => Program.Lang.String("region_j"),
            5 => Program.Lang.String("region_k"),
            _ => Program.Lang.String("original")
        };

        public static Region VidModeToRegion(string baseRegion) => NormalizeRegion(baseRegion) switch {
            //"USA" => Region.USA,
            var reg when reg == Program.Lang.String("region_u") => Region.USA,
            var reg when reg == Program.Lang.String("region_e") => Region.Europe,
            var reg when reg == Program.Lang.String("region_j") => Region.Japan,
            var reg when reg == Program.Lang.String("region_k") => Region.Korea,
            _ => Region.USA
        };

        /// <summary>
        ///     Checks if there are *any* region conflicts in the string array
        ///         (arr should consist of banner, channel, and base wad regions)
        /// </summary>
        public static bool HasRegConflict(params string[] regions) => GetConflictSrcs(regions).Count > 0;

        /// <summary>
        ///     Compares the VidModes string array to the selected video mode to find any conflicts with WAD region 
        /// </summary>
        public static bool HasVidModeConflict(string selected, Region baseRegion) => Array.IndexOf(GetVidModeFor(baseRegion), selected) < 0;

        /// <summary>
        ///     Compares the VidModes SelectedIndex to the selected video mode using GetVidModeFor(int) to find any conflicts with WAD region 
        /// </summary>
        public static bool HasVidModeConflict(int selected, Region baseRegion) => Array.IndexOf(GetVidModeFor(baseRegion), GetVidModeFor(selected)) < 0;

        /// <summary>
        ///     Trims whitespace and replaces occurences in the List with their shortforms for consistent matching
        /// </summary>
        private static string NormalizeRegion(string region) {
            if (string.IsNullOrEmpty(region)) return "";
            var replacements = new List<(string from, string to)> {
                ("America", "USA"), ("United States", "USA"), ("USA", Program.Lang.String("region_u")),
                ("Europe", Program.Lang.String("region_e")),
                ("Japan", Program.Lang.String("region_j")),
                ("Korea", Program.Lang.String("region_k"))
            };
            replacements.ForEach(reg => region = region.Replace(reg.from, reg.to));
            return region.Trim();
        }

        /// <summary>
        ///     Compare base, banner, and channel regions after 'normalizing' the strings
        ///         Uses #NormalizeRegions() to replace occurences in a List with their shortforms for consistent matching
        /// </summary>
        public static List<string> GetConflictSrcs(params string[] matchParams) {
            string banner = NormalizeRegion(matchParams.ElementAtOrDefault(0) ?? "");
            string channel = NormalizeRegion(matchParams.ElementAtOrDefault(1) ?? "");
            string baseRegion = matchParams.ElementAtOrDefault(2) ?? "";
            string baseRegionTxt = NormalizeRegion(baseRegion);
            int vidMode = (int.TryParse(matchParams.ElementAtOrDefault(3), out var idx) ? idx : -1);
            string saveTitle = matchParams.ElementAtOrDefault(4) ?? "";
            string bannerImg = matchParams.ElementAtOrDefault(5) ?? "";
            string[] wildcards = { Program.Lang.String("original"), Program.Lang.String("automatic") };

            bool Matches(string lhs, string rhs) =>
                lhs != "Region-Free" && rhs != "Region-Free" && (Array.Exists(wildcards, wc => wc == lhs) || Array.Exists(wildcards, wc => wc == rhs) || lhs == rhs);

            var conflictMap = new (string tag, bool condition)[] {
                (BNR_REG_WARN, !Matches(banner, baseRegionTxt)),
                (CHL_REG_WARN, !Matches(channel, baseRegionTxt)),
                (VDM_WARN, vidMode >= 0 && HasVidModeConflict(vidMode, VidModeToRegion(baseRegion))),
                (SVT_WARN, string.IsNullOrEmpty(saveTitle)),
                (BNR_IMG_WARN, string.IsNullOrEmpty(bannerImg))
            };
            return conflictMap.Where(c => c.condition).Select(c => c.tag).ToList();
        }

        /// <summary>
        ///     Points per platform...
        /// </summary>
        private static readonly Dictionary<string, string> PointMap = new() {
            ["nes"] = "500", ["snes"] = "800",
            ["smd"] = "800", ["sms"] = "500",
            ["n64"] = "1000",
            ["c64"] = "500",
            ["tg16"] = "800", ["tgcd"] = "800",
            ["pce"] = "800", ["pcecd"] = "800",
            ["flash"] = "0",
            ["neo"] = "900",
            ["msx"] = "700"
        };

        /// <summary>
        ///     Builds the html controller info per platform with the localized string
        /// </summary>
        private static string GetCtrlHtml(string platform, string ctrl) {
            if (string.IsNullOrEmpty(ctrl)) return "";

            string[] lines = ctrl.Split(new[] { "<L2>" }, StringSplitOptions.None);

            string ctrldv = "<div id=\"controller\" align=\"center\">";
            string rvl = "<img src=\"/oss/oss/common/images//banner/B_08_RvlCtrl.gif\" width=\"68\" height=\"51\" />";
            string cls = "<img src=\"/oss/oss/common/images//banner/B_08_ClassicCtrl.gif\" width=\"68\" height=\"51\" />";
            string gcn = "<div id=\"GcCtrl\" style=\"display: inline-block; _display: inline;\">" +
                        "<img src=\"/oss/oss/common/images//banner/B_08_GcCtrl.gif\" width=\"68\" height=\"51\" />" +
                        "</div>";
            string text03 = "<div id=\"text03-01\"><div align=\"center\"><div align=\"left\" class=\"contentsBlackS\">";
            string text04 = "<div id=\"text04-01\"><div align=\"center\"><div align=\"left\" class=\"contentsBlackS\">";
            string cls3d = "</div></div></div>";

            return platform.ToLower() switch {
                "nes" or "sms" or "smd" => $"{ctrldv}{rvl}{cls}{gcn}</div>{text03}{lines[0]}{cls3d}{text04}{(lines.Length > 1 ? lines[1] : "")}{cls3d}",
                "n64" => $"{ctrldv}{cls}{gcn}</div>{text03}{lines[0]}{cls3d}{text04}{(lines.Length > 1 ? lines[1] : "")}{cls3d}",
                "wii" => $"{ctrldv}{rvl}</div>{text03}{lines[0]}{cls3d}{text04}{(lines.Length > 1 ? lines[1] : "")}{cls3d}",
                _ => ctrl
            };
        }

        /// <summary>
        ///     Formates the date to full width numerals if JP or 2025 if problematic
        /// </summary>
        public static string FormatDate(string date, string dateFormat, string region) {
            string year = DateTime.Now.Year.ToString(); // default to current year
            year = !string.IsNullOrEmpty(date) && date.Length >= 4 ? date.Substring(0, 4) : year;
            return region == "JP" ? string.Concat(year.Select(c => char.IsDigit(c) ? (char)(c + 0xFF10 - '0') : c)) : year;
        }

        private static string FormatPlayers(string format, int count, string region) {
            string tCount = count.ToString();
            tCount = region == "JP" ? string.Concat(tCount.Select(c => char.IsDigit(c) ? (char)(c + 0xFF10 - '0') : c)) : tCount;
            format = count != 1 && region == "NL" ? format.Replace("speler", "spelers") : format;
            return format.Replace("#", tCount);
        }

        private static string NormalizePlatform(string platformStr) 
            => !string.IsNullOrEmpty(platformStr) ? platformStr.ToLowerInvariant().Replace("pcecd", "pce") : null;

        /// <summary>
        ///     Normalize WadRegion names so GetLocalPair is consistent
        /// </summary>
        private static string NormalizeWadReg(string wadRegion) {
            return wadRegion switch {
                "US" or "USA" or "America" => "USA",
                "EU" or "EUR" or "Europe" => "Europe",
                "JP" or "Japan" => "Japan",
                "KR" or "Korea" => "Korea",
                _ => wadRegion
            };
        }

        /// <summary>
        ///     Creates an array of tuples containing region and lang flags for the provided #wadRegion
        /// </summary>
        public static (string region, string language)[] GetLocalPair(string wadRegion) {
            return string.IsNullOrEmpty(wadRegion) ? Array.Empty<(string, string)>() : NormalizeWadReg(wadRegion) switch {
                "USA" => new[] { ("US", "EN"), ("CA", "FR"), ("MX", "ES") },
                "Europe" => new[] { ("GB", "EN"), ("FR", "FR"), ("DE", "DE"), ("IT", "IT"), ("ES", "ES"), ("NL", "NL") },
                "Japan" => new[] { ("JP", "JA") },
                "Korea" => new[] { ("KR", "KO") },
                _ => new[] { (wadRegion, "") },
            };
        }

        /// <summary>
        ///     Generates the JSON blocks for the given WAD $Meta
        /// </summary>
        public static void Generate(Meta meta) {
            if (meta == null) return;
            var regionLangPairs = GetLocalPair(meta.WadRegion);
            var addedRegions = new List<string>();

            foreach (var (region, lang) in regionLangPairs) {
                if (!MetaLocalizations.LocalDict.TryGetValue(region, out var loc)) {
                    Logger.WARN($"No local data found for region '{region}'");
                    continue;
                }
                string normPlatform = NormalizePlatform(meta.Platform);
                if (!loc.PlatformDict.TryGetValue(normPlatform, out var platform))  {
                    Logger.WARN($"Platform '{platform.Name}' not found in region '{region}'");
                    continue;
                }

                string fmLang = string.IsNullOrEmpty(lang) ? loc.Lang : lang;
                string fmGenre;
                try {
                    fmGenre = GoogleTrans.Translate(meta.Genre, fmLang).GetAwaiter().GetResult();
                }
                catch (Exception ex) {
                    Logger.WARN($"Translation failed for '{meta.Genre}' -> '{fmLang}': {ex.Message}");
                    fmGenre = meta.Genre;
                }
                var infoBlock = new Dictionary<string, object> {
                    ["id"] = meta.HexTID,
                    ["title1"] = meta.BannerTitle?.FirstOrDefault() ?? "",
                    ["title2"] = meta.BannerTitle?.Skip(1).FirstOrDefault() ?? "",
                    ["console"] = platform.Name,
                    ["controllers"] = GetCtrlHtml(normPlatform, platform.Controllers),
                    ["region"] = region,
                    ["language"] = (region == "JP" || region == "KR") ? "" : fmLang,
                    ["attributes"] = "",
                    ["date"] = FormatDate(meta.Released, loc.DateFormat, region),
                    ["added"] = "",
                    ["publisher"] = meta.Publisher,
                    ["genre"] = fmGenre,
                    ["points"] = PointMap.TryGetValue(normPlatform, out var pts) ? pts : "0",
                    ["players"] = FormatPlayers(loc.Players, int.TryParse(meta.Players, out var p) ? p : 1, region),
                    ["rating"] = "",
                    ["ratingdetails"] = "",
                    ["link"] = "",
                    ["mirror"] = "",
                    ["size"] = meta.Blocks,
                    ["thumbnail"] = "FFFD0001"
                };
                meta.RegionalBlocks ??= new List<Dictionary<string, object>>();
                meta.RegionalBlocks.Add(infoBlock);
                addedRegions.Add(region);
            }
            if (addedRegions.Any()) {
                string plur = addedRegions.Count > 1 ? "s" : "";
                Logger.Sub($"Added game info block{plur}/object{plur} for platform \"{meta.Platform}\" in region{plur}: {string.Join(", ", addedRegions)}");
            }
        }

        /// <summary>
        ///     Injects meta.json into the Banner U8 archive using LibWiiSharp dep
        /// </summary>
        internal static void Write(Method md, string wadPath, ProjectForm.Region InWadRegion) {
            if (md.WAD == null)
                throw new InvalidOperationException("WAD must be loaded before writing metadata.");

            string base64Banner = "";
            // unedited original image, "VCPic" has rounded edges applied
            if (md.Img?.Source != null) {
                using MemoryStream ms = new();
                md.Img.Source.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                base64Banner = Convert.ToBase64String(ms.ToArray());
            }
            Utils.EnsureTempWritable();

            var meta = new Meta {
                TID = md.TitleID,
                HexTID = HexifyTID(md.TitleID),
                Platform = md.Platform.ToString(),
                Publisher = Program.Config.application.publisher_opt_tb,
                WadBase = System.Net.WebUtility.UrlDecode(Path.GetFileNameWithoutExtension(md.SrcBase)),
                ChannelTitle = md.ChannelTitles.Length > 1 ? md.ChannelTitles[1] : md.ChannelTitles.FirstOrDefault() ?? "",
                BannerTitle = md.BannerTitle?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
                SaveTitles = md.SaveDataTitle,
                Released = md.BannerYear.ToString(),
                Players = md.BannerPlayers.ToString(),
                Genre = md.Genre,
                BannerBmp = base64Banner,
                VideoMode = Meta.GetVidModeFor(md.WadVideoMode),
                WadRegion = InWadRegion.ToString(),
                ChannelRegion = ((Region) md.WadRegion).ToString(),
                BannerRegion = md.BannerRegion.ToString(),
                WadVersion = md.WAD.TitleVersion.ToString(),
                IOS = ((int)(md.WAD.StartupIOS & 0xFFFFFFFF)).ToString(),
                Blocks = BlocksToBytes(md.WAD.NandBlocks.ToString()).ToString(),
                InputSettings = md.Settings.List?.ToDictionary(kv => kv.Key, kv => kv.Value),
                InputKeymap = md.Settings.Keymap?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            };
            Generate(meta);

            // convert meta.json to bytes and pack
            var encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            string metaJson = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
            metaJson = metaJson.Replace("\"oss.common.js.games\": [", "\n  \"oss.common.js.games\": [");
            byte[] metaBytes = Encoding.UTF8.GetBytes(metaJson);

            md.WAD.BannerApp.AddFile("meta/meta.json", metaBytes);
            if (File.Exists(wadPath)) File.Delete(wadPath);
            md.WAD.Save(wadPath);

            Logger.Sub("Packed WAD meta.json into the \"meta\" folder inside the \"Banner\" U8 archive.");
        }
    }
}