using ICSharpCode.SharpZipLib.Zip;
using libWiiSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FriishProduce
{
    public partial class ProjectForm : Form
    {
        protected Platform TargetPlatform { get; set; }
        private readonly BannerOptions banner_form;
        private readonly Savedata savedata;
        private MessageBoxHTML htmlDialog;
        private readonly TheArtOfDev.HtmlRenderer.WinForms.HtmlToolTip tip = HTML.CreateToolTip();

        protected string Untitled;
        protected (string Letter, string[] Exclude) TIDPrefix;
        private bool GameScanned { get; set; } = false;

        private void DrawGraphics(object sender, PaintEventArgs e) {
            if (banner_form.region.SelectedIndex != region.SelectedIndex)
                e.Graphics.DrawImage(Properties.Resources.warn_ico, new Point(10, 10));
        }

        protected bool IsVirtualConsole
        {
            get
            {
                bool value = false;

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = injection_methods.SelectedItem?.ToString().ToLower() == Program.Lang.String("vc").ToLower(); }));
                else
                    value = injection_methods.SelectedItem?.ToString().ToLower() == Program.Lang.String("vc").ToLower();

                return value;
            }
        }
        public bool IsForwarder { get => !IsVirtualConsole && TargetPlatform != Platform.Flash; }

        protected bool showPatch = false;
        private bool _showSaveData;
        protected bool ShowSaveData
        {
            get => _showSaveData;
            set => edit_save_data.Enabled = _showSaveData = value;
        }
        private readonly bool _isShown;
        private bool _isMint;

        #region Public bools (for main form)
        public bool IsModified
        {
            get => _isModified;

            set
            {
                _isModified = value;
                if (value) _isMint = false;
                Program.MainForm.toolbarSave.Enabled = value;
                Program.MainForm.save_project.Enabled = value;
                Program.MainForm.toolbarSaveAs.Enabled = value;
                Program.MainForm.save_project_as.Enabled = value;
                Program.MainForm.toolbarExport.Enabled = IsExportable;
                Program.MainForm.export.Enabled = IsExportable;
            }
        }
        private bool _isModified;

        private bool _isVisible = false;
        public bool IsVisible
        {
            get => _isVisible;

            set
            {
                groupBox1.Visible =
                groupBox2.Visible =
                groupBox3.Visible =
                groupBox4.Visible =
                groupBox5.Visible =
                groupBox6.Visible =
                _isVisible = value;
            }
        }

        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;

            set
            {
                _isEmpty = value;

                if (_isShown)
                {
                    title_id_random.Visible = Enabled = !value;

                    checkImg1.Visible = !value && use_offline_wad.Checked;
                    include_patch.Enabled = !value && showPatch;
                    injection_method_help.Visible = !value && htmlDialog != null;
                }
            }
        }

        public bool IsExportable
        {
            get
            {
                bool yes = !string.IsNullOrEmpty(_tID) && _tID.Length == 4
                            && !string.IsNullOrWhiteSpace(channel_name.Text)
                            && !string.IsNullOrEmpty(_bannerTitle)
                            && (BannerImg != null)
                            && rom?.FilePath != null
                            && ((use_online_wad.Checked) || (!use_online_wad.Checked && File.Exists(InBaseWAD)));

                if (!File.Exists(InBaseWAD) && !string.IsNullOrWhiteSpace(InBaseWAD))
                {
                    InBaseWAD = null;
                    Invoke(new MethodInvoker(delegate { ValueChanged(null, new EventArgs()); }));
                }

                return ShowSaveData ? yes && !string.IsNullOrEmpty(savedata.Lines[0]) : yes;
            }
        }

        public string ProjectPath { get; set; }
        #endregion

        public new enum Region
        {
            America,
            Europe,
            Japan,
            Korea,
            Free,
            Orig
        };

        // -----------------------------------
        // Public variables
        // -----------------------------------
        protected ChannelDatabase channels { get; set; }
        protected (int baseNumber, int region) InputWadX { get; set; } //unused
        protected string InBaseWAD { get; set; }
        public Region InBaseRegion
        {
            get
            {
                ChannelDatabase.ChannelEntry channel = null;
                Region value = Region.America;

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { channel = channels?.Entries[Base.SelectedIndex]; }));
                else channel = channels?.Entries[Base.SelectedIndex];

                for (int index = 0; index < channel.Regions.Count; index++)
                    if (channel.GetUpperID(index)[3] == baseID.Text[3])
                        value = channel.Regions[index] == 0 ? Region.Japan
                              : channel.Regions[index] == 6 || channel.Regions[index] == 7 ? Region.Korea
                              : channel.Regions[index] >= 3 && channel.Regions[index] <= 5 ? Region.Europe
                              : Region.America;
                return value;
            }
        }

        private Project project;

        private libWiiSharp.Region outWadRegion
        {
            get
            {
                string index = "";
                int indexNum = 0;

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { index = region.SelectedItem?.ToString(); indexNum = region.SelectedIndex; }));
                else
                { index = region.SelectedItem?.ToString(); indexNum = region.SelectedIndex; }

                return index == Program.Lang.String("region_j") ? libWiiSharp.Region.Japan
                     : index == Program.Lang.String("region_u") ? libWiiSharp.Region.USA
                     : index == Program.Lang.String("region_e") ? libWiiSharp.Region.Europe
                     : index == Program.Lang.String("region_k") ? libWiiSharp.Region.Korea
                     : indexNum == 0 ? InBaseRegion switch { Region.Japan => libWiiSharp.Region.Japan, Region.Korea => libWiiSharp.Region.Korea, Region.Europe => libWiiSharp.Region.Europe, Region.America => libWiiSharp.Region.USA, _ => libWiiSharp.Region.Free }
                     : libWiiSharp.Region.Free;
            }
        }

        protected ROM rom { get; set; }
        private string _patch { get; set; }
        protected string patch
        {
            get => _patch;
            set
            {
                if (_patch != value)
                {
                    _patch = value;

                    if (File.Exists(value))
                    {
                        /* try
                        { 
                           rom.Patch(value);
                        }

                        catch (Exception ex)
                        {
                            if (Program.DebugMode)
                                throw ex;
                            else
                                MessageBox.Error(ex.Message);
                            return;
                        } */

                        include_patch.Checked = true;
                        ValueChanged(null, new EventArgs());
                    }

                    else
                    {
                        _patch = null;
                        // rom.Patch(null);
                        include_patch.Checked = false;
                        ValueChanged(null, new EventArgs());
                    }
                }
            }
        }
        protected string manual { get; set; }
        protected ImageHelper BannerImg { get; set; }

        internal Preview preview = new();

        protected ContentOptions contentOptionsForm { get; set; }
        protected IDictionary<string, string> contentOptions { get => contentOptionsForm?.Options; }
        protected (bool Enabled, IDictionary<Buttons, string> List) keymap
        {
            get
            {
                (bool Enabled, IDictionary<Buttons, string> List) value = (false, null);

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = contentOptionsForm != null ? (contentOptionsForm.UsesKeymap, contentOptionsForm.Keymap) : (false, null); }));
                else
                { value = contentOptionsForm != null ? (contentOptionsForm.UsesKeymap, contentOptionsForm.Keymap) : (false, null); }

                return value;
            }
        }

        #region Channel/banner parameters
        private string _tID
        {
            get
            {
                string value = "";

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = title_id.Text.ToUpper(); }));
                else
                    value = title_id.Text.ToUpper();

                return value;
            }
        }
        private string _genre
        {
            get
            {
                string value = "";

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = genre.Text; }));
                else
                    value = (project != null && project.Platform == Platform.Flash) ? "Flash" : genre.Text;

                return value;
            }
        }
        private string[] _channelTitles
        {
            get
            {
                string[] value = new string[8];

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = new string[8] { channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text }; }));
                else
                    value = new string[8] { channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text, channel_name.Text };

                // DEFAULT: "無題", "Untitled", "Ohne Titel", "Sans titre", "Sin título", "Senza titolo", "Onbekend", "제목 없음"

                return value;
            }
        }
        private string _bannerTitle
        {
            get
            {
                string value = "";

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = banner_form.title.Text; }));
                else
                    value = banner_form.title.Text;

                return value;
            }
        }
        private int _bannerYear
        {
            get
            {
                int value = 0;

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = (int)banner_form.released.Value; }));
                else
                    value = (int)banner_form.released.Value;

                return value;
            }
        }
        private int _bannerPlayers
        {
            get
            {
                int value = 0;

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = (int)banner_form.players.Value; }));
                else
                    value = (int)banner_form.players.Value;

                return value;
            }
        }
        private string _bannerSound
        {
            get
            {
                string value = "";

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = banner_form.sound; }));
                else
                    value = banner_form.sound;

                return value;
            }
        }
        private string[] _saveDataTitle
        {
            get
            {
                string[] value = new string[2];

                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = savedata.IsMultiline == false ? new string[] { savedata.Lines[0] } : savedata.Lines.Length == 0 ? new string[] { "" } : savedata.Lines; }));
                else
                    value = savedata.IsMultiline == false ? new string[] { savedata.Lines[0] } : savedata.Lines.Length == 0 ? new string[] { "" } : savedata.Lines;

                return value;
            }
        }

        private libWiiSharp.Region _bannerRegion
        {
            get
            {
                int value = 0;
                if (InvokeRequired)
                    Invoke(new MethodInvoker(delegate { value = banner_form.region.SelectedIndex - 1; }));
                else value = banner_form.region.SelectedIndex - 1;

                if (value == -1 || banner_form.region.Text == "Automatic")
                {
                    value = channels != null ? InBaseRegion switch { Region.Japan => 0, Region.Europe => 2, Region.Korea => 3, _ => 1 } : 1;

                    if (!IsVirtualConsole && Program.Lang.GetRegion() is Language.Region.Japan)
                        value = 0;

                    if (!IsVirtualConsole && Program.Lang.GetRegion() is Language.Region.Korea)
                        value = 3;
                }
                // Forced regions (TODO make MSX available per region at a later time)

                // Japan/Korea: Use USA banner for C64 & Flash
                if (value != 1 && value != 2 && (TargetPlatform == Platform.C64 /*|| targetPlatform == Platform.Flash*/))
                    return libWiiSharp.Region.USA;

                // International: Use Japan banner for MSX
                else if (value != 0 && TargetPlatform == Platform.MSX)
                    return libWiiSharp.Region.Japan;

                // Korea: Use Europe banner for SMD
                else if (value == 3 && TargetPlatform == Platform.SMD)
                    return libWiiSharp.Region.Europe;

                // Korea: Use USA banner for non-available platforms
                else if (value == 3 && (int)TargetPlatform >= 3)
                    return libWiiSharp.Region.USA;

                return value switch { 0 => libWiiSharp.Region.Japan, 2 => libWiiSharp.Region.Europe, 3 => libWiiSharp.Region.Korea, _ => libWiiSharp.Region.USA };
            }
        }
        #endregion
        // -----------------------------------

        private void SetRecentProjects(string project)
        {
            int max = 10;
            bool modified = false;

            // Add project to #1 slot
            // ********
            if (project != Program.Config.paths.recent_00)
            {
                for (int i = max - 1; i > 0; i--)
                {
                    var prop1 = Program.Config.paths.GetType().GetProperty($"recent_{i - 1:D2}");
                    var prop2 = Program.Config.paths.GetType().GetProperty($"recent_{i:D2}");
                    var path = prop1.GetValue(Program.Config.paths, null)?.ToString();
                    prop2.SetValue(Program.Config.paths, path);
                }

                Program.Config.paths.recent_00 = project;
                Program.Config.Save();

                modified = true;
            }

            if (Program.MainForm.CleanupRecent() || modified)
                Program.MainForm.RefreshRecent();
        }

        public void SaveProject(string path)
        {
            ProjectPath = path;
            bool isLegacy = path.EndsWith(".fppj", StringComparison.OrdinalIgnoreCase);
            string updatedExt = isLegacy ? Path.ChangeExtension(path, ".jfpp") : path;

            var p = new Project() {
                ProjectPath = path,

                Platform = TargetPlatform,

                ROM = rom?.FilePath,
                Patch = patch,
                Manual = (manual_type.SelectedIndex, manual),
                Img = (BannerImg?.SavePath ?? null, BannerImg?.Source ?? null),
                ImageOptions = (image_interpolation_mode.SelectedIndex, image_resize1.Checked),

                ContentOptions = contentOptions ?? null,
                Keymap = (keymap.Enabled, keymap.List ?? null),
                InjectionMethod = injection_methods.SelectedIndex,
                ForwarderStorageDevice = forwarder_root_device.SelectedIndex,
                IsMultifile = multifile_software.Checked,

                LinkSaveDataTitle = savedata.Fill.Checked,
                VideoMode = video_mode.SelectedIndex,
                WiiUDisplay = wiiu_display.SelectedIndex,

                TitleID = _tID,
                Genre = _genre,
                Sound = _bannerSound,
                ChannelTitles = _channelTitles,
                BannerTitle = _bannerTitle,
                BannerYear = _bannerYear,
                BannerRegion = _bannerRegion,
                BannerPlayers = _bannerPlayers,
                SaveDataTitle = savedata.Lines,

                WADRegion = region.SelectedIndex,
            };
            p.OfflineWAD = InBaseWAD;
            p.OnlineWAD = (Base.SelectedIndex, 0);

            for (int idx = 0; idx < baseRegionList.Items.Count; idx++)
                if (baseRegionList.Items[idx] is ToolStripMenuItem item && item.Checked)
                    p.OnlineWAD = (Base.SelectedIndex, idx);

            string jfpp = JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true, 
                Converters = { new DlBaseWadParser(), new ManualParser(), new BmpParser(), new KeyParser(), new ImgOptsParser(), new JsonStringEnumConverter() }
            });

            File.WriteAllText(updatedExt, jfpp);

            if (isLegacy && File.Exists(path))
                try { File.Delete(path); } catch (Exception ex) { Logger.ERROR($"Failed to delete legacy project file (.fppj, binary-serialized): {ex.Message}"); }

            IsModified = false;
            _isMint = true;

            SetRecentProjects(updatedExt);
        }

        public void RefreshForm()
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            bool isMint = _isMint || !Program.MainForm.save_project_as.Enabled;

            if (Theme.ChangeColors(this, false))
            {
                BaseRegion.BackColor = Theme.Colors.Form.Bottom;
            }

            #region ------------------------------------------ Localization: Controls ------------------------------------------
            Program.Lang.Control(this, "projectform");
            Font = Program.MainForm.Font;

            // File filters
            browsePatch.Filter = Program.Lang.String("filter.patch");
            // BrowseManualZIP.Filter = Program.Lang.String("filter.zip");

            // Change title text to untitled string
            Untitled = Program.Lang.Format(("untitled_project", "mainform"), Program.Lang.String(Enum.GetName(typeof(Platform), TargetPlatform).ToLower(), "platforms"));
            Text = string.IsNullOrWhiteSpace(channel_name.Text) ? Untitled : channel_name.Text;

            checkImg1.Location = new Point(import_wad.Location.X + import_wad.Width + 4, checkImg1.Location.Y);
            baseID.Location = new Point(current_wad.Location.X + current_wad.Width + 2, current_wad.Location.Y + 1);

            setFilesText();

            // Selected index properties
            Program.Lang.Control(image_interpolation_mode, Name);
            if (wiiu_display.Items.Count > 2) wiiu_display.Items.RemoveAt(2);
            image_interpolation_mode.SelectedIndex = Program.Config.application.image_interpolation;
            wiiu_display.SelectedIndex = Program.Config.application.default_wiiu_display;

            // Manual
            manual_type.SelectedIndex = 0;
            manual = null;

            // Regions lists
            region.Items.Clear();
            region.Items.Add(Program.Lang.String("original"));
            region.Items.Add(Program.Lang.String("region_rf"));
            region.SelectedIndex = 0;

            // Video modes
            video_mode.Items[0] = Program.Lang.String("original");
            video_mode.SelectedIndex = 0;

            switch (Program.Lang.Current.ToLower())
            {
                default:
                    region.Items.Add(Program.Lang.String("region_u"));
                    region.Items.Add(Program.Lang.String("region_e"));
                    region.Items.Add(Program.Lang.String("region_j"));
                    region.Items.Add(Program.Lang.String("region_k"));
                    break;

                case "ja":
                    region.Items.Add(Program.Lang.String("region_j"));
                    region.Items.Add(Program.Lang.String("region_u"));
                    region.Items.Add(Program.Lang.String("region_e"));
                    region.Items.Add(Program.Lang.String("region_k"));
                    break;

                case "ko":
                    region.Items.Add(Program.Lang.String("region_k"));
                    region.Items.Add(Program.Lang.String("region_u"));
                    region.Items.Add(Program.Lang.String("region_e"));
                    region.Items.Add(Program.Lang.String("region_j"));
                    break;
            }
            #endregion

            #region ------------------------------------------ Localization: Tooltips ------------------------------------------
            Program.Lang.ToolTip(tip, channel_name, null, channel_name_l.Text);
            Program.Lang.ToolTip(tip, channel_name_l, null, channel_name_l.Text);
            Program.Lang.ToolTip(tip, video_mode, null, video_mode_l.Text, Program.Lang.Format(("t_unsure_s", "html"), video_mode.Items[0].ToString()));
            Program.Lang.ToolTip(tip, injection_method_options);
            Program.Lang.ToolTip(tip, multifile_software);
            Program.Lang.ToolTip(tip, warn_ban_reg);
            Program.Lang.ToolTip(tip, warn_ch_reg);
            Program.Lang.ToolTip(tip, warn_savetitle);
            #endregion

            if (Base.SelectedIndex >= 0)
                for (int i = 0; i < channels.Entries[Base.SelectedIndex].Regions.Count; i++)
                {
                    baseRegionList.Items[i].Text = channels.Entries[Base.SelectedIndex].Regions[i] switch
                    {
                        1 or 2 => Program.Lang.String("region_u"),
                        3 or 4 or 5 => Program.Lang.String("region_e"),
                        6 or 7 => Program.Lang.String("region_k"),
                        _ => Program.Lang.String("region_j"),
                    };
                }


            for (int i = 0; i < Base.Items.Count; i++)
            {
                var title = channels.Entries[i].Regions.Contains(0) && Program.Lang.Current.ToLower().StartsWith("ja") ? channels.Entries[i].Titles[0]
                          : channels.Entries[i].Regions.Contains(0) && Program.Lang.Current.ToLower().StartsWith("ko") ? channels.Entries[i].Titles[channels.Entries[i].Titles.Count - 1]
                          : channels.Entries[i].Regions.Contains(0) && channels.Entries[i].Regions.Count > 1 ? channels.Entries[i].Titles[1]
                          : channels.Entries[i].Titles[0];

                Base.Items[i] = title;
            }

            // Injection methods list
            injection_methods.Items.Clear();

            switch (TargetPlatform)
            {
                case Platform.NES:
                    injection_methods.Items.Add(Program.Lang.String("vc"));
                    injection_methods.Items.Add(Forwarder.List[0].Name);
                    injection_methods.Items.Add(Forwarder.List[1].Name);
                    injection_methods.Items.Add(Forwarder.List[2].Name);
                    break;

                case Platform.SNES:
                    injection_methods.Items.Add(Program.Lang.String("vc"));
                    injection_methods.Items.Add(Forwarder.List[3].Name);
                    injection_methods.Items.Add(Forwarder.List[4].Name);
                    injection_methods.Items.Add(Forwarder.List[5].Name);
                    break;

                case Platform.N64:
                    injection_methods.Items.Add(Program.Lang.String("vc"));
                    injection_methods.Items.Add(Forwarder.List[8].Name);
                    injection_methods.Items.Add(Forwarder.List[9].Name);
                    injection_methods.Items.Add(Forwarder.List[10].Name);
                    injection_methods.Items.Add(Forwarder.List[11].Name);
                    break;

                case Platform.SMS:
                case Platform.SMD:
                    injection_methods.Items.Add(Program.Lang.String("vc"));
                    injection_methods.Items.Add(Forwarder.List[7].Name);
                    break;

                case Platform.PCE:
                case Platform.PCECD:
                case Platform.NEO:
                case Platform.MSX:
                case Platform.C64:
                    injection_methods.Items.Add(Program.Lang.String("vc"));
                    break;

                case Platform.Flash:
                    injection_methods.Items.Add(Program.Lang.String("by_default"));
                    break;

                case Platform.GBA:
                    injection_methods.Items.Add(Forwarder.List[6].Name);
                    break;

                case Platform.PSX:
                    injection_methods.Items.Add(Forwarder.List[12].Name);
                    break;

                case Platform.RPGM:
                    injection_methods.Items.Add(Forwarder.List[13].Name);
                    break;

                default:
                    break;
            }

            injection_methods.SelectedIndex = TargetPlatform switch
            {
                Platform.NES => Program.Config.application.default_injection_method_nes,
                Platform.SNES => Program.Config.application.default_injection_method_snes,
                Platform.N64 => Program.Config.application.default_injection_method_n64,
                Platform.SMS or Platform.SMD => Program.Config.application.default_injection_method_sega,
                _ => 0
            };
            injection_methods.Enabled = injection_methods.Items.Count > 1;
            banner_form.released.Maximum = DateTime.Now.Year;

            image_resize1.Checked = Program.Config.application.image_fit_aspect_ratio;
            image_resize0.Checked = !image_resize1.Checked;
            resetImages();
            if (isMint && IsModified) IsModified = false;
        }

        private void LoadChannelDatabase()
        {
            try { channels = new ChannelDatabase(TargetPlatform); }
            catch (Exception ex)
            {
                if ((int)TargetPlatform < 10 || TargetPlatform == Platform.Flash)
                {
                    System.Windows.Forms.MessageBox.Show($"A fatal error occurred retrieving the {TargetPlatform} WADs database.\n\nException: {ex.GetType().FullName}\nMessage: {ex.Message}\n\nThe application will now shut down.", "Halt", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    Environment.FailFast("Database initialization failed.");
                }
                else { channels = new ChannelDatabase(); }
            }
        }

        public ProjectForm(Platform platform, string ROMpath = null, Project project = null)
        {
            TargetPlatform = platform;
            IsEmpty = true;
            banner_form = new BannerOptions(platform);
            savedata = new Savedata(platform);
            LoadChannelDatabase();

            InitializeComponent();
            Utils.AddCtrlListeners(this);
            Setup();
            _isShown = true;

            if (project != null && ROMpath == null)
                this.project = project;

            if (ROMpath != null && project == null)
                rom.FilePath = ROMpath;

            this.Load += (s, e) => AdjustLabels();
            this.rom_label.SizeChanged += (s, e) => AdjustLabels();
        }

        private void AdjustLabels() {
            // adjusts the filename position according to the rom label text width for localizations
            this.rom_label_filename.Location = new Point(this.rom_label.Right + 2, this.rom_label.Top);
            /// TODO add more auto-adjustments, chname, titleid etc
        }

        private string GetRegionSuffix(Region inWadRegion)
        {
            return inWadRegion switch
            {
                Region.Japan    => "J",
                Region.Europe   => "P",
                Region.Korea    => "K",
                _ => "E"
            };
        }

        private void Setup()
        {
            AddBases();
            // Declare ROM and WAD metadata modifier
            // ********
            TIDPrefix = (null, new[] { "F", "J", "N", "L", "M", "P", "Q", "E", "C", "X", "W" });
            switch (TargetPlatform)
            {
                case Platform.NES:
                    TIDPrefix = ("F", null);
                    rom = new ROM_NES();
                    showPatch = true;
                    break;

                case Platform.SNES:
                    TIDPrefix = ("J", null);
                    rom = new ROM_SNES();
                    showPatch = true;
                    break;

                case Platform.N64:
                    TIDPrefix = ("N", null);
                    rom = new ROM_N64();
                    showPatch = true;
                    break;

                case Platform.SMS:
                    TIDPrefix = ("L", null);
                    rom = new ROM_SEGA() { IsSMS = true };
                    showPatch = true;
                    break;

                case Platform.SMD:
                    TIDPrefix = ("M", null);
                    rom = new ROM_SEGA() { IsSMS = false };
                    showPatch = true;
                    break;

                case Platform.PCE:
                    TIDPrefix = ("P", null);
                    rom = new ROM_PCE();
                    showPatch = true;
                    break;

                case Platform.PCECD:
                    TIDPrefix = ("Q", null);
                    rom = new Disc();
                    break;

                case Platform.NEO:
                    TIDPrefix = ("E", null);
                    rom = new ROM_NEO();
                    break;

                case Platform.C64:
                    TIDPrefix = ("C", null);
                    rom = new ROM_C64();
                    break;

                case Platform.MSX:
                    TIDPrefix = ("X", null);
                    rom = new ROM_MSX();
                    showPatch = true;
                    break;

                case Platform.Flash:
                    TIDPrefix = ("W", null);
                    rom = new SWF();
                    break;

                case Platform.RPGM:
                    // TIDPrefix = ("X", null);
                    rom = new RPGM();
                    banner_form.players.Enabled = false;
                    break;

                default:
                    rom = new Disc();
                    break;
            }

            switch (TargetPlatform)
            {
                // ROM formats
                default:
                    try
                    {
                        var extensions = Platforms.Filters.Where(x => x.Key == (TargetPlatform == Platform.S32X ? Platform.SMD : TargetPlatform)).ToArray()[0];
                        for (int i = 0; i < extensions.Value.Length; i++)
                            if (!extensions.Value[i].StartsWith("*")) extensions.Value[i] = "*" + extensions.Value[i];

                        browseROM.Filter = Program.Lang.Format(("filter.rom", null), Program.Lang.Console(TargetPlatform), string.Join(", ", extensions.Value), string.Join(";", extensions.Value));
                    }
                    catch
                    {
                        browseROM.Filter = Program.Lang.String("filter").TrimStart('|');
                    }
                    break;

                // CD images
                case Platform.PCECD:
                case Platform.PSX:
                case Platform.SMCD:
                case Platform.GCN:
                    browseROM.Filter = Program.Lang.String("filter.disc") + "|" + Program.Lang.String("filter.zip") + Program.Lang.String("filter");
                    break;

                case Platform.NEO:
                    browseROM.Filter = Program.Lang.String("filter.zip");
                    break;

                case Platform.Flash:
                    browseROM.Filter = Program.Lang.String("filter.swf");
                    break;

                case Platform.RPGM:
                    browseROM.Filter = Program.Lang.String("filter.rpgm") + "|" + Program.Lang.String("filter.zip") + Program.Lang.String("filter");
                    break;
            }
        }

        private void Form_Shown(object sender, EventArgs e)
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            // Set icon
            // ********
            if (Program.GUI)
                Icon = Icon.FromHandle(Platforms.Icons[TargetPlatform].GetHicon());

            // Cosmetic
            // ********
            RefreshForm();

            bool removeManual = true;
            foreach (var manualConsole in new List<Platform>() {
                // Confirmed to have an algorithm exist for NES, SNES, N64, SEGA, PCE, NEO
                Platform.NES,
                Platform.SNES,
                Platform.N64,
                Platform.SMS,
                Platform.SMD,
                Platform.Flash,
                // Platform.PCE,
                // Platform.NEO
            }) removeManual = !(TargetPlatform == manualConsole);
            if (removeManual) manual_type.Items.RemoveAt(2);

            // *****************************************************
            // LOADING PROJECT
            // *****************************************************
            bool loadProject = project != null;
            if (loadProject) {
                try {
                    if (string.IsNullOrEmpty(project.ProjectPath) || !File.Exists(project.ProjectPath))
                        throw new FileNotFoundException("Project file does not exist in given path.", project.ProjectPath);

                    string ext = Path.GetExtension(project.ProjectPath);
                    if (ext.Equals(".jfpp", StringComparison.OrdinalIgnoreCase)) {
                        string jfpp = File.ReadAllText(project.ProjectPath);
                        project = JsonSerializer.Deserialize<Project>(jfpp, new JsonSerializerOptions {
                            Converters = { new DlBaseWadParser(), new ManualParser(), new BmpParser(), new KeyParser(), new ImgOptsParser(), new JsonStringEnumConverter() }
                        });
                    }
                    else if (ext.Equals(".fppj", StringComparison.OrdinalIgnoreCase))
                    {
                        try { banner_form.region.SelectedIndex = Meta.RegToInt(project.BannerRegion) + 1; }
                        catch { banner_form.region.SelectedIndex = 0; }
                        finally {
                            linkSaveDataTitle();
                            resetImages(true);
                        }
                    }

                    if (project == null)
                        throw new Exception("Failed to load project file.");

                    Logger.INFO($"Opened project at:\n\"{project.ProjectPath}\"");
                    SetRecentProjects(project.ProjectPath);
                    ProjectPath = project.ProjectPath;

                    video_mode.SelectedIndex = project.VideoMode;

                    BannerImg = new ImageHelper(project.Platform, null);
                    BannerImg.LoadImage(!string.IsNullOrEmpty(project.Img.File) ? project.Img.File : project.Img.Bmp);
                    LoadROM(project.ROM, false);

                    if (File.Exists(project.OfflineWAD)) {
                        use_online_wad.Enabled = Program.Config.application.use_online_wad_enabled;
                        use_offline_wad.Checked = true;
                        LoadWAD(project.OfflineWAD);
                    }
                    else {
                        use_online_wad.Enabled = use_online_wad.Checked = true;
                        use_offline_wad.Checked = false;
                        try { Base.SelectedIndex = project.OnlineWAD.BaseNumber; UpdateBaseForm(project.OnlineWAD.Region); }
                        catch { Base.SelectedIndex = 0; UpdateBaseForm(); }
                    }

                    patch = File.Exists(project.Patch) ? project.Patch : null;

                    try { channel_name.Text = project.ChannelTitles[1]; } catch { }
                    try { banner_form.title.Text = project.BannerTitle; } catch { }
                    try { banner_form.released.Value = project.BannerYear; } catch { }
                    try { banner_form.players.Value = project.BannerPlayers; } catch { }

                    var pjReg = File.Exists(project.OfflineWAD) ? Meta.RegToInt(project.BannerRegion) + 1 : Base.SelectedIndex;
                    try { banner_form.region.SelectedIndex = pjReg; }
                    catch { banner_form.region.SelectedIndex = 0; }
                    finally {
                        linkSaveDataTitle();
                        resetImages(true);
                    }

                    try { savedata.title.Text = project.SaveDataTitle[0]; } catch { }
                    try { savedata.subtitle.Text = project.SaveDataTitle.Length > 1 && savedata.subtitle.Enabled ? project.SaveDataTitle[1] : null; } catch { }
                    try { title_id.Text = project.TitleID; } catch { }
                    try { genre.Text = project.Platform == Platform.Flash ? "Flash" : project.Genre; } catch { }

                    try { injection_methods.SelectedIndex = project.InjectionMethod; } catch { }
                    try { multifile_software.Checked = project.IsMultifile; } catch { }
                    try { image_interpolation_mode.SelectedIndex = project.ImageOptions.Item1; } catch { }
                    try { image_resize0.Checked = !project.ImageOptions.Item2; } catch { }
                    try { image_resize1.Checked = project.ImageOptions.Item2; } catch { }
                    try { region.SelectedIndex = project.WADRegion; } catch { }
                    try { 
                        video_mode.SelectedIndex = project.VideoMode;
                        video_mode.SelectedIndexChanged += (s, e) => {
                            ValueChanged(s, e);
                            string pal6050 = video_mode.SelectedIndex == 6 ? "warn_pal6050" : warn_vidmode.Name;
                            string vidtip_name = video_mode.SelectedIndex == 7 ? "warn_ntscpal60" : pal6050;
                            Program.Lang.ToolTip(tip, warn_vidmode, vidtip_name);
                        };
                    } catch {}
                    try { wiiu_display.SelectedIndex = project.WiiUDisplay; } catch { }

                    if (contentOptionsForm != null) {
                        contentOptionsForm.Options = project.ContentOptions;
                        contentOptionsForm.UsesKeymap = project.Keymap.Enabled;
                        contentOptionsForm.Keymap = project.Keymap.List;
                    }

                    LoadImage();
                    LoadManual(project.Manual.Type, project.Manual.File);
                    banner_form.LoadSound(project.Sound);
                    setFilesText();
                }
                catch (Exception ex) {
                    MessageBox.Show($"Error loading project: {ex.Message}", "Error", MessageBox.Buttons.Ok, MessageBox.Icons.Warning);
                    loadProject = false;
                }
            }
            else {
                Logger.INFO($"Created new {TargetPlatform} project.");
                use_online_wad.Enabled = Program.Config.application.use_online_wad_enabled;

                if (use_online_wad.Enabled) {
                    if (!use_offline_wad.Checked && !use_online_wad.Checked)
                        use_online_wad.Checked = true;

                    if (use_online_wad.Checked) {
                        Base.SelectedIndex = 0; // base wad
                        UpdateBaseForm(1); // region (usa default)
                    }
                }
                else {
                    use_online_wad.Checked = false;
                    use_offline_wad.Checked = true;
                }
            }

            savedata.Fill.Checked = loadProject ? project.LinkSaveDataTitle : Program.Config.application.auto_fill_save_data;
            if (savedata.Fill.Checked) linkSaveDataTitle();
            forwarder_root_device.SelectedIndex = loadProject ? project.ForwarderStorageDevice : Program.Config.forwarder.root_storage_device;

            Program.Lang.ToolTip(tip, fetch_patch, null, fetch_patch.Text);
            Program.Lang.ToolTip(tip, fetch_patch_l, null, fetch_patch.Text);
            Program.Lang.ToolTip(tip, fetch_patch_btn, null, fetch_patch.Text);

            IsVisible = true;

            IsEmpty = !loadProject;
            IsModified = false;
            _isMint = true;

            // Error messages for not found files
            // ********
            if (loadProject) {
                foreach (var item in new string[] { project.ROM, project.Patch, project.OfflineWAD, project.Sound })
                    if (!File.Exists(item) && !string.IsNullOrWhiteSpace(item))
                        MessageBox.Show(string.Format(Program.Lang.Msg(11, 1), Path.GetFileName(item)));
            }
            project = null;
            //if (File.Exists(rom?.FilePath) && IsEmpty)
                //LoadROM(rom.FilePath, Program.Config.application.auto_prefill);
        }

        // -----------------------------------

        public void BrowseROMDialog(string text)
        {
            browseROM.Title = text.Replace("&", "");

            if (browseROM.ShowDialog() == DialogResult.OK)
            {
                LoadROM(browseROM.FileName, Program.Config.application.auto_prefill);
            }
        }

        public void BrowseImageDialog()
        {
            browseImage.Title = import_image.Text.Replace("&", "");
            browseImage.Filter = Program.Lang.String("filter.img");

            if (browseImage.ShowDialog() == DialogResult.OK) LoadImage(browseImage.FileName);
        }

        private void ImportBannerClick(object sender, EventArgs e) => BrowseImageDialog();
        private void DlBannerClick(object sender, EventArgs e) => GameScan(true);

        private void ValueChanged(object sender, EventArgs e)
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            if (IsVirtualConsole && (InBaseWAD == null && !use_online_wad.Checked))
                injection_method_options.Enabled = false;
            else
                injection_method_options.Enabled = contentOptionsForm != null;

            if (channels.Entries?[0].ID == "00010001-53544c42")
            {
                use_online_wad.Checked = true;
                import_wad.Enabled = use_offline_wad.Enabled = use_online_wad.Enabled = false;
                using_default_wad.Visible = true;
                using_default_wad.BringToFront();
            }

            if (!IsEmpty)
                IsModified = true;

            CheckWarnings();
            setFilesText();
        }

        private void CheckWarnings() {
            if (rom == null || string.IsNullOrEmpty(rom.FilePath)) return;
            string[] matchParams = {
                banner_form.region.Text,
                Meta.IntToChReg(region.SelectedIndex),
                InBaseRegion.ToString(),
                video_mode.SelectedIndex.ToString(),
                savedata.Lines[0],
                BannerImg?.VCPic != null ? "img" : ""
            };
            var conflicts = Meta.GetConflictSrcs(matchParams);
            bool regConflict = conflicts.Contains(Meta.BNR_REG_WARN);
            bool imgConflict = conflicts.Contains(Meta.BNR_IMG_WARN);
            bool flashSave = contentOptionsForm is Options_Flash flashForm && flashForm.save_data_enable.Checked;

            warn_ban_reg.Visible = regConflict || imgConflict;
            warn_ban_reg.BringToFront();
            warn_ch_reg.Visible = conflicts.Contains(Meta.CHL_REG_WARN);
            warn_ch_reg.BringToFront();
            warn_vidmode.Visible = conflicts.Contains(Meta.VDM_WARN);
            warn_vidmode.BringToFront();
            warn_savetitle.Visible = conflicts.Contains(Meta.SVT_WARN) && (flashSave || rom?.FilePath != null);
            warn_savetitle.BringToFront();

            if (imgConflict)
                Program.Lang.PrependToolTip(tip, warn_ban_reg, "<b>[Missing Banner Image]</b><hr>", includeBase: regConflict);
            else if (regConflict)
                Program.Lang.ToolTip(tip, warn_ban_reg);
        }

        public bool[] ToolbarButtons
        {
            get => new bool[]
            {
                TargetPlatform != Platform.Flash
                && TargetPlatform != Platform.RPGM, // LibRetro / game data (1)

                TargetPlatform != Platform.Flash
                && TargetPlatform != Platform.RPGM
                && rom?.FilePath != null, // LibRetro / game data (2, less strict)

                /*
                targetPlatform != Platform.Flash
                && targetPlatform != Platform.RPGM
                && isVirtualConsole,
                */ // Browse manual
            };
        }

        public Bitmap FileTypeImage
        {
            get
            {
                return TargetPlatform switch
                {
                    Platform.NEO => Properties.Resources.page_white_zip,
                    Platform.Flash => Properties.Resources.page_white_flash,
                    _ => Properties.Resources.page_white_cd
                };
            }
        }

        public string FileTypeName
        {
            get
            {
                return TargetPlatform switch
                {
                    Platform.PSX
                    or Platform.PCECD
                    or Platform.SMCD
                    or Platform.GCN => Program.Lang.String(rom_label.Name + "2", Name),
                    Platform.RPGM => Program.Lang.String(rom_label.Name + "1", Name),
                    Platform.NEO => "ZIP",
                    Platform.Flash => "SWF",
                    _ => "ROM",
                };
            }
        }

        private void setFilesText()
        {
            // ROM/ISO
            // ********
            bool hasRom = !string.IsNullOrWhiteSpace(rom?.FilePath);
            bool hasWad = !string.IsNullOrWhiteSpace(InBaseWAD);

            groupBox1.Text = Program.Lang.Format(("main", Name), FileTypeName);
            rom_label.Text = Program.Lang.Format((rom_label.Name, Name), FileTypeName);
            rom_label_filename.Text = hasRom ? Utils.GetFileCN(rom?.FilePath) + Path.GetExtension(rom?.FilePath) : Program.Lang.String("none");
            if (rom_label_filename.Text.Length > 65) rom_label_filename.Text = rom_label_filename.Text.Substring(0, 62) + "...";

            // WAD
            // ********
            if (!hasWad && !use_online_wad.Checked)
            {
                baseID.Visible = false;
                baseName.Location = baseID.Location;
                baseName.Text = Program.Lang.String("none");
            }
            else
            {
                baseID.Visible = true;
                baseName.Location = new Point(baseID.Location.X + baseID.Width, baseID.Location.Y);
            }

            checkImg1.Image = hasWad ? Program.Lang.Current.ToLower().StartsWith("ja") || Program.Lang.Current.ToUpper().EndsWith("-JP") ? Properties.Resources.tick_circle : Properties.Resources.tick : Properties.Resources.cross;
        }

        private void randomTID()
        {
            string baseId;
            baseId = TIDPrefix.Letter != null ? TIDPrefix.Letter + GenerateTitleID().Substring(0, 2) : GenerateTitleID().Substring(0, 3);

            // Add region suffix
            string regionSuffix = GetRegionSuffix(InBaseRegion);
            title_id.Text = baseId + regionSuffix;

            // Change title ID prefix to avoid 4:3 stretching on Wii U, if a list is provided
            //      -> Uh? No idea what this has to do with with 4:3 aspect, maybe prefix determining console? -Subnetic
            // ********
            if (TIDPrefix.Exclude?.Length > 0)
            {
            Loop:
                int verified = TIDPrefix.Exclude?.Length ?? 0;
                while (verified > 0)
                {
                    for (int i = 0; i < TIDPrefix.Exclude?.Length; i++)
                    {
                        if (title_id.Text[0] == TIDPrefix.Exclude[i][0])
                        {
                            // regenerate platform prefix, keep rest
                            title_id.Text = GenerateTitleID().Substring(0, 1) + title_id.Text.Substring(1, 2) + regionSuffix;
                            goto Loop;
                        }

                        verified--;
                    }
                }
            }

            ValueChanged(null, new EventArgs());
        }

        private void getUniqueTID()
        {
            HashSet<string> existingTIDs = LoadExistingTIDs();
            string uTID;

            while (true) // repeat until we find a base ID valid for all suffixes
            {
                // base ID with optional prefix
                string baseId = TIDPrefix.Letter != null ? TIDPrefix.Letter + GenerateTitleID().Substring(0, 2) : GenerateTitleID().Substring(0, 3);
                if (TIDPrefix.Exclude?.Length > 0)
                {
                    for (int i = 0; i < TIDPrefix.Exclude.Length; i++)
                    {
                        baseId = baseId[0] == TIDPrefix.Exclude[i][0] ? GenerateTitleID().Substring(0, 1) + baseId.Substring(1, 2) : baseId;
                        i = baseId[0] == TIDPrefix.Exclude[i][0] ? -1 : i;
                    }
                }
                bool avail = true;
                foreach (var suffix in new[] { "E", "P", "J", "K" })
                {
                    string checkTID = baseId + suffix;
                    avail = !existingTIDs.Contains(checkTID);
                    if (!avail) break;
                }
                uTID = avail ? baseId + GetRegionSuffix(InBaseRegion) : null;
                if (avail) break; // else: repeat loop with a new baseId
            }

            title_id.Text = uTID;
            ValueChanged(null, new EventArgs());
        }

        // Load existing TIDs from JSON list compiled and deduplicated from WiiMart games.jsom
        private HashSet<string> LoadExistingTIDs()
        {
            string wmtids = "Resources/wmtids.json";
            return !File.Exists(wmtids) ? new HashSet<string>() : new HashSet<string>(JsonSerializer.Deserialize<string[]>(File.ReadAllText(wmtids)) ?? Array.Empty<string>());
        }

        public string GetName(bool export)
        {
            string FILENAME = File.Exists(patch) ? Utils.GetFileCN(patch) : Utils.GetFileCN(rom?.FilePath);

            string CHANNELNAME = channel_name.Text;
            if (string.IsNullOrWhiteSpace(CHANNELNAME)) CHANNELNAME = Untitled;

            string FULLNAME = System.Text.RegularExpressions.Regex.Replace(_bannerTitle, @"\((.*?)\)", "").Replace("\r\n", "\n").Replace("\n", " - ");
            if (string.IsNullOrWhiteSpace(FULLNAME)) FULLNAME = Untitled;

            string TITLEID = title_id.Text.ToUpper();

            string PLATFORM = TargetPlatform.ToString();

            string GENRE = PLATFORM == "Flash" ? "Flash" : genre.Text;
            if (string.IsNullOrWhiteSpace(GENRE)) GENRE = "Unknown";

            string REGION = region.SelectedItem.ToString() == Program.Lang.String("region_j") ? "JPN"
                          : region.SelectedItem.ToString() == Program.Lang.String("region_u") ? "USA"
                          : region.SelectedItem.ToString() == Program.Lang.String("region_e") ? "EUR"
                          : region.SelectedItem.ToString() == Program.Lang.String("region_k") ? "KOR"
                          : region.SelectedIndex == 1 ? "Region-Free"
                          : null;
            if (REGION == null)
            {
                for (int i = 0; i < baseRegionList.Items.Count; i++)
                {
                    if ((baseRegionList.Items[i] as ToolStripMenuItem).Checked)
                    {
                        REGION = channels.Entries[Base.SelectedIndex].Regions[i] switch
                        {
                            0 => "JPN",
                            1 or 2 => "USA",
                            3 or 4 or 5 => "EUR",
                            6 or 7 => "KOR",
                            8 => "Region-Free",
                            _ => "Original"
                        };
                    }
                }
            }

            string target = export ? Program.Config.application.default_export_filename : Program.Config.application.default_target_filename;
            bool lowerOut = export && Program.Config.application.lowerParams;
            bool transToggled = export && Program.Config.application.transParams;

            if (target == Untitled)
                target = "";

            var map = new (string Key, string Val, bool Lower, bool Trans)[] {
                ("FILENAME",    FILENAME,       true,       false),
                ("CHANNELNAME", CHANNELNAME,    true,       true),
                ("FULLNAME",    FULLNAME,       true,       true),
                ("TITLEID",     TITLEID,        true,       false),
                ("GENRE",       GENRE,          false,      false),
                ("PLATFORM",    PLATFORM,       true,       false),
                ("REGION",      REGION,         true,       false),
                //KEY           VAL             LOWER       TRANS
            };
            foreach (var (key, val, lower, trans) in map) {
                string replace = val;

                if (lowerOut && lower)
                    replace = replace.ToLower();

                if (transToggled && trans && GoogleTrans.ContainsCJK(replace)) {
                    try {
                        replace = Task.Run(() => GoogleTrans.Translate(replace)).GetAwaiter().GetResult();
                    } catch {}
                }
                target = target.Replace(key, replace);
            }
            string result = string.Join("_", target.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            return result;
        }

        private bool _canClose = false;
        public bool CanClose
        {
            get
            {
                if (!_canClose)
                    _canClose = CheckUnsaved();
                return _canClose;
            }
        }

        private void isClosing(object sender, FormClosingEventArgs e)
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            if (!CanClose)
                e.Cancel = !CanClose;

            if (!e.Cancel)
            {
                if (rom != null) rom.Dispose();
                rom = null;
                channels = null;

                if (BannerImg != null) BannerImg.Dispose();
                BannerImg = null;

                if (contentOptionsForm != null) contentOptionsForm.Dispose();
                contentOptionsForm = null;

                preview.Dispose();
                preview = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                try { Dispose(); } catch { }
            }
        }

        public bool CheckUnsaved()
        {
            if (IsModified)
            {
                var result = MessageBox.Show(string.Format(Program.Lang.Msg(1), Text), MessageBox.Buttons.YesNoCancel, MessageBox.Icons.Warning);

                switch (result)
                {
                    case MessageBox.Result.Yes:
                    case MessageBox.Result.Button1:
                        if (File.Exists(ProjectPath))
                        {
                            SaveProject(ProjectPath);
                            return true;
                        }
                        else return Program.MainForm.SaveAs_Trigger(this);

                    case MessageBox.Result.No:
                    case MessageBox.Result.Button2:
                        return true;

                    default:
                    case MessageBox.Result.Cancel:
                    case MessageBox.Result.Button3:
                        return false;
                }
            }

            return true;
        }

        private void Random_Click(object sender, EventArgs e) => getUniqueTID();

        private void linkSaveDataTitle()
        {
            savedata.SourcedLines = new string[] { channel_name.Text, banner_form.title.Lines.Length > 1 ? banner_form.title.Lines[1] : "" };

            if (savedata.Fill.Checked)
            {
                savedata.SyncTitles();
                ValueChanged(null, new EventArgs());
            }
        }

        private void TextBox_Changed(object sender, EventArgs e)
        {
            if (sender == channel_name)
            {
                Text = string.IsNullOrWhiteSpace(channel_name.Text) ? Untitled : channel_name.Text;
                linkSaveDataTitle();
            }

            var currentSender = sender as TextBox;
            if (currentSender.Multiline && currentSender.Lines.Length > 2) currentSender.Lines = new string[] { currentSender.Lines[0], currentSender.Lines[1] };

            ValueChanged(sender, e);
        }

        private void TextBox_Handle(object sender, KeyPressEventArgs e)
        {
            var currentSender = sender as TextBox;
            var currentIndex = currentSender.GetLineFromCharIndex(currentSender.SelectionStart);
            var lineMaxLength = currentSender.Multiline ? Math.Round((double)currentSender.MaxLength / 2) : currentSender.MaxLength;

            if (!string.IsNullOrEmpty(currentSender.Text)
                && currentSender.Lines[currentIndex].Length >= lineMaxLength
                && e.KeyChar != (char)Keys.Delete && e.KeyChar != (char)8 && e.KeyChar != (char)Keys.Enter)
                goto Handled;

            if (currentSender.Multiline && currentSender.Lines.Length == 2 && e.KeyChar == (char)Keys.Enter) goto Handled;

            return;

            Handled:
            SystemSounds.Beep.Play();
            e.Handled = true;
        }

        private void OpenWAD_CheckedChanged(object sender, EventArgs e)
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            use_offline_wad.Checked = !use_online_wad.Checked;
            BaseRegion.Enabled = use_online_wad.Checked;
            Base.Enabled = use_online_wad.Checked && Base.Items.Count > 1;
            checkImg1.Visible = import_wad.Enabled = use_offline_wad.Checked;

            if (use_online_wad.Checked)
            {
                InBaseWAD = null;
                AddBases();
            }
            else
            {
                if (Base.Items.Count > 0) Base.SelectedIndex = 0;
            }

            if (!BaseRegion.Enabled)
                BaseRegion.Image = null;

            ValueChanged(sender, e);
        }

        private void import_wad_Click(object sender, EventArgs e)
        {
            browseInputWad.Title = import_wad.Text.Replace("&", "");
            browseInputWad.Filter = Program.Lang.String("filter.wad");
            var result = browseInputWad.ShowDialog();

            if (result == DialogResult.OK)
                LoadWAD(browseInputWad.FileName);
        }

        private void InterpolationChanged(object sender, EventArgs e)
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            if (image_interpolation_mode.SelectedIndex != Program.Config.application.image_interpolation)
                ValueChanged(sender, e);
            LoadImage();
        }

        private void SwitchAspectRatio(object sender, EventArgs e)
        {
            // ----------------------------
            if (DesignMode) return;
            // ----------------------------

            if (sender == image_resize0 || sender == image_resize1)
            {
                LoadImage();
            }

            if (sender == forwarder_root_device || sender == wiiu_display)
            {
                ValueChanged(sender, e);
            }
        }

        #region Load Data Functions
        private string GenerateTitleID()
        {
            var r = new Random();
            string allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(allowed, 4).Select(s => s[r.Next(s.Length)]).ToArray());
        }

        public bool LoadWAD(string path)
        {
            WAD Reader = new();

            try
            {
                if (Directory.Exists(PathConstants.WAD)) Directory.Delete(PathConstants.WAD, true);
                Reader = WAD.Load(path);
            }
            catch
            {
                goto Failed;
            }

            for (int h = 0; h < channels.Entries.Count; h++)
                for (int i = 0; i < channels.Entries[h].Regions.Count; i++)
                {
                    if (channels.Entries[h].GetUpperID(i) == Reader.UpperTitleID.ToUpper())
                    {
                        InBaseWAD = path;
                        ValueChanged(null, new EventArgs());

                        // Fix Flash Placeholder (USA) bug
                        // ****************
                        if ((int)Reader.Region == 1 && Reader.UpperTitleID.ToUpper().StartsWith("WNA"))
                            i = 0;

                        Base.SelectedIndex = h;
                        UpdateBaseForm(i);
                        Reader.Dispose();
                        return true;
                    }
                }

            Failed:
            SystemSounds.Beep.Play();
            MessageBox.Show(string.Format(Program.Lang.Msg(5), Reader.UpperTitleID == "\0\0\0\0" || Reader.UpperTitleID.Length != 4 ? Program.Lang.String("none") : Reader.UpperTitleID));

            try { Reader.Dispose(); }
            catch { Reader = null; }

            if (InBaseWAD != null)
            {
                InBaseWAD = null;
                ValueChanged(null, new EventArgs());
            }

            return false;
        }

        protected void LoadManual(int index, string path = null, bool isFolder = true)
        {
            bool failed = false;

            #region Load manual as ZIP file (if exists)
            if (File.Exists(path) && !isFolder)
            {
                int applicable = 0;
                // bool hasFolder = false;

                using ZipFile ZIP = new(path);
                foreach (ZipEntry entry in ZIP)
                {
                    if (entry.IsFile)
                    {
                        // Check if is a valid emanual contents folder
                        // ****************
                        // if ((item.FileName is "emanual" or "html") && item.IsDirectory)
                        //    hasFolder = true;

                        // Check key files
                        // ****************
                        if ((entry.Name.StartsWith("startup") && Path.GetExtension(entry.Name) == ".html")
                          || (entry.Name is "standard.css" or "contents.css" or "vsscript.css"))
                            applicable++;
                    }
                }

                if (applicable >= 2 /* && hasFolder */)
                {
                    manual = path;
                    goto End;
                }

                failed = true;
            }
            #endregion

            #region Load manual as folder/directory (if exists)
            else if (Directory.Exists(path) && isFolder)
            {
                // Check if is a valid emanual contents folder
                // ****************
                string folder = path;
                if (Directory.Exists(Path.Combine(path, "emanual")))
                    folder = Path.Combine(path, "emanual");
                else if (Directory.Exists(Path.Combine(path, "html")))
                    folder = Path.Combine(path, "html");

                int validFiles = 0;
                if (folder != null)
                    foreach (var item in Directory.EnumerateFiles(folder)) {
                        bool valid = Path.GetFileName(item) is "standard.css" or "contents.css" or "vsscript.css";
                        if ((Utils.GetFileCN(item).StartsWith("startup") && Path.GetExtension(item) == ".html") || valid)
                            validFiles++;
                    }

                if (validFiles >= 2)
                {
                    manual = path;
                    goto End;
                }

                failed = true;
            }
            #endregion

            else
            {
                manual = null;
                goto End;
            }

            End:
            if (failed)
            {
                MessageBox.Show(Program.Lang.Msg(7), MessageBox.Buttons.Ok, MessageBox.Icons.Warning);
                manual = null;
            }

            manual_type.SelectedIndex = manual == null && index >= 2 ? 0 : manual != null && index < 2 ? 2 : index;
        }

        protected void LoadImage()
        {
            if (BannerImg != null) LoadImage(BannerImg.Source);
            else ValueChanged(null, new EventArgs());
        }

        protected void LoadImage(string path)
        {
            BannerImg = new ImageHelper(TargetPlatform, path);
            LoadImage(BannerImg.Source);
        }

        #region /////////////////////////////////////////////// Inheritable functions ///////////////////////////////////////////////
        /// <summary>
        /// Additionally edit image before generating files, e.g. with modification of image palette/brightness, used only for images with exact resolution of original screen size
        /// </summary>
        // protected abstract void platformImageFunction(Bitmap src);

        protected void platformImageFunction(Bitmap src)
        {
            Bitmap bmp = null;

            switch (TargetPlatform)
            {
                case Platform.NES:
                    bmp = cloneImage(src);
                    if (bmp == null) return;

                    if (contentOptions != null && bool.Parse(contentOptions.ElementAt(1).Value))
                    {
                        var contentOptionsNES = contentOptionsForm as Options_VC_NES;
                        var palette = contentOptionsNES.ImgPalette(src);

                        if (palette != -1 && src.Width == 256 && (src.Height == 224 || src.Height == 240))
                            bmp = contentOptionsNES.SwapColors(bmp, contentOptionsNES.Palettes[palette], contentOptionsNES.Palettes[int.Parse(contentOptions.ElementAt(0).Value)]);
                    }
                    else bmp = src;
                    break;

                case Platform.SMS:
                case Platform.SMD:
                    break;
            }

            BannerImg.Generate(bmp ?? src);
        }

        private Bitmap cloneImage(Bitmap src)
        {
            try { return (Bitmap)src.Clone(); } catch { try { return (Bitmap)BannerImg?.Source.Clone(); } catch { return null; } }
        }
        #endregion

        private void resetImages(bool bannerOnly = false)
        {
            banner.Image = preview.Banner
                (
                    BannerImg?.VCPic,
                    banner_form.title.Text,
                    (int)banner_form.released.Value,
                    (int)banner_form.players.Value,
                    TargetPlatform,
                    _bannerRegion
                );

            if (!bannerOnly)
            {
                savedata.Picture.Image = BannerImg?.SaveIcon();
            }
        }

        protected bool LoadImage(Bitmap src)
        {
            if (src == null) return false;

            try
            {
                Invoke(new MethodInvoker(delegate
                {
                    BannerImg.InterpMode = (InterpolationMode)image_interpolation_mode.SelectedIndex;
                    BannerImg.FitAspectRatio = image_resize1.Checked;

                    platformImageFunction(src);

                    if (BannerImg.Source != null)
                    {
                        resetImages();
                        ValueChanged(null, new EventArgs());
                    }
                }));

                return true;
            }

            catch
            {
                MessageBox.Show(Program.Lang.Msg(1, 1));
                return false;
            }
        }

        public void LoadROM(string ROMpath, bool AutoScan = true, bool filter = false) {

            bool filtered = filter && !browseROM.Filter.ToLower().Contains(Path.GetExtension(ROMpath).ToLower());
            if (ROMpath == null || rom == null || !File.Exists(ROMpath) || filtered) {
                SystemSounds.Beep.Play();
                return;
            }

            switch (TargetPlatform) {
                // ROM file formats
                // ****************
                default:
                    if (!rom.CheckValidity(ROMpath)) {
                        MessageBox.Show(Program.Lang.Msg(2), 0, MessageBox.Icons.Warning);
                        return;
                    }
                    else IsEmpty = false;

                    if (TargetPlatform == Platform.RPGM && (rom as RPGM).GetTitle(ROMpath) != null) {
                        banner_form.title.Text = (rom as RPGM).GetTitle(ROMpath);
                        if (_bannerTitle.Length <= channel_name.MaxLength) channel_name.Text = banner_form.title.Text;
                        resetImages(true);
                    }
                    break;

                // Disc format
                // ****************
                case Platform.PSX:
                    IsEmpty = false;
                    break;

                // Flash SWF format
                // ****************
                case Platform.Flash:
                    if (!rom.CheckValidity(ROMpath)) {
                        MessageBox.Show(Program.Lang.Msg(2), 0, MessageBox.Icons.Warning);
                        return;
                    }
                    else IsEmpty = false;

                    (rom as SWF).Parse(ROMpath);
                    genre.Text = "Flash";
                    genre.Enabled = genre_l.Enabled = false;
                    break;
            }
            rom.FilePath = ROMpath;
            getUniqueTID();
            patch = null;
            Program.MainForm.toolbarGameScan.Enabled = Program.MainForm.game_scan.Enabled = ToolbarButtons[1];

            if (rom != null) {
                if (AutoScan && ToolbarButtons[1]) GameScan(false);
                else if (string.IsNullOrEmpty(genre.Text)) GetDbGenre();
            }
            setFilesText();
        }

        /// <summary>
        ///     Gets the proper Shop.wii style genre
        /// </summary>
        public static readonly Dictionary<string, string[]> GenreMap = new(StringComparer.OrdinalIgnoreCase) {
            { "RPG", new[] { "Role playing game", "Role playing" } },
            { "Platform", new[] { "Platformer", "Platforming" } },
            // need to collect officially used genres, and database genres
            //      for now this is theese are only notable needed fixes
        };

        /// <summary>
        ///     Formats a game genre by direct and fuzzy matching key val pairs in the <c>GenreMap</c> dict
        ///         then provides the correct 'official usage' genre
        /// </summary>
        public static string FormatGenre(string genre) {
            if (string.IsNullOrWhiteSpace(genre))
                return genre;

            // direct match on key
            if (GenreMap.ContainsKey(genre.Trim()))
                return genre.Trim();

            // check vals
            foreach (var kvp in GenreMap) {
                if (kvp.Value.Any(val => genre.Trim().Equals(val, StringComparison.OrdinalIgnoreCase)))
                    return kvp.Key;
            }

            // fuzzy match (plural, minor typos)
            foreach (var kvp in GenreMap) {
                if (kvp.Value.Any(val => genre.Trim().StartsWith(val, StringComparison.OrdinalIgnoreCase) || genre.Trim().EndsWith(val, StringComparison.OrdinalIgnoreCase)))
                    return kvp.Key;

                // Levenshtein distance if all else fails and is close enough
                if (kvp.Value.Any(val => Utils.LevenshteinDistance(genre.Trim(), val) <= 2))
                    return kvp.Key;
            }
            return genre.Trim();
        }

        /// <summary>
        ///     Formats a ROM title string by removing known junk/lang tags and whitespace
        /// </summary>
        public static string FormatROMT(string romt) {
            // remove lang flags
            romt = Regex.Replace(romt, @"\((Ja|En|De|Fr|Es|It|Unl)\)", "", RegexOptions.IgnoreCase);

            // remove known junk/revision/version/proto/etc. tags
            string[] junkTags = {
                "Patched", "Proto", "Beta", "Sample", "Demo", "Unl", "Pirate",
                "Aftermarket", "SegaNet", "Program", "LodgeNet", "Switch Online", "Virtual Console"
            };
            string pattern = $@"\((Rev\s?[A-Z0-9]+|V\d+(\.\d+)?|Alt(\s?\d+)?|{string.Join("|", junkTags)})\)";
            romt = Regex.Replace(romt, pattern, "", RegexOptions.IgnoreCase);

            // remove year/date tags, then [b] [!] tags etc, then whitespace
            romt = Regex.Replace(romt, @"\(([0-9]{1,4}([-/][0-9]{1,2}){0,2})\)", "", RegexOptions.IgnoreCase);
            romt = Regex.Replace(romt, @"\[[^\]]+\]", "", RegexOptions.IgnoreCase);
            romt = Regex.Replace(romt, @"\s{2,}", " ");
            return romt.Trim();
        }

        /// <summary>
        ///     Attempt to find and strip a region tag from the provided ROM title
        /// </summary>
        /// <returns>
        ///     The normalized region group key ("USA", "Europe", "Japan", "World") or <c>null</c> if no region is found
        /// </returns>
        public static string ExtractRegion(ref string name) {
            var regionGroups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
                { "USA",     new[] { "USA", "U", "US", "America" } },
                { "Europe",  new[] { "Europe", "EUR", "EU", "E" } },
                { "Japan",   new[] { "Japan", "JP", "J", "JPN" } },
                { "World",   new[] { "World", "W" } },
                
                { "France",  new[] { "France", "FR", "F" } },
                { "Germany", new[] { "Germany", "DE", "G" } },
                { "Spain",   new[] { "Spain", "ES" } },
                { "Italy",   new[] { "Italy", "IT" } },
                { "Korea",   new[] { "Korea", "KR", "KOR" } },
                { "Brazil",  new[] { "Brazil", "BR" } },
                { "Taiwan",  new[] { "Taiwan", "TW" } }
            };
            string matchedRegion = null;
            var match = Regex.Match(name, @"\(([^)]+)\)", RegexOptions.IgnoreCase);

            if (match.Success) {
                string contents = match.Groups[1].Value;
                var parts = contents.Split(',').Select(p => p.Trim());

                // loop through regionGroups dict keys to check each region in order of priority/insertion order
                foreach (var pri in regionGroups.Keys) {
                    if (parts.Any(part => regionGroups[pri].Any(variant => string.Equals(part, variant, StringComparison.OrdinalIgnoreCase)))) {
                        matchedRegion = pri;
                        break;
                    }
                }
                name = name.Remove(match.Index, match.Length).Trim();
            }
            name = FormatROMT(name);
            return matchedRegion;
        }

        /// <summary>
        ///     Attempts to match an input ROM file name to entries in the provided DataTable,
        ///         using normalized title and region, in both the input ROM file name and the DataTable.
        /// </summary>
        /// <param name="dt">The DataTable containing ROM entries, must have a "name" column!</param>
        /// <param name="romName">The raw ROM filename or title to look up</param>
        /// <param name="romRegion">The stripped region indicator from the filename or title</param>
        /// <returns>
        ///     A matching <see cref="DataRow"/> (title) if found; else <c>null</c>.
        /// </returns>
        /// <remarks>
        /// The lookup proceeds in the following steps:
        ///     1). 'Normalize' ROM name via <c>FormatROMT</c>
        /// 
        ///     2). Define known region groups and their shorthand variants
        /// 
        ///     3). Attempt to resolve <paramref name="romRegion"/> into one of these groups
        /// 
        ///     4). Collect all DataTable rows whose normalized title matches the ROM title
        /// 
        ///     5). If no rows match, return <c>null</c>
        /// 
        ///     6). If a region group was detected, prefer a row whose original title
        ///         explicitly matches one of that group's region variants
        /// 
        ///     7). *if no matches* and all else fails <c>LevenshteinDistance</c> fuzzy match
        /// 
        ///     8). return the first matching row if there was one
        /// </remarks>
        private static DataRow DbLookup(DataTable dt, string romName, string romRegion) {
            Logger.INFO($"Searching for game \"{romName}\" with detected region \"{romRegion}\"");
            string romTrim = FormatROMT(romName);

            var regions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
                { "USA", new[] { "U", "US", "USA", "America" } },
                { "Europe", new[] { "E", "EU", "EUR", "Europe" } },
                { "Japan", new[] { "J", "JP", "Japan" } },
                { "World", new[] { "W", "World" } }
            };

            // Match romRegion to region dict group
            string regionGroup = null;
            foreach (var kv in regions) {
                if (kv.Value.Any(r => string.Equals(r, romRegion, StringComparison.OrdinalIgnoreCase))) {
                    regionGroup = kv.Key;
                    break;
                }
            }

            var titleMatches = new List<DataRow>();
            foreach (DataRow row in dt.Rows) {
                if (row["name"] == null) continue;

                string dbOgTitle = row["name"].ToString();
                string dbCopy = dbOgTitle; // ehh s= need to keep working copy
                string dbRegion = ExtractRegion(ref dbCopy); // strip region + normalize

                if (string.Equals(dbCopy, romTrim, StringComparison.OrdinalIgnoreCase))
                    titleMatches.Add(row);
            }

            if (titleMatches.Count == 0) {
                // try fuzzy match
                Logger.INFO($"No direct matches found, attempting fuzzy match...");
                string squished = Regex.Replace(romTrim, @"\s+", "").ToLowerInvariant();
                int threshold = (int)(squished.Length * 0.1); // roughly ~10% difference allowed

                foreach (DataRow row in dt.Rows) {
                    if (row["name"] == null) continue;

                    string dbOgTitle = row["name"].ToString();
                    string dbCopy = dbOgTitle;
                    ExtractRegion(ref dbCopy); // normalize
                    string dbSquish = Regex.Replace(dbCopy, @"\s+", "").ToLowerInvariant();

                    // Use Levenshtein distance on normalized rom title & db title with threshold for the fuzzy match
                    if (Utils.LevenshteinDistance(squished, dbSquish) <= threshold)
                        return row;
                }
                // Nothing found even with fallback
                return null;
            }

            // If region was detected, try to pick the titleMatch with a region match from the region group
            if (regionGroup != null) {
                foreach (var row in titleMatches) {
                    string dbOgTitle = row["name"].ToString();
                    string[] regVariants = regions[regionGroup];
                    if (regVariants.Any(rv => Regex.IsMatch(dbOgTitle, $@"\({rv}\)", RegexOptions.IgnoreCase)))
                        return row;
                }
            }
            return titleMatches[0];
        }

        private void LocSaveBanner(string path) {
            if (string.IsNullOrEmpty(path) || !Program.Config.application.locsave_banner) return;
            try {
                string locsaveDir = Program.Config.application.locsave_banner_tb;
                string bannersDir = string.IsNullOrEmpty(locsaveDir) ? PathConstants.DefaultLocSaveBanners : locsaveDir;

                if (!Directory.Exists(bannersDir))
                    Directory.CreateDirectory(bannersDir);

                using System.Net.WebClient cl = new();
                string fileName = Path.GetFileName(new Uri(path).LocalPath);
                string localPath = Path.Combine(bannersDir, fileName);

                string skip = "Skipping banner download, already stored locally";
                Logger.INFO(!File.Exists(localPath) ? $"Downloading banner image:\n{path}" : skip);

                if (!File.Exists(localPath))
                    cl.DownloadFile(path, localPath);
            }
            catch (Exception ex) {
                Logger.ERROR($"Failed to fetch banner image {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets any game metadata that is available for the file based on its CRC32 reading hash, including the software title, year, players, and title image URL.
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="path">The ROM/ISO path.</param>
        /// <returns></returns>
        protected (string Name, string Serial, string Year, string Players, string Image, string Genre, bool IsComplete) GetGameData(Platform platform, string path, bool genreOnly = false) {
            string fileName = Utils.GetFileCN(path);
            Logger.INFO("Processing game data:", $"Game:\"{fileName}\", Platform: \"{platform}\"", $"Path:\"{path}\"");

            // disc-based
            if (platform is Platform.PCECD or Platform.GCN or Platform.SMCD or Platform.PSX) {
                if (Path.GetExtension(path).ToLower() == ".cue") {
                    foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(path))) {
                        if ((Path.GetExtension(item).ToLower() is ".bin" or ".iso") && Utils.GetFileCN(path).Equals(Utils.GetFileCN(item), StringComparison.OrdinalIgnoreCase)) {
                            path = item;
                            break;
                        }
                    }
                }
                if (Path.GetExtension(path).ToLower() is not ".bin" and not ".iso") {
                    Logger.WARN($"Unsupported disc file extension for {path}");
                    return (null, null, null, null, null, null, false);
                }
            }

            //  Download platform database if not found,
            //      then notify user if downloading or moving on to fetch game data
            bool dldb = Databases.LibRetro.IsWeb(platform);
            if (dldb) Web.InternetTest();
            string fetching = $"Attempting to fetch game {(genreOnly ? "genre!" : "data!")}";
            Program.MainForm.Wait(dldb ? $"Downloading '{platform}' database..." : fetching);

            // attempt CRC match
            var result = Databases.LibRetro.Read(path, platform);
            if (!string.IsNullOrEmpty(result.Name)) {
                Logger.INFO($"CRC match found for '{path}' -> '{result.Name}'");
                result.Name = System.Text.RegularExpressions.Regex.Replace(result.Name?.Replace(": ", Environment.NewLine).Replace(" - ", Environment.NewLine), @"\((.*?)\)", "").Trim();
                
                if (result.Name.Contains(", The"))
                    result.Name = "The " + result.Name.Replace(", The", string.Empty);

                if (!genreOnly && !string.IsNullOrEmpty(result.Image) && Program.Config.application.locsave_banner)
                    LocSaveBanner(result.Image);
                return result;
            }
            Logger.INFO("No CRC match found, attempting name lookup...");
            DataTable dt = Databases.LibRetro.Parse(platform);
            if (dt == null) {
                Logger.WARN("Database parsing failed or returned null.");
                return (null, null, null, null, null, null, false);
            }
            // attempt filename lookup if CRC match fails

            string region = ExtractRegion(ref fileName);
            DataRow row = DbLookup(dt, fileName, region);
            if (row == null) {
                Logger.WARN($"No game title or CRC matches could be found in the database.");
                return (null, null, null, null, null, null, false);
            }

            var data = new Dictionary<string, string> {
                ["name"] = row.Table.Columns.Contains("name") ? row["name"]?.ToString() : null,
                ["serial"] = row.Table.Columns.Contains("serial") ? row["serial"]?.ToString() : null,
                ["year"] = row.Table.Columns.Contains("releaseyear") ? row["releaseyear"]?.ToString() : null,
                ["players"] = row.Table.Columns.Contains("users") ? row["users"]?.ToString() : null,
                ["image"] = row.Table.Columns.Contains("image") ? row["image"]?.ToString() : null,
                ["genre"] = row.Table.Columns.Contains("db_genre") ? row["db_genre"]?.ToString() : null
            };
            if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(data["name"]) && !data["name"].Contains(region))
                data["name"] += $" ({region})";

            if (!string.IsNullOrEmpty(data["name"])) {
                data["name"] = System.Text.RegularExpressions.Regex
                    .Replace(data["name"].Replace(": ", Environment.NewLine).Replace(" - ", Environment.NewLine), @"\((.*?)\)", "").Trim();
                data["name"] = data["name"].Contains(", The") ? "The " + data["name"].Replace(", The", "") : data["name"];
            }

            bool complete = data.Where(kvp => kvp.Key != "serial").All(kvp => !string.IsNullOrEmpty(kvp.Value));
            if (genreOnly && !string.IsNullOrEmpty(data["genre"]))
                complete = true;
            else if (platform == Platform.C64 || platform == Platform.PCECD)
                complete = !string.IsNullOrEmpty(data["name"]) && !string.IsNullOrEmpty(data["image"]);

            if (!genreOnly && !string.IsNullOrEmpty(data["image"]) && Program.Config.application.locsave_banner)
                LocSaveBanner(data["image"]);

            Logger.INFO($"Filename match successful for '{fileName}' -> '{data["name"]}'");
            return (data["name"], data["serial"], data["year"], data["players"], data["image"], FormatGenre(data["genre"]), complete);
        }

        /// <summary>
        ///     Calls #GetGameData to fetch any valid database values and attempt to apply them
        ///         Updates any relevant elements (text boxes, banner image)
        /// </summary>
        /// <param name="imageOnly">Determines if we should fetch *only* the banner image</param>
        public async void GameScan(bool imageOnly) {
            GameScanned = false;
            if (rom?.FilePath == null) return;
            try {
                var gameData = await Task.Run(() => GetGameData(TargetPlatform, rom.FilePath));
                bool retrieved = imageOnly ? !string.IsNullOrEmpty(gameData.Image) : gameData != (null, null, null, null, null, null, false);

                if (retrieved && !imageOnly) {
                    banner_form.title.Text = gameData.Name ?? banner_form.title.Text;
                    channel_name.Text = GetDbString(gameData.Name, channel_name);
                    genre.Text = GetDbString(gameData.Genre, genre);
                    banner_form.released.Value = int.TryParse(gameData.Year, out int year) ? year : banner_form.released.Value;
                    banner_form.players.Value = int.TryParse(gameData.Players, out int ply) ? ply : banner_form.players.Value;
                    linkSaveDataTitle();
                }
                // Load image if present
                if (retrieved && !string.IsNullOrEmpty(gameData.Image))
                    await Task.Run(() => LoadImage(gameData.Image));

                await Task.Run(() => resetImages(true));
                SystemSounds.Beep.Play();
                Program.MainForm.Wait(false, false, false);

                if (!gameData.IsComplete && !imageOnly) {
                    var mgd = new Dictionary<string, bool> {
                        { "Title", string.IsNullOrEmpty(gameData.Name) || gameData.Name   == "Unknown" },
                        { "Genre", string.IsNullOrEmpty(gameData.Genre) || gameData.Genre  == "Unknown" },
                        { "Date", string.IsNullOrEmpty(gameData.Year) },
                        { "Players", string.IsNullOrEmpty(gameData.Players) },
                        { "Banner Image", string.IsNullOrEmpty(gameData.Image) }
                    };
                    var missingKeys = mgd.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
                    if (missingKeys.Any()) {
                        var translations = await Task.WhenAll(missingKeys.Select(key => GoogleTrans.PrefTranslate(key)));
                        MessageBox.Show(Program.Lang.Msg(4) + "  • " + string.Join("\n  • ", translations));
                    }
                }
                GameScanned = true;
            }
            catch (Exception ex) {
                Program.MainForm.Wait(false, false, false);
                if (Program.DebugMode) throw;
                else MessageBox.Error(ex.Message);
            }
        }

        /// <summary>
        ///     Fetches only the genre for the current ROM and updates the text box
        /// </summary>
        public async void GetDbGenre() {
            if (rom?.FilePath == null) return;
            try {
                var gameData = await Task.Run(() => GetGameData(TargetPlatform, rom.FilePath, true));
                genre.Text = string.IsNullOrEmpty(genre.Text) ? GetDbString(gameData.Genre, genre) : genre.Text;
                await Task.Run(() => resetImages(true)); // cosmetic, and slight pause allows reading
                Program.MainForm.Wait(false, false, false);
            }
            catch (Exception ex) {
                if (Program.DebugMode) throw;
                else MessageBox.Error(ex.Message);
            }
        }

        /// <summary>
        ///     Gets the first line of a database string for their text boxes
        /// </summary>
        private string GetDbString(string dbData, TextBox tb) {
            var parts = string.IsNullOrEmpty(dbData)
                ? Array.Empty<string>() : dbData.Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string result = parts.Length > 0 ? parts[0] : "Unknown";
            return result.Length <= tb.MaxLength ? result : tb.Text;
        }

        public void SaveToWAD(string targetFile = null) => backgroundWorker.RunWorkerAsync(targetFile);
        private void saveToWAD_UpdateProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e) => Program.MainForm.Wait(true, false, true, e.ProgressPercentage);
        private void saveToWAD(object sender, System.ComponentModel.DoWorkEventArgs e) {
            Exception error = null;
            string targetFile = e.Argument.ToString();
            targetFile ??= PathConstants.WorkingFolder + "out.wad";

            try {
                Method m = new(TargetPlatform) {
                    ROM = rom,
                    Patch = patch,
                    Img = BannerImg,

                    ChannelTitles = _channelTitles,
                    BannerTitle = _bannerTitle,
                    BannerYear = _bannerYear,
                    BannerPlayers = _bannerPlayers,
                    BannerSound = _bannerSound,

                    // IsMultifile = multifile_software.Checked,
                    BannerRegion = _bannerRegion,
                    SaveDataTitle = _saveDataTitle,
                    Settings = (contentOptions, keymap.Enabled ? keymap.List ?? null : null),

                    WAD = WAD.Load(Properties.Resources.StaticBase),
                    WadRegion = (int)outWadRegion,
                    // WadVideoMode = video_modes.SelectedIndex,
                    // WiiUDisplay = wiiu_display.SelectedIndex,
                    EmuVersion = emuVer,

                    // Manual = manual,
                    TitleID = _tID,
                    Genre = _genre,

                    Out = targetFile,
                };

                string emulator = null;
                Forwarder.Storages device = 0;
                Invoke(new MethodInvoker(delegate {
                    m.IsMultifile = multifile_software.Checked;
                    m.WadVideoMode = video_mode.SelectedIndex;
                    m.WiiUDisplay = wiiu_display.SelectedIndex;
                    m.Manual = manual_type.SelectedIndex == 0 ? null : manual_type.SelectedIndex == 1 ? "orig" : manual;
                    emulator = injection_methods.SelectedItem.ToString();
                    device = forwarder_root_device.SelectedIndex == 1 ? Forwarder.Storages.USB : Forwarder.Storages.SD;
                }));

                int wad_tries = 0;

                Start:
                bool hasInWad = InBaseWAD != null;
                Program.MainForm.Wait(false, false, false);

                // Get WAD data
                // *******
                if (hasInWad) m.GetWAD(InBaseWAD, baseID.Text, hasInWad);
                else {
                    var entry = channels.Entries.Where(x => x.GetUpperIDs().Contains(baseID.Text)).ToArray()[0];
                    var index = Array.IndexOf(entry.GetUpperIDs(), baseID.Text);
                    m.GetWAD(entry.GetWAD(index), entry.GetUpperID(index), hasInWad);
                }
                backgroundWorker.ReportProgress(m.Progress);

                try {
                    if (File.Exists(patch)) {
                        try {
                            m.ROM.Patch(patch);
                        }
                        catch (Exception ex) {
                            Logger.ERROR($"Failed to patch ROM: {ex.Message}\n{ex.StackTrace}");
                            throw;
                        }
                    }
                    Logger.INFO($"Target Platform: {TargetPlatform}");
                    switch (TargetPlatform) {
                        case Platform.NES:
                        case Platform.SNES:
                        case Platform.N64:
                        case Platform.SMS:
                        case Platform.SMD:
                        case Platform.PCE:
                        case Platform.PCECD:
                        case Platform.NEO:
                        case Platform.C64:
                        case Platform.MSX:
                            try {
                                if (IsVirtualConsole)
                                    m.Inject();
                                else
                                    m.CreateForwarder(emulator, device);
                            }
                            catch (KeyNotFoundException ex) {
                                Logger.ERROR($"KeyNotFoundException in Inject/CreateForwarder for platform {TargetPlatform}:\n{ex.Message}\n{ex.StackTrace}");
                                throw; // rethrow to outer catch
                            }
                            catch (Exception ex) {
                                Logger.ERROR($"Unexpected exception in Inject/CreateForwarder for platform {TargetPlatform}:\n{ex.Message}\n{ex.StackTrace}");
                                throw;
                            }
                            break;

                        case Platform.Flash:
                            try {
                                m.Inject();
                            }
                            catch (Exception ex) {
                                Logger.ERROR($"Exception in Flash Inject(): {ex.Message}\n{ex.StackTrace}");
                                Logger.ERROR("# Var details:");
                                Logger.ERROR(
                                    $"m.TitleID: {(m.TitleID == null ? "null" : "set")}, ",
                                    $"m.WAD: {(m.WAD == null ? "null" : "set")}, ",
                                    $"m.ROM: {(m.ROM == null ? "null" : "set")}",
                                    $"m.Img: {(m.Img == null ? "null" : "set")}",
                                    $"m.Genre: {(m.Genre == null ? "null" : "set")}",
                                    $"m.ChannelTitles: {(m.ChannelTitles == null ? "null" : "set")}",
                                    $"m.BannerTitle: {(m.BannerTitle == null ? "null" : "set")}",
                                    $"Settings.List: {(contentOptions == null ? "null" : "set")}",
                                    $"Settings.Keymap: {(keymap.List == null ? "null" : "set")}"
                                );
                                throw;
                            }
                            break;

                        case Platform.GB:
                        case Platform.GBC:
                        case Platform.GBA:
                        case Platform.S32X:
                        case Platform.SMCD:
                        case Platform.PSX:
                        case Platform.RPGM:
                            try {
                                m.CreateForwarder(emulator, device);
                            }
                            catch (Exception ex) {
                                Logger.ERROR($"Exception in CreateForwarder for platform {TargetPlatform}: {ex.Message}\n{ex.StackTrace}");
                                throw;
                            }
                            break;

                        default:
                            throw new NotImplementedException($"Unhandled platform: {TargetPlatform}");
                    }
                }
                catch (Exception ex) {
                    // Outer catch: log everything possible for debugging
                    Logger.ERROR("=== Export failed with exception ===");
                    Logger.ERROR($"Message: {ex.Message}");
                    Logger.ERROR($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                        Logger.ERROR($"InnerException: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                    Logger.ERROR($"Platform: {TargetPlatform}, isVC: {IsVirtualConsole}, PatchFile: {patch}");
                    
                    if (!hasInWad && ex.Message.Contains("U8 Header: Invalid Magic!")) {
                        if (wad_tries == 0) {
                            wad_tries++;
                            Logger.ERROR("Retrying WAD download due to U8 Header: Invalid Magic!");
                            goto Start;
                        }
                        else {
                            Logger.ERROR("WAD invalid or failed twice. Process halted.");
                            throw;
                        }
                    }
                    throw; // rethrow so outer code can handle it
                }
                backgroundWorker.ReportProgress(m.Progress);

                // Change WAD region & internal main.dol things
                // *******

                // Other WAD settings to be changed done by WAD creator helper, which will save to a new file
                // *******
                m.EditMetadata();
                backgroundWorker.ReportProgress(m.Progress);
                m.EditBanner();
                backgroundWorker.ReportProgress(m.Progress);
                m.Save();
                backgroundWorker.ReportProgress(m.Progress);

                // write inject/WAD meta.json to Banner U8
                if (Program.Config.application.write_metadata) {
                    Logger.Sub("Please wait while inject metadata is added...");
                    Meta.Write(m, targetFile, InBaseRegion);
                }
                if (File.Exists(targetFile) && File.ReadAllBytes(targetFile).Length > 10) error = null;
                else throw new Exception(Program.Lang.Msg(7, 1));
            }
            catch (Exception ex) { 
                error = ex; 
            }
            finally {
                Program.MainForm.Wait(false, false, false);
                Program.CleanTemp();

                Invoke(new MethodInvoker(delegate {
                    if (error == null) {
                        SystemSounds.Beep.Play();
                        switch (MessageBox.Show(Program.Lang.Msg(3), null, MessageBox.Buttons.YesNo, MessageBox.Icons.Information)) {
                            case MessageBox.Result.Yes:
                                System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{targetFile}\"");
                                break;
                        }
                    }
                    else {
                        if (Program.DebugMode) throw error;
                        else {
                            string msg = error.Message;
                            if (!string.IsNullOrWhiteSpace(error.InnerException?.Message)) msg += Environment.NewLine + error.InnerException?.Message;
                            MessageBox.Error(msg);
                        }
                    }
                }));
            }
        }
        #endregion

        #region **Console-Specific Functions**
        // ******************
        // CONSOLE-SPECIFIC
        // ******************
        private void openInjectorOptions(object sender, EventArgs e)
        {
            if (contentOptionsForm == null) return;

            contentOptionsForm.Font = Program.MainForm.Font;
            contentOptionsForm.Text = Program.Lang.String(injection_method_options.Name, Name).TrimEnd('.').Trim();
            var result = contentOptionsForm.ShowDialog(this) == DialogResult.OK;

            switch (TargetPlatform)
            {
                default:
                    if (result) { ValueChanged(sender, e); }
                    break;

                case Platform.NES:
                    if (result) { LoadImage(); }
                    break;
            }
        }
        private void FetchPatchData(object sender, EventArgs e) {
            var raw = fetch_patch.Text?.Trim();
            if (string.IsNullOrWhiteSpace(raw)) {
                //string errInput = Program.Lang.String("err_patch_input");
                //MessageBox.Show("Invalid Input", errInput, MessageBox.Buttons.Ok, MessageBox.Icons.Warning);
                MessageBox.Show(Program.Lang.Msg(17));
                return;
            }
            string url = null;
            bool validUrl = Regex.IsMatch(raw, @"^https?://(www\.)?romhacking\.net/(hacks|translations)/\d+/?$", RegexOptions.IgnoreCase);

            if (validUrl || Regex.IsMatch(raw, @"^/?(hacks|translations)/\d+/?$", RegexOptions.IgnoreCase))
                url = validUrl ? raw : "https://www.romhacking.net/" + raw.TrimStart('/').TrimEnd('/') + "/";
            else {
                //string errUrl = Program.Lang.String("err_patch_url");
                //MessageBox.Show(errUrl, "Unsupported URL", MessageBox.Buttons.Ok, MessageBox.Icons.Error);
                MessageBox.Show(Program.Lang.Msg(18));
                return;
            }
            var info = Romhacking.GetEntry(url);
            var released = info.Released ?? "";
            var match = Regex.Match(released, @"\b\d{4}\b");
            banner_form.released.Value = match.Success && int.TryParse(match.Value, out var year) ? year : banner_form.released.Value;
            banner_form.title.Text = info.Title ?? banner_form.title.Text;
            savedata.Lines[0] = info.Title ?? savedata.Lines[0];
            linkSaveDataTitle();
            channel_name.Text = info.Title ?? channel_name.Text;
            genre.Text = info.Genre;
            LoadImage(info.ThumbUrl);

            if (info.AllDLUrls != null)
                Romhacking.PromptDLPatch(info.AllDLUrls);

            resetImages();
        }
        #endregion

        #region Base WAD Management/Visual
        private void AddBases()
        {
            Base.Items.Clear();

            foreach (var entry in channels.Entries)
            {
                var title = entry.Regions.Contains(0) && Program.Lang.GetRegion() is Language.Region.Japan ? entry.Titles[0]
                          : entry.Regions.Contains(0) && Program.Lang.GetRegion() is Language.Region.Korea ? entry.Titles[entry.Titles.Count - 1]
                          : entry.Regions.Contains(0) && entry.Regions.Count > 1 ? entry.Titles[1]
                          : entry.Titles[0];

                Base.Items.Add(title);
            }

            if (Base.Items.Count > 0) { Base.SelectedIndex = 0; }

            Base.Enabled = Base.Items.Count > 1;
            UpdateBaseForm();
        }

        // -----------------------------------

        private void Base_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ----------------------------
            if (DesignMode || Base.SelectedIndex < 0) return;
            // ----------------------------

            var regions = new List<string>();
            for (int i = 0; i < channels.Entries[Base.SelectedIndex].Regions.Count; i++)
            {
                switch (channels.Entries[Base.SelectedIndex].Regions[i])
                {
                    case 0:
                        regions.Add(Program.Lang.String("region_j"));
                        break;

                    case 1:
                    case 2:
                        regions.Add(Program.Lang.String("region_u"));
                        break;

                    case 3:
                    case 4:
                    case 5:
                        regions.Add(Program.Lang.String("region_e"));
                        break;

                    case 6:
                    case 7:
                        regions.Add(Program.Lang.String("region_k"));
                        break;

                    default:
                    case 8:
                        regions.Add(Program.Lang.String("region_rf"));
                        break;
                }
            }

            // Check if language is set to Japanese or Korean
            // If so, make Japan/Korea region item the first in the WAD region context list
            // ********

            var selected = -1;
            
            Language.Region region = Program.Lang.GetRegion();
            if      (region is Language.Region.Korea)   selected = regions.IndexOf(Program.Lang.String("region_k"));
            else if (region is Language.Region.Japan)   selected = regions.IndexOf(Program.Lang.String("region_j"));
            else if (region is Language.Region.Europe)  selected = regions.IndexOf(Program.Lang.String("region_e"));

            if (selected == -1) selected = regions.IndexOf(Program.Lang.String("region_u"));
            if (selected == -1) selected = 0;

            // Reset currently-selected base info
            // ********
            baseRegionList.Items.Clear();

            // Add regions to WAD region context list
            // ********
            for (int i = 0; i < channels.Entries[Base.SelectedIndex].Regions.Count; i++)
            {
                switch (channels.Entries[Base.SelectedIndex].Regions[i])
                {
                    case 0:
                        baseRegionList.Items.Add(Program.Lang.String("region_j"), null, WADRegionList_Click);
                        break;

                    case 1:
                    case 2:
                        baseRegionList.Items.Add(Program.Lang.String("region_u"), null, WADRegionList_Click);
                        break;

                    case 3:
                    case 4:
                    case 5:
                        baseRegionList.Items.Add(Program.Lang.String("region_e"), null, WADRegionList_Click);
                        break;

                    case 6:
                    case 7:
                        baseRegionList.Items.Add(Program.Lang.String("region_k"), null, WADRegionList_Click);
                        break;

                    default:
                    case 8:
                        baseRegionList.Items.Add(Program.Lang.String("region_rf"), null, WADRegionList_Click);
                        break;
                }
            }

            // Final visual updates
            // ********
            UpdateBaseForm(selected);
            BaseRegion.Cursor = baseRegionList.Items.Count == 1 ? Cursors.Default : Cursors.Hand;
        }

        private void WADRegion_Click(object sender, EventArgs e)
        {
            if (baseRegionList.Items.Count > 1)
                baseRegionList.Show(this, PointToClient(Cursor.Position));
        }

        private void WADRegionList_Click(object sender, EventArgs e)
        {
            string targetRegion = (sender as ToolStripMenuItem).Text;

            for (int i = 0; i < baseRegionList.Items.Count; i++)
            {
                if ((baseRegionList.Items[i] as ToolStripMenuItem).Text == targetRegion)
                {
                    UpdateBaseForm(i);
                    return;
                }
            }
        }

        private void UpdateBaseForm(int index = -1)
        {
            if (index == -1)
            {
                for (index = 0; index < channels.Entries[Base.SelectedIndex].Regions.Count; index++)
                    if (channels.Entries[Base.SelectedIndex].GetUpperID(index)[3] == baseID.Text[3])
                        goto Set;

                return;
            }

            Set:
            // Native name & Title ID
            // ********
            baseName.Text = channels.Entries[Base.SelectedIndex].Titles[index];
            baseID.Text = channels.Entries[Base.SelectedIndex].GetUpperID(index);

            if (baseRegionList.Items.Count > 0)
            {
                foreach (ToolStripMenuItem item in baseRegionList.Items.OfType<ToolStripMenuItem>())
                    item.Checked = false;
                (baseRegionList.Items[index] as ToolStripMenuItem).Checked = true;
            }

            // Flag
            // ********
            BaseRegion.Image = channels.Entries[Base.SelectedIndex].Regions[index] switch
            {
                0 => Properties.Resources.flag_jp,
                1 or 2 => Properties.Resources.flag_us,
                3 => (int)TargetPlatform <= 2 ? Properties.Resources.flag_eu50 : Properties.Resources.flag_eu,
                4 or 5 => (int)TargetPlatform <= 2 ? Properties.Resources.flag_eu60 : Properties.Resources.flag_eu,
                6 or 7 => Properties.Resources.flag_kr,
                _ => null,
            };
            savedata.Reset(TargetPlatform, (int)InBaseRegion);
            //CheckWarnings();
            resetImages();
            linkSaveDataTitle();
            resetContentOptions();
            ValueChanged(null, new EventArgs());
        }

        private int emuVer
        {
            get
            {
                if (channels != null)
                    foreach (var entry in channels.Entries)
                        for (int i = 0; i < entry.Regions.Count; i++)
                            if (entry.GetUpperID(i) == baseID.Text.ToUpper())
                                return entry.EmuRevs[i];

                return 0;
            }
        }

        /// <summary>
        /// Changes injector settings based on selected base/console
        /// </summary>
        private void resetContentOptions()
        {
            if (TargetPlatform == Platform.Flash && contentOptionsForm != null) return;

            contentOptionsForm = null;
            htmlDialog = null;

            bool hasWiiU = false;
            bool hasExtra = false;
            manual_type.Visible = false;
            forwarder_root_device.Visible = false;
            multifile_software.Visible = false;

            if (IsVirtualConsole)
            {
                hasExtra = true;
                extra.Text = Program.Lang.String(manual_type.Name, Name);
                manual_type.Visible = true;

                switch (TargetPlatform)
                {
                    case Platform.NES:
                        contentOptionsForm = new Options_VC_NES() { EmuType = emuVer };
                        break;

                    case Platform.SNES:
                        contentOptionsForm = new Options_VC_SNES();
                        htmlDialog = new(Program.Lang.HTML(1, false), injection_methods.SelectedItem.ToString());
                        break;

                    case Platform.N64:
                        contentOptionsForm = new Options_VC_N64() { EmuType = InBaseRegion == Region.Korea ? 3 : emuVer };
                        htmlDialog = new(Program.Lang.HTML(2, false), injection_methods.SelectedItem.ToString());
                        break;

                    case Platform.SMS:
                    case Platform.SMD:
                        contentOptionsForm = new Options_VC_SEGA() { EmuType = emuVer, IsSMS = TargetPlatform == Platform.SMS };
                        break;

                    case Platform.PCE:
                    case Platform.PCECD:
                        contentOptionsForm = new Options_VC_PCE();
                        break;

                    case Platform.NEO:
                        contentOptionsForm = new Options_VC_NEO() { EmuType = emuVer };
                        break;

                    case Platform.MSX:
                        break;

                    case Platform.C64:
                        break;
                }
            }

            else if (TargetPlatform == Platform.Flash)
            {
                hasExtra = true;
                extra.Text = Program.Lang.String(manual_type.Name, Name);
                manual_type.Visible = true;
                contentOptionsForm = new Options_Flash();
                multifile_software.Visible = true;
            }

            else
            {
                hasExtra = true;
                extra.Text = Program.Lang.String(forwarder_root_device.Name, Name);
                forwarder_root_device.Visible = true;

                switch (TargetPlatform)
                {
                    case Platform.GB:
                    case Platform.GBC:
                    case Platform.GBA:
                    case Platform.S32X:
                    case Platform.SMCD:
                    case Platform.PSX:
                        contentOptionsForm = new Options_Forwarder(TargetPlatform);
                        if (TargetPlatform == Platform.PSX) multifile_software.Visible = true;
                        break;
                    case Platform.NES:
                        break;
                    case Platform.SNES:
                        break;
                    case Platform.N64:
                        break;
                    case Platform.SMS:
                        break;
                    case Platform.SMD:
                        break;
                    case Platform.PCE:
                        break;
                    case Platform.PCECD:
                        break;
                    case Platform.NEO:
                        break;
                    case Platform.MSX:
                        break;
                    case Platform.C64:
                        break;
                    case Platform.Flash:
                        break;
                    case Platform.RPGM:
                        contentOptionsForm = new Options_RPGM();
                        break;
                    default:
                        break;
                }
            }

            if (contentOptionsForm != null)
            {
                contentOptionsForm.Font = Font;
                // contentOptionsForm.Text = Program.Lang.String("injection_method_options", "projectform").TrimEnd('.').Trim();
                contentOptionsForm.Icon = Icon.FromHandle((injection_method_options.Image as Bitmap).GetHicon());
            }

            /*if (!isVirtualConsole && manual != null)
            {
                manual = null;
                manual_type.SelectedIndex = 0;
            }*/

            ShowSaveData = IsVirtualConsole || TargetPlatform == Platform.Flash;
            download_image.Enabled = Databases.LibRetro.Exists(TargetPlatform);

            bool hasHelp = !string.IsNullOrWhiteSpace(htmlDialog?.FormText);
            injection_method_help.Visible = hasHelp && !IsEmpty;
            injection_methods.Size = hasHelp ? injection_methods.MinimumSize : injection_methods.MaximumSize;

            int space = 46;
            wiiu_display_l.Location = new Point(extra.Location.X, extra.Location.Y + (hasExtra ? space : 0));
            wiiu_display.Location = new Point(wiiu_display.Location.X, wiiu_display_l.Location.Y + 18);
            //int multimanu_y = targetPlatform == Platform.Flash ? 22 : 0;
            forwarder_root_device.Location = manual_type.Location = new Point(manual_type.Location.X, (extra.Location.Y + 18));
            multifile_software.Location = new Point(multifile_software.Location.X, (hasWiiU ? wiiu_display_l.Location.Y : extra.Location.Y) + (hasExtra || hasWiiU ? space : -1));
            extra.Visible = hasExtra;
        }
        #endregion

        private void CustomManual_CheckedChanged(object sender, EventArgs e)
        {
            if (manual_type.Enabled && manual_type.SelectedIndex == 2 && manual == null)
            {
                if (!Program.Config.application.donotshow_000) MessageBox.Show((sender as Control).Text, Program.Lang.Msg(6), 0);

                if (browseManual.ShowDialog() == DialogResult.OK) LoadManual(manual_type.SelectedIndex, browseManual.SelectedPath, true);
                else manual_type.SelectedIndex = 0;
            }

            if (manual_type.Enabled && manual_type.SelectedIndex < 2) LoadManual(manual_type.SelectedIndex);

            ValueChanged(sender, e);
        }

        private void include_patch_CheckedChanged(object sender, EventArgs e)
        {
            if (include_patch.Checked && patch == null)
            {
                if (browsePatch.ShowDialog() == DialogResult.OK)
                {
                    patch = browsePatch.FileName;
                }

                else patch = null;
            }

            else if (!include_patch.Checked)
                patch = null;
        }

        private void InjectorsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            resetImages();
            resetContentOptions();
            LoadImage();
            ValueChanged(sender, e);
        }

        /* private void banner_preview_Click(object sender, EventArgs e)
        {
            using (Form f = new Form())
            {
                f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                f.ShowInTaskbar = false;
                f.Text = Program.Lang.String("banner_preview", Name);
                f.Icon = Icon;

                var p = new PictureBox() { Name = "picture" };
                p.SizeMode = PictureBoxSizeMode.AutoSize;
                p.Location = new Point(0, 0);

                p.Image = preview.Banner
                (
                    img?.VCPic,
                    banner_form.title.Text,
                    (int)banner_form.released.Value,
                    (int)banner_form.players.Value,
                    targetPlatform,
                    _bannerRegion
                );

                f.ClientSize = p.Image.Size;
                f.StartPosition = FormStartPosition.CenterParent;
                f.Controls.Add(p);
                f.ShowDialog();
            }
        } */

        private void banner_Click(object sender, EventArgs e)
        {
            // banner_form.Text = Program.Lang.String(banner_details.Name, Name);
            banner_form.origTitle = banner_form.title.Text;
            banner_form.origYear = (int)banner_form.released.Value;
            banner_form.origPlayers = (int)banner_form.players.Value;
            banner_form.origRegion = banner_form.region.SelectedIndex;
            banner_form.origSound = banner_form.sound;

            if (banner_form.ShowDialog() == DialogResult.OK)
            {
                bool hasBanner = banner_form.origTitle != banner_form.title.Text;
                bool hasYear = banner_form.origYear != (int)banner_form.released.Value;
                bool hasPlayers = banner_form.origPlayers != (int)banner_form.players.Value;
                bool hasRegion = banner_form.origRegion != banner_form.region.SelectedIndex;
                bool hasSound = banner_form.origSound != banner_form.sound;
                linkSaveDataTitle();

                if (hasBanner || hasYear || hasPlayers || hasRegion)
                {
                    resetImages(true);
                    ValueChanged(sender, e);
                }
            }
        }

        private void edit_save_data_Click(object sender, EventArgs e)
        {
            // savedata.Text = Program.Lang.String(edit_save_data.Name, Name);

            if (savedata.ShowDialog() == DialogResult.OK) ValueChanged(sender, e);
        }

        private void injection_method_help_Click(object sender, EventArgs e) => htmlDialog.ShowDialog(this);
    }
}
