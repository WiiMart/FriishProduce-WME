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
        static long BlocksToBytes(string blocks) => int.TryParse(blocks?.Split('-').LastOrDefault()?.Trim(), out var c) ? c * 128L * 1024 : 0;

        /// <summary>
        ///     Convert our 4 char TID to a 8 char hexidecimal TID, prepend the rest
        /// </summary>
        static string AsciiToHex(string tid) {
            _ = string.IsNullOrEmpty(tid) ? throw new ArgumentException("Input cannot be null or empty.", nameof(tid)) : 0;
            bool sysChan = tid.StartsWith("H", StringComparison.OrdinalIgnoreCase);
            return sysChan ? "00010002" : "00010001" + string.Concat(Encoding.ASCII.GetBytes(tid).Select(b => b.ToString("X2")));
        }
        
        /// <summary>
        ///     Our Sharpii.exe for CLI calls
        /// </summary>
        private static readonly string Sharpii = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "apps", "sharpii_1.7.3", "Sharpii.exe" );

        /// <summary>
        ///     Simple override for EOA, rather than inputing a full CLI string, just provide the paths, type(WAD/U8) and bool for pack/unpack
        /// </summary>
        private static void RunSharpii(SharpiiTarget type, bool pack, string inPath, string outPath) => RunSharpii($"{type} {(pack ? "-p" : "-u")} \"{inPath}\" \"{outPath}\"");

        private static void Pack(SharpiiTarget type, string inPath, string outPath) => RunSharpii(type, true, inPath, outPath);

        private static void Unpack(SharpiiTarget type, string inPath, string outPath) => RunSharpii(type, false, inPath, outPath);

        /// <summary>
        ///     Run Sharpii and optionally capture output
        /// </summary>
        private static string RunSharpii(string args, bool quiet = true, bool captureOutput = false) {
            if (!File.Exists(Sharpii))
                throw new FileNotFoundException($"Sharpii.exe not found at: {Sharpii}");

            var psi = new ProcessStartInfo {
                FileName = Sharpii, Arguments = args + (quiet ? " -quiet" : ""),
                RedirectStandardOutput = captureOutput, RedirectStandardError = captureOutput, UseShellExecute = false, CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new Exception("Failed to start Sharpii process.");

            string output = null;
            if (captureOutput)
                output = proc.StandardOutput.ReadToEnd();

            proc.WaitForExit();

            if (proc.ExitCode != 0) {
                string err = captureOutput ? proc.StandardError.ReadToEnd() : "";
                throw new Exception($"Sharpii failed with exit code {proc.ExitCode}: {err}");
            }
            return output;
        }

        private static (int Version, string Blocks, int IOS) GetInfo(string wadPath) {
            if (!File.Exists(wadPath))
                throw new FileNotFoundException($"WAD not found at {wadPath}");

            string output = RunSharpii($"WAD -i \"{wadPath}\"", quiet: false, captureOutput: true);
            int version = 0;
            string blocks = null;
            int ios = 0;

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                string trim = line.Trim();
                if (trim.StartsWith("Version:", StringComparison.OrdinalIgnoreCase) && int.TryParse(trim.Substring("Version:".Length).Trim(), out var pVer))
                    version = pVer;
                else if (trim.StartsWith("Blocks:", StringComparison.OrdinalIgnoreCase))
                    blocks = trim.Substring("Blocks:".Length).Trim();
                else if (trim.StartsWith("IOS:", StringComparison.OrdinalIgnoreCase) && int.TryParse(trim.Substring("IOS:".Length).Trim(), out var pIos))
                    ios = pIos;
            }
            return (version, blocks, ios);
        }

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

            // get addition WAD info with Sharpii
            var (wadVersion, blocks, ios) = GetInfo(wadPath);

            var meta = new WadMeta {
                TID = AsciiToHex(md.TitleID),
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
                WadVersion = wadVersion.ToString(),
                IOS = ios.ToString(),
                Blocks = BlocksToBytes(blocks).ToString(),
                InputSettings = md.Settings.List?.ToDictionary(kv => kv.Key, kv => kv.Value),
                InputKeymap = md.Settings.Keymap?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            };

            if (!File.Exists(wadPath))
                throw new FileNotFoundException($"WAD file does not exist: {wadPath}");

            string tempDir = Path.Combine(Path.GetTempPath(), "Sharpii_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try {
                // extract to sys temp dir
                Unpack(SharpiiTarget.WAD, wadPath, tempDir);
                string tempWadDir = Directory.GetDirectories(tempDir).FirstOrDefault() ?? tempDir;
                var bnrU8 = Directory.GetFiles(tempWadDir, "*.app").FirstOrDefault();
                if (bnrU8 == null)
                    throw new FileNotFoundException("No .app file found in extracted WAD.");

                // extract banner U8
                string bnrOut = bnrU8 + "_out";
                Directory.CreateDirectory(bnrOut);
                Unpack(SharpiiTarget.U8, bnrU8, bnrOut);

                // ensure meta folder
                // all banner archives should? have meta folder but just in case for consistent meta.json placement
                string metaFolder = Path.Combine(bnrOut, "meta");
                Directory.CreateDirectory(metaFolder);

                // write our meta.json! :hyperpog: then repack
                var encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                string metaJson = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
                File.WriteAllText(Path.Combine(metaFolder, "meta.json"), metaJson, Encoding.UTF8);
                Pack(SharpiiTarget.U8, bnrOut, bnrU8);

                if (File.Exists(wadPath)) File.Delete(wadPath);
                Pack(SharpiiTarget.WAD, tempWadDir, wadPath);
                Logger.Log($"Packed WAD meta.json inside Banner U8 archive.");
            }
            finally {
                if (Directory.Exists(tempDir)) {
                    try { Directory.Delete(tempDir, true); }
                    catch { Logger.Log($"Failed to delete temp directory: {tempDir}"); }
                }
            }
        }
    }
}