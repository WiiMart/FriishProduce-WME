using System;
using System.IO;

namespace FriishProduce
{
    public static class PathConstants
    {
        public static string EnvironmentFolder
        {
            get
            {
                string value = System.Windows.Forms.Application.StartupPath;
                if (!value.EndsWith("\\")) value += '\\';
                return value;
            }
        }
        public static readonly string Update = Path.Combine(EnvironmentFolder, "fpwme_latest.zip");
        public static readonly string WorkingFolder = Path.Combine(EnvironmentFolder, "resources\\temp\\");
        public static readonly string Banners = Path.Combine(EnvironmentFolder, "resources\\tools\\banners\\");

        // Application paths
        public static readonly string Config = Path.Combine(EnvironmentFolder, "FriishProduce.json");
        public static readonly string Log = Path.Combine(EnvironmentFolder, "log.txt");
        public static readonly string Databases = Path.Combine(EnvironmentFolder, "resources\\databases\\");
        public static readonly string Tools = Path.Combine(EnvironmentFolder, "resources\\tools\\");
        public static readonly string Languages = Path.Combine(EnvironmentFolder, "strings\\");
        public static readonly string Out = Path.Combine(EnvironmentFolder, "out\\");

        // Extracted paths
        public static readonly string WAD = WorkingFolder + "wad\\";
        public static readonly string Manual = WorkingFolder + "manual\\";
        public static readonly string DataCCF = WorkingFolder + "data_ccf\\";
        public static readonly string MiscCCF = DataCCF + "misc_ccf\\";

        // C64 paths
        public static readonly string Frodo = Tools + "c64\\frodosrc\\";
        public static readonly string FrodoSnapshot = Frodo + "ik.fss";
        public static readonly string FrodoRom = Frodo + "rom.d64";
        public static readonly string FrodoOutput = Frodo + "snap.fss";

        // Flash paths
        public static readonly string FlashContents = WorkingFolder + "flash\\";
        public static readonly string FlashSWF = FlashContents + "content\\menu.swf";
        public static readonly string FlashConfig = FlashContents + "config\\config.common.pcf";
        public static readonly string FlashHBM = WorkingFolder + "flash_manual\\";
        public static readonly string FlashManualTempPath = Path.Combine(Path.GetTempPath(), "Friish_FlashManualImport_temp.jpg");

        // Forwarder paths
        public static readonly string SDUSBRoot = WorkingFolder + "root\\";
        public static readonly string BIOSFiles = Path.Combine(EnvironmentFolder, "resources\\bios\\");

        // Other
        public static readonly string PatchedSuffix = "-patched";

        public static readonly string DefaultLocSavePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads"));
        public static readonly string RomhackPath = Path.GetFullPath(Path.Combine(DefaultLocSavePath, "Romhacking"));
        public static readonly string DefaultLocSaveBanners = Path.GetFullPath(Path.Combine(DefaultLocSavePath, "Banners"));
        public static readonly string DefaultLocSaveWADs = Path.GetFullPath(Path.Combine(DefaultLocSavePath, "WADs"));
        public static readonly string RhTransPath = Path.GetFullPath(Path.Combine(RomhackPath, "Translations"));
        public static readonly string RhHacksPath = Path.GetFullPath(Path.Combine(RomhackPath, "Hacks"));
    }
}
