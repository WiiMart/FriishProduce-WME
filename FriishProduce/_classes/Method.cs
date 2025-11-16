using ICSharpCode.SharpZipLib.Zip;
using libWiiSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriishProduce
{
    [Serializable]
    class Method
    {
        private static readonly int steps = 3;
        (double step, double max) _progress = (0.0, steps);
        private void _updateProgress()
        {
            if (_progress.step < _progress.max)
                _progress.step += 1.0;
            else _progress.step = _progress.max;
        }

        public int Progress
        {
            get => (int)Math.Round(_progress.step / _progress.max * 100.0);
        }

        public bool IsMultifile { get; set; } = false;
        public string Patch { get; set; } = null;
        public string Manual { get; set; } = null;
        public (IDictionary<string, string> List, IDictionary<Buttons, string> Keymap) Settings = (null, null);

        public string TitleID { get; set; } = "ABCD";
        public string Genre { get; set; } = "Action, Platform";
        public int WadRegion { get; set; } = -1;
        public int WadVideoMode { get; set; } = 0;
        public int WiiUDisplay { get; set; } = 0;
        public string[] ChannelTitles { get; set; } = new string[] { "無題", "Untitled", "Ohne Titel", "Sans titre", "Sin título", "Senza titolo", "Onbekend", "제목 없음" };
        private string[] ChannelTitles_Limit
        {
            get
            {
                string[] value = ChannelTitles;

                int maxLength = 20;
                for (int i = 0; i < value.Length; i++)
                    if (value[i].Length > maxLength)
                    {
                        string delimiter = i == 0 ? "…" : "...";
                        value[i] = value[i].Substring(0, maxLength - delimiter.Length) + delimiter;
                    }

                return value;
            }
        }

        public Region BannerRegion { get; set; } = 0;
        public string BannerTitle { get; set; } = "";
        public int BannerYear { get; set; } = 1980;
        public int BannerPlayers { get; set; } = 1;
        public string BannerSound { get; set; } = null;
        public string[] SaveDataTitle { get; set; } = new string[2];
        public string Out { get; set; } = null;

        public ROM ROM { get; set; } = null;
        public WAD WAD { get; set; } = null;
        public string SrcBase { get; set; } = null;
        public ImageHelper Img { get; set; } = null;

        public int EmuVersion { get; set; } = 0;
        public Platform Platform { get; set; } = 0;

        public Method(Platform platform)
        {
            Platform = platform;
            Program.CleanTemp();
        }

        public void GetWAD(string path, string tid, bool hasInWad, bool ToggleMCLite = false) {
            try {
                object toLoad = null;

                if (!string.IsNullOrWhiteSpace(path)) {
                    if (File.Exists(path)) {
                        Logger.INFO($"Loading imported WAD with title ID: {tid}");
                        toLoad = path;
                        SrcBase = path;
                    }
                    else if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                        string saveDir = Program.Config.application.locsave_wad_tb ?? PathConstants.DefaultLocSaveWADs;
                        string fileUri = Path.GetFileName(new Uri(path).LocalPath);
                        string localPath = Path.Combine(saveDir, string.IsNullOrEmpty(fileUri) ? $"{tid}.wad" : fileUri);

                        if (!Directory.Exists(saveDir))
                            Directory.CreateDirectory(saveDir);

                        if (!hasInWad && !File.Exists(localPath))
                            Web.InternetTest(true);

                        Program.MainForm.Wait(true, true, true, 0, 1);

                        if (File.Exists(localPath)) {
                            Logger.INFO($"\nLoading downloaded WAD base from local files with title ID: {tid}");
                            toLoad = localPath;
                            SrcBase = localPath;
                        }
                        else {
                            _progress.max += 1.0;
                            byte[] wadData;
                            string pattern = @"https://repo\.mariocube\.com/WADs/_WiiWare,%20VC,%20DLC,%20Channels%20&%20IOS/";
                            string mclite = System.Text.RegularExpressions.Regex.Replace(path, pattern, Web.MCLITE + Web.MCL_WADS);
                            try {
                                string finalPath = ToggleMCLite ? mclite : path;
                                byte[] dlFile = Web.Get(finalPath, "\nDownloading WAD from URL:\n");
                                wadData = Path.GetExtension(new Uri(finalPath).AbsolutePath).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Zip.ExtractWADFrom(dlFile) : dlFile;
                            } catch {
                                Logger.WARN($"Failed to download from initial source, trying alternative...\n");
                                string finalPath = ToggleMCLite ? path : mclite;
                                byte[] dlFile = Web.Get(finalPath, "\nDownloading WAD from URL:\n");
                                wadData = Path.GetExtension(new Uri(finalPath).AbsolutePath).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Zip.ExtractWADFrom(dlFile) : dlFile;
                            }
                            SrcBase = !Program.Config.application.locsave_wad ? path : localPath;
                            toLoad = Program.Config.application.locsave_wad ? localPath : wadData;

                            if (Program.Config.application.locsave_wad) {
                                File.WriteAllBytes(localPath, wadData);
                                Logger.INFO($"Saved WAD locally to:\n\"{localPath}\"\n");
                            }

                            _updateProgress();
                        }
                    }
                }
                else {
                    Logger.INFO("Loading blank WAD.");
                    toLoad = Properties.Resources.StaticBase;
                }

                // load the path or bytes given from 'toLoad'
                //      then perform some validity checks with proper debugging<3
                WAD baseWad = ((toLoad is string loadPath) ? WAD.Load(loadPath) : (toLoad is byte[] loadData) ? WAD.Load(loadData) : null)
                    ?? throw new Exception($"Failed to load WAD for TID:[{tid}] from {path ?? "embedded resource"}");

                if (!string.Equals(baseWad.UpperTitleID, tid, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"TID:[{baseWad.UpperTitleID}] does not match expected TID:[{tid}]");

                if (!baseWad.HasBanner)
                    throw new Exception($"WAD for TID:[{tid}] does not have a valid banner file.");

                if (baseWad.NumOfContents <= 1)
                    throw new Exception($"WAD for TID:[{tid}] seems to be missing contents or is corrupted.");

                this.WAD = baseWad;
                Logger.INFO("WAD base loaded successfully.");
            }
            catch (Exception ex) {
                try {
                    WAD?.Dispose();
                } catch {}

                if (ex.InnerException != null)
                    Logger.ERROR(ex.InnerException.Message);
                throw;
            }
        }
            
        public void Inject(bool useOrigManual = false)
        {
            try
            {
                if (Platform == Platform.Flash)
                {
                    Injectors.Flash Flash = new()
                    {
                        SWF = ROM as SWF,
                        Settings = Settings.List,
                        Keymap = Settings.Keymap,
                        Multifile = IsMultifile,
                    };
                    Flash.Manual = Manual;
                    Logger.INFO("Injecting Adobe Flash, Small Web File.");
                    WAD = Flash.Inject(WAD, SaveDataTitle, Img);
                }

                else
                {
                    // Create Wii VC injector to use
                    // *******
                    InjectorWiiVC VC = null;
                    Logger.INFO($"Injecting VC data for {Platform}.");
                    switch (Platform)
                    {
                        default:
                            throw new NotImplementedException();

                        // NES
                        // *******
                        case Platform.NES:
                            VC = new Injectors.NES();
                            break;

                        // SNES
                        // *******
                        case Platform.SNES:
                            VC = new Injectors.SNES();
                            break;

                        // N64
                        // *******
                        case Platform.N64:
                            VC = new Injectors.N64()
                            {
                                CompressionType = EmuVersion == 3 ? (Settings.List["romc"] == "0" ? 1 : 2) : 0,
                                Allocate = Settings.List["rom_autosize"] == "True" && (EmuVersion <= 1),
                            };
                            break;

                        // SEGA
                        // *******
                        case Platform.SMS:
                        case Platform.SMD:
                            VC = new Injectors.SEGA()
                            {
                                IsSMS = Platform == Platform.SMS
                            };
                            break;

                        // PCE(CD)
                        // *******
                        case Platform.PCE:
                        case Platform.PCECD:
                            VC = new Injectors.PCE()
                            {
                                IsDisc = Platform == Platform.PCECD
                            };
                            break;

                        // NEOGEO
                        // *******
                        case Platform.NEO:
                            VC = new Injectors.NEO();
                            break;

                        // MSX
                        // *******
                        case Platform.C64:
                            VC = new Injectors.C64();
                            break;

                        // MSX
                        // *******
                        case Platform.MSX:
                            VC = new Injectors.MSX();
                            break;
                    }

                    // Get settings from relevant form
                    // *******
                    VC.Settings = Settings.List;
                    Logger.INFO("Applied injection method settings.");
                    VC.Keymap = Settings.Keymap;
                    Logger.INFO("Keymap.ini settings applied.");

                    // Set path to manual (if it exists) and load WAD
                    //// *******
                    VC.Manual = Manual;
                    Logger.INFO("Operations Manual settings applied.");

                    // Actually inject everything
                    // *******
                    WAD = VC.Inject(WAD, ROM, SaveDataTitle, Img);
                    Logger.INFO("Flashed ROM data.", "Save data titles written.", "Recompressed channel, banner, and save images.");
                }
                _updateProgress();
            }

            catch (Exception ex) {
                Logger.ERROR($"{ex.Message}");
                throw;
            }
        }

        public void CreateForwarder(string emulator, Forwarder.Storages storage)
        {
            try
            {
                Forwarder f = new()
                {
                    ROM = ROM.FilePath,
                    Multifile = IsMultifile,
                    ID = TitleID,
                    Emulator = emulator,
                    Storage = storage,
                    Name = ChannelTitles[1]
                };

                // Get settings from relevant form
                // *******
                f.Settings = Settings.List;

                // Actually inject everything
                // *******
                f.CreateZIP(Path.Combine(Path.GetDirectoryName(Out), Path.GetFileNameWithoutExtension(Out) + $" ({f.Storage}).zip"));
                WAD = f.CreateWAD(WAD);

                Logger.INFO($"Created {emulator} forwarder.");
                _updateProgress();
            }

            catch (Exception ex)
            {
                Logger.ERROR($"Failed to create forwarder. {ex.Message}");
                throw;
            }
        }

        public void EditMetadata()
        {
            if (WadRegion >= 0) WAD.Region = (Region)WadRegion;
            Utils.ChangeVideoMode(WAD, WadVideoMode, /* WiiUDisplay */ 0);
            WAD.ChangeChannelTitles(ChannelTitles_Limit);
            WAD.ChangeTitleID(LowerTitleID.Channel, TitleID);
            WAD.FakeSign = true;
            Logger.INFO($"Changed WAD title ID to {TitleID}.");
            Logger.INFO("Fakesigned WAD.");
        }

        public void EditBanner()
        {
            try
            {
                // Sound
                // *******
                if (File.Exists(BannerSound))
                    SoundHelper.ReplaceSound(WAD, BannerSound);
                else
                    SoundHelper.ReplaceSound(WAD, Properties.Resources.Sound_WiiVC);

                // Banner text
                // *******
                BannerHelper.Modify
                (
                    WAD,
                    Platform,
                    BannerRegion,
                    BannerTitle,
                    BannerYear,
                    BannerPlayers
                );

                // Image
                // *******
                if (Img?.VCPic != null) Img.ReplaceBanner(WAD);
                _updateProgress();
            }

            catch (Exception ex)
            {
                Logger.ERROR($"Failed to add VC banner. {ex.Message}");
                throw;
            }
        }

        public void Save() {
            try {
                if (Directory.Exists(PathConstants.SDUSBRoot)) {
                    Directory.CreateDirectory(PathConstants.SDUSBRoot + "wad\\");
                    WAD.Save(PathConstants.SDUSBRoot + "wad\\" + Path.GetFileNameWithoutExtension(Out) + ".wad");

                    // Get ZIP directory path & compress to .ZIP archive
                    // *******
                    try { File.Delete(Out); } catch { }

                    FastZip z = new();
                    z.CreateZip(Out, PathConstants.SDUSBRoot, true, null);

                    // Clean
                    // *******
                    Directory.Delete(PathConstants.SDUSBRoot, true);
                } else 
                    WAD.Save(Out);

                Logger.Log($"SUCCESS! Exported WAD to:\n\"{Out}\"");
                _updateProgress();
                _progress.step = _progress.max;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex) {
                Logger.ERROR($"Failed to export. {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _progress = (0.0, steps);

            Patch = null;
            Manual = null;
            Settings = (null, null);

            WadRegion = 0;
            WadVideoMode = 0;
            TitleID = null;
            Genre = null;
            ChannelTitles = null;

            BannerRegion = 0;
            BannerTitle = null;
            BannerYear = 1980;
            BannerPlayers = 1;
            SaveDataTitle = null;
            Out = null;

            ROM.Dispose();
            WAD.Dispose();
            Img.Dispose();

            EmuVersion = 0;
            Platform = 0;
        }
    }
}
