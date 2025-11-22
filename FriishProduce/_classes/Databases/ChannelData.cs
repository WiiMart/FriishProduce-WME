using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FriishProduce;

namespace FriishProduce
{
    public class ChannelDatabase {
        private readonly string dbErr = "The database format or styling is not valid.";

        public class ChannelEntry {
            public string ID { get; set; }
            public int Count { get => (Regions?.Count == Titles?.Count) && (Titles?.Count == MarioCube?.Count) ? Titles?.Count ?? 0 : -1; }
            public List<int> Regions = new();
            public List<string> Titles = new();
            public List<int> EmuRevs = new();
            public List<string> MarioCube = new();
            public List<string> RomIDs { get; set; } = new(); 

            public string GetID(int index, bool raw = false) {
                string reg = "41";
                reg = Regions[index] switch {
                    1 => "45", 2 => "4e", 3 => "50", 4 => "4c", 5 => "4d", 6 => "51", 7 => "54", _ => "4a",
                };
                return raw ? ID.Replace("__", reg).Replace("-", "").ToLower() : ID.Replace("__", reg).ToLower();
            }

            public string GetUpperID(int index) {
                if (index == -1) return null;
                string hex = GetID(index).Substring(ID.Length - 8);
                return string.Concat(Enumerable.Range(0, hex.Length / 2).Select(i => (char) Convert.ToInt64(hex.Substring(i * 2, 2), 16))).ToUpper();
            }
            public string[] GetUpperIDs() => Enumerable.Range(0, Regions.Count).Select(GetUpperID).ToArray();

            /// <summary>
            /// Gets a WAD file from the entry corresponding to the index entered.
            /// </summary>
            public string GetWAD(int index) {
                string tid = GetUpperID(index);

                // Load Static WAD by default
                // ****************
                if (tid == null || tid?.Length < 4 || tid == "STLB")
                    return null;

                if (DropboxDL.FindUrlFor(tid) != null)
                    return DropboxDL.FindUrlFor(tid);

                string reg = Regions[index] switch {
                    1 or 2 => " (USA)",
                    3 or 4 or 5 => " (Europe)",
                    6 => " (Korea) (Ja,Ko)", 7 => " (Korea) (En,Ko)",
                    _ => " (Japan)"
                };
                string console = tid[0] switch {
                    'F' => "NES", 'J' => "SNES", 'N' => "N64",
                    'L' => "SMS", 'M' => "SMD",
                    'E' => "NG",
                    'P' or 'Q' => "TGX",
                    'C' => "C64",
                    'X' => "MSX",
                    _ => ""
                };

                string name = MarioCube[index] + reg;
                name += GetUpperID(index).StartsWith("XAF") ? " (v256)" : ""; // Metal Gear
                name += !string.IsNullOrWhiteSpace(console) ? " (" + console + ")" : "";

                // ****************
                // Load WAD from MarioCube.
                // I have done a less copyright-friendly workaround solution for now.
                // ------------------------------------------------
                // Sadly, the NUS downloader cannot decrypt VC/Wii Shop titles on its own without needing the ticket file.
                // Trying to generate a ticket locally using the leaked title key algorithm (https://gbatemp.net/threads/3ds-wii-u-titlekey-generation-algorithm-leaked.566318/) fails to decrypt contents to a readable format anyway.
                // ------------------------------------------------
                // Direct link is not included, for obvious reasons!
                // ****************
                bool mcLite = ProjectForm.GetCurrentForm() != null && ProjectForm.GetCurrentForm().toggleMCLite.Checked;
                string repo = mcLite ? "https://archive.org/download/MarioCubeLite/WADs/" : "https://repo.mariocube.com/WADs/";
                string repoMain = repo + "_WiiWare,%20VC,%20DLC,%20Channels%20&%20IOS/";
                string folder = int.TryParse(name[0].ToString(), out int result) ? "0-9" : name[0].ToString().ToUpper();
                string URL = repoMain + folder + "/" + Uri.EscapeDataString(name + " (Virtual Console)") + ".wad";
                int ver = (Regions[index] == 0 || GetUpperID(index).StartsWith("HCJ")) ? 768 : Regions[index] == 3 ? 1537 : 1536;

                if (GetUpperID(index).StartsWith("WNA")) // Flash Placeholder
                    URL = repo + "Flash%20Injects/Base/" + Uri.EscapeDataString(name) + ".wad";

                //BBC, YT, Kirby, respectively
                else if (GetUpperID(index).StartsWith("HCJ") || GetUpperID(index).StartsWith("HCX") || GetUpperID(index).StartsWith("HCM"))
                    URL = repoMain + folder + "/" + Uri.EscapeDataString(name + (!GetUpperID(index).StartsWith("HCM") ? $" (v{ver})" : "") + " (Channel).wad");

               return URL;
            }
        }

        public List<ChannelEntry> Entries { get; private set; }

        #region Standalone Database
        /// <summary>
        /// Loads the Static Base WAD.
        /// </summary>
        public ChannelDatabase() {
            Entries = new List<ChannelEntry>();

            if (!File.Exists(Program.Config.paths.database)) {
                Program.Config.paths.database = null;
                Program.Config.Save();
            }
            GetStaticBase();
        }

