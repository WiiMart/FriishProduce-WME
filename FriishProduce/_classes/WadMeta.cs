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

        public static readonly string SharpiiTemp = Path.Combine(Path.GetTempPath(), "Sharpii_" + Guid.NewGuid());

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
        ///     Our Sharpii.exe for CLI calls
        /// </summary>
        private static readonly string Sharpii = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "apps", "sharpii_1.7.3", "Sharpii.exe" );

        /// <summary>
        ///     Simple override for EOA, rather than inputing a full CLI string, just provide the paths, type(WAD/U8) and bool for pack/unpack
        /// </summary>
        private static void RunSharpii(SharpiiTarget type, bool pack, string inPath, string outPath) =>
            RunSharpii($"{type} {(pack ? "-p" : "-u")} {Utils.SafeQuote(inPath, forceQuotes:true)} {Utils.SafeQuote(outPath, forceQuotes:true)}", quiet: false);

        private static void Pack(SharpiiTarget type, string inPath, string outPath) => RunSharpii(type, true, inPath, outPath);

        private static void Unpack(SharpiiTarget type, string inPath, string outPath) => RunSharpii(type, false, inPath, outPath);

        /// <summary>
        ///     Run Sharpii and optionally capture output
        /// </summary>
        private static string RunSharpii(string args, bool quiet = true) {
            if (!File.Exists(Sharpii))
                throw new FileNotFoundException($"Sharpii.exe not found at: {Sharpii}");

            //string argsLog = args.Replace(SharpiiTemp, "%SharpiiTemp%");
            //Logger.Log($"\nRunning Sharpii with args:\n    {argsLog}");
            //captureOutput = Program.DebugMode;
            var psi = new ProcessStartInfo {
                FileName = Sharpii, Arguments = args + (quiet ? " -quiet" : ""),
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
            };

            Process proc;
            try {
                proc = Process.Start(psi) ?? throw new Exception("Failed to start Sharpii process.");
            }
            catch (System.ComponentModel.Win32Exception ex) {
                Logger.Log($"Win32Exception starting Sharpii.exe: {ex.Message} (ErrorCode: {ex.NativeErrorCode})");
                Logger.Log($"ProcessStartInfo:");
                Logger.Log($"    FileName:\n\"{psi.FileName}\"");
                Logger.Log($"    Arguments:\n\"{psi.Arguments}\"");
                throw;
            }
            using (proc) {
                var outSb = new StringBuilder();
                var errSb = new StringBuilder();
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) outSb.AppendLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) errSb.AppendLine(e.Data); };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                // try to log everything...
                //Logger.Log($"Sharpii output:\n{outSb}");
                if (errSb.Length > 0)
                    Logger.Log($"Sharpii errors:\n{errSb}");

                // Exit code check is still useful, some errors may not catch or produce stderr
                if (proc.ExitCode != 0)
                    throw new Exception($"Sharpii failed with exit code {proc.ExitCode}:\n{errSb}");

                return outSb.ToString();
            }
        }

        private static (string HexTID, int Version, string Blocks, int IOS) GetInfo(string wadPath) {
            if (!File.Exists(wadPath))
                throw new FileNotFoundException($"WAD not found at {wadPath}");

            var output = RunSharpii($"WAD -i {Utils.SafeQuote(wadPath, forceQuotes:true)}", quiet: false);
            string hexTid = null;
            int version = 0;
            string blocks = null;
            int ios = 0;

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                string trim = line.Trim();
                if (trim.StartsWith("Full Title ID:", StringComparison.OrdinalIgnoreCase))
                    hexTid = trim.Substring("Full Title ID:".Length).Trim();
                else if (trim.StartsWith("Version:", StringComparison.OrdinalIgnoreCase) && int.TryParse(trim.Substring("Version:".Length).Trim(), out var pVer))
                    version = pVer;
                else if (trim.StartsWith("Blocks:", StringComparison.OrdinalIgnoreCase))
                    blocks = trim.Substring("Blocks:".Length).Trim();
                else if (trim.StartsWith("IOS:", StringComparison.OrdinalIgnoreCase) && int.TryParse(trim.Substring("IOS:".Length).Trim(), out var pIos))
                    ios = pIos;
            }
            return (hexTid, version, blocks, ios);
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

            // check that sys temp files can be accessed
            Utils.EnsureTempWritable();

            // get additional WAD info with Sharpii
            var (hexTid, wadVersion, blocks, ios) = GetInfo(wadPath);

            var meta = new WadMeta {
                TID = md.TitleID,
                HexTID = hexTid.Replace("-", ""),
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

            if (!File.Exists(SharpiiTemp))
                Directory.CreateDirectory(SharpiiTemp);

            try {
                // extract to sys temp dir
                Unpack(SharpiiTarget.WAD, wadPath, SharpiiTemp);
                string tempWadDir = Directory.GetDirectories(SharpiiTemp).FirstOrDefault() ?? SharpiiTemp;
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
                if (Directory.Exists(SharpiiTemp)) {
                    try { Directory.Delete(SharpiiTemp, true); }
                    catch { Logger.Log($"Failed to delete temp directory: {SharpiiTemp}"); }
                }
            }
        }
    }
}