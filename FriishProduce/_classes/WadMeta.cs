using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;

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
        ///     Gets the byte value of the max blocks a WAD occupies
        /// </summary>
        static long BlocksToBytes(string blocks) =>
            int.TryParse(blocks?.Split('-').LastOrDefault()?.Trim(), out var c) ? c * 128L * 1024 : 0;

        /// <summary>
        ///     Convert to hex manually... Since I can't seem to find a hex TID from LWS
        /// </summary>
        public static string HexifyTID(string tid) => 
            $"{(tid.StartsWith("H") ? "00010002" : "00010001")}{string.Concat(tid.Select(c => ((int)c).ToString("X2")))}";

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
                ChannelRegion = ((libWiiSharp.Region) md.WadRegion).ToString(),
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