        private void GetStaticBase() {
            Entries = new List<ChannelEntry>();

            var entry = new ChannelEntry() {
                ID = "00010001-53544c42"
            };
            entry.Regions.Add(8);
            entry.Titles.Add("Static Base");
            entry.EmuRevs.Add(0);
            entry.MarioCube.Add("");
            Entries.Add(entry);

            if (Entries.Count == 0)
                throw new Exception(dbErr);
        }
        #endregion

        #region Platform Databases
        /// <summary>
        /// Loads a database of WADs for a selected console/platform.
        /// </summary>
        public ChannelDatabase(Platform c, string externalFile = null) {
            bool externFile = File.Exists(externalFile) && !string.IsNullOrWhiteSpace(externalFile);
            bool invalidDb = !File.Exists(Program.Config.paths.database) && !string.IsNullOrWhiteSpace(Program.Config.paths.database);
            string file = externFile ? externalFile : File.Exists(Program.Config.paths.database) ? Program.Config.paths.database : null;
            Entries = new List<ChannelEntry>();

            if (invalidDb) {
                Program.Config.paths.database = null;
                Program.Config.Save();
            }
            try {
                GetEntries(c, File.ReadAllBytes(file));
            }
            catch {
                if (!string.IsNullOrWhiteSpace(externalFile))
                    throw;
                else
                    GetEntries(c, Properties.Resources.Database);
            }
        }

        private void GetEntries(Platform pf, byte[] file) {
            Entries = new List<ChannelEntry>();
            using (MemoryStream ms = new(file))
            using (StreamReader sr = new(ms, Encoding.Unicode))
            using (var doc = JsonDocument.Parse(sr.ReadToEnd(), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })) {

                var json = doc.Deserialize<JsonElement>(new JsonSerializerOptions() {
                    AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip}).GetProperty(pf.ToString().ToLower());

                if (json.ValueKind != JsonValueKind.Array)
                    throw new Exception(dbErr);

                sr.Dispose();
                ms.Dispose();

                foreach (var item in json.EnumerateArray()) {
                    ChannelEntry entry = new() {
                        ID = item.GetProperty("id").GetString() 
                    };
                    var reg = item.GetProperty("region");

                    for (int idx = 0; idx < reg.GetArrayLength(); idx++) {
                        entry.Regions.Add(reg[idx].GetInt32());
                        JsonElement titles = JsonUtils.GetOrDefault(item, "titles");
                        int titleCount = titles.ValueKind == JsonValueKind.Array ? titles.GetArrayLength() : 0;
                        entry.Titles.Add(JsonUtils.GetOrDefault(item, "titles", idx, e => e.GetString()!, ""));
                        entry.EmuRevs.Add(JsonUtils.GetOrDefault(item, "emu_ver", idx, e => e.GetInt32(), 0));
                        entry.RomIDs.Add(JsonUtils.GetOrDefault(item, "romIds", idx, e => e.GetString() ?? "", ""));

                        // ************************************************************************************************************-
                        
                        if (Program.Lang.GetRegion() is not Language.Region.Korea and not Language.Region.Japan) {
                            // Change Korean title to English if language is not CJK
                            if (entry.Regions.Count == 4 && (entry.Regions[3] is 6 or 7) && titleCount == 4)
                                entry.Titles[3] = titles[1].GetString();
                        }

                        if (Program.Lang.GetRegion() is not Language.Region.Japan) {
                            // Change Japanese title to English if language is not Japanese
                            /// TODO optional keeping localized titles
                            if (((entry.Regions.Count == 1 && entry.Regions[0] == 0) || (entry.Regions.Count > 1 && !entry.Regions.Contains(0))) && titleCount > 1)
                                entry.Titles[0] = titles[1].GetString();
                        }
                        else {
                            // Change Korean title of Japanese-derived Korean WADs to original
                            if (entry.Regions.Contains(6) && entry.Regions.Contains(0) && titleCount > 1)
                                entry.Titles[entry.Regions.IndexOf(6)] = titles[0].GetString();
                        }
                    }
                    for (int i = 0; i < entry.Regions.Count; i++) {
                        string wadTitle = entry.Titles[entry.Regions.Count > 1 ? 1 : 0];
                        var strip = wadTitle.StartsWith("The ") ? wadTitle.Substring(4) : null;
                        wadTitle = (strip != null ? (strip.Contains(": ") ? strip.Replace(": ", ", The: ") : strip + ", The") : wadTitle).Replace(": ", " - ").Replace('é', 'e');
                        entry.MarioCube.Add(JsonUtils.GetOrDefault(item, "wad_titles", i, e => e.GetString()!, wadTitle));
                    }
                    if (!(Program.Lang.GetRegion() is Language.Region.Japan && !entry.Regions.Contains(0)) || pf == Platform.Flash || pf == Platform.C64 || pf == Platform.MSX)
                        Entries.Add(entry);
                }
            }
            if (Entries.Count == 0)
                throw new Exception(dbErr);
        }
        #endregion
    }
}
