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
    public class WadMeta {
        public string TID { get; set; }
        public string HexTID { get; set; }
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
        private enum SharpiiTarget { WAD, U8 }

        /// <summary>
        ///     Used to determine which WAD inject conflicts exists
        /// </summary>
        public static readonly string 
            BNR_REG_WARN = "$BNRREG", BNR_IMG_WARN = "$BNRIMG", CHL_REG_WARN = "$CHLREG", VDM_WARN = "$VDM", SVT_WARN = "$SVT";

        /// <summary>
        ///     Video mode array for 'pretty' printing
        /// </summary>
        public static readonly string[] VidModes = {
            "Original", "NTSC", "MPAL", "PAL60", "PAL50", "NTSC+MPAL", "PAL60+PAL50", "NTSC+PAL60", "MPAL+PAL50"
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
            string[] ntsc = new[] { "Original", "NTSC" };
            return region switch {
                Region.USA => ntsc, Region.Japan => ntsc, Region.Korea => ntsc,
                Region.Europe => new[] { "Original", "PAL50", "PAL60" },
                _ => new[] { "Original" }
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
            "Original" => 0, "Region-Free" => 1, "USA" => 2, "Europe" => 3, "Japan" => 4, "Korea" => 5, _ => 0
        };

        public static string IntToChReg(int val) => val switch {
            0 => "Original", 1 => "Region-Free", 2 => "USA", 3 => "Europe", 4 => "Japan", 5 => "Korea", _ => "Original"
        };

        public static Region VidModeToRegion(string baseRegion) => NormalizeRegion(baseRegion) switch {
            "USA" => Region.USA, "Europe" => Region.Europe, "Japan" => Region.Japan, "Korea" => Region.Korea, _ => Region.USA
        };

        /// <summary>
        ///     Trims whitespace and replaces occurences in the List with their shortforms for consistent matching
        /// </summary>
        private static string NormalizeRegion(string region) {
            if (string.IsNullOrEmpty(region)) return "";
            var replacements = new List<(string from, string to)> {
                ("America", "USA"), ("United States", "USA"), ("Europe/Australia", "Europe"), ("Republic of Korea", "Korea")
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
            string[] wildcards = { "Original", "Automatic" };

            bool Matches(string lhs, string rhs) {
                if (lhs == "Region-Free" || rhs == "Region-Free") return false;
                if (Array.Exists(wildcards, wc => wc == lhs) || Array.Exists(wildcards, wc => wc == rhs)) return true;
                return lhs == rhs;
            }
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

            var meta = new WadMeta {
                TID = md.TitleID,
                HexTID = HexifyTID(md.TitleID),
                Publisher = Program.Config.application.publisher_opt_tb,
                WadBase = System.Net.WebUtility.UrlDecode(Path.GetFileNameWithoutExtension(md.SrcBase)),
                ChannelTitle = md.ChannelTitles.Length > 1 ? md.ChannelTitles[1] : md.ChannelTitles.FirstOrDefault() ?? "",
                BannerTitle = md.BannerTitle?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
                SaveTitles = md.SaveDataTitle,
                Released = md.BannerYear.ToString(),
                Players = md.BannerPlayers.ToString(),
                Genre = md.Genre,
                BannerBmp = base64Banner,
                VideoMode = WadMeta.GetVidModeFor(md.WadVideoMode),
                WadRegion = InWadRegion.ToString(),
                ChannelRegion = ((Region) md.WadRegion).ToString(),
                BannerRegion = md.BannerRegion.ToString(),
                WadVersion = md.WAD.TitleVersion.ToString(),
                IOS = ((int)(md.WAD.StartupIOS & 0xFFFFFFFF)).ToString(),
                Blocks = BlocksToBytes(md.WAD.NandBlocks.ToString()).ToString(),
                InputSettings = md.Settings.List?.ToDictionary(kv => kv.Key, kv => kv.Value),
                InputKeymap = md.Settings.Keymap?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            };

            // convert meta.json to bytes and pack
            var encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            string metaJson = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
            byte[] metaBytes = Encoding.UTF8.GetBytes(metaJson);

            md.WAD.BannerApp.AddFile("meta/meta.json", metaBytes);
            if (File.Exists(wadPath)) File.Delete(wadPath);
            md.WAD.Save(wadPath);

            Logger.Log("Packed WAD meta.json inside the Banner U8 archive => 'meta' folder.");
        }

    }
}