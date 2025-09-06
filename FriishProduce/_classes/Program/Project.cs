using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using libWiiSharp;

namespace FriishProduce
{
    public class Project
    {
        public string ProjectPath { get; set; }

        public Platform Platform { get; set; }
        public string ROM { get; set; }

        [JsonConverter(typeof(DlBaseWadParser))]
        public (int BaseNumber, int Region) OnlineWAD { get; set; } = (0, 1); //base wad, region

        public string OfflineWAD { get; set; }
        public string Patch { get; set; }

        [JsonConverter(typeof(ManualParser))]
        public (int Type, string File) Manual { get; set; }

        [JsonConverter(typeof(BmpParser))]
        public (string File, System.Drawing.Bitmap Bmp) Img { get; set; }

        public string Sound { get; set; }

        public int InjectionMethod { get; set; }
        public int ForwarderStorageDevice { get; set; } = Program.Config.forwarder.root_storage_device;
        public bool IsMultifile { get; set; }

        public IDictionary<string, string> ContentOptions { get; set; }

        [JsonConverter(typeof(KeyParser))]
        public (bool Enabled, IDictionary<Buttons, string> List) Keymap { get; set; }

        public int WADRegion { get; set; } = 0;
        public bool LinkSaveDataTitle { get; set; } = Program.Config.application.auto_fill_save_data;

        [JsonConverter(typeof(ImgOptsParser))]
        public (int, bool) ImageOptions { get; set; } = (Program.Config.application.image_interpolation, Program.Config.application.image_fit_aspect_ratio);

        public int VideoMode { get; set; }
        public int WiiUDisplay { get; set; } = Program.Config.application.default_wiiu_display;

        public string TitleID { get; set; }

        public string Genre { get; set; } = "";

        public string[] ChannelTitles { get; set; }

        public Region BannerRegion { get; set; }
        public string BannerTitle { get; set; }
        public int BannerYear { get; set; } = 1980;
        public int BannerPlayers { get; set; } = 1;
        public string[] SaveDataTitle { get; set; } = new string[] { "" };

        /// <summary>
        ///     Checks if a project file is a legacy (binary serialized)
        ///         then checks file extension *if* file content cant be read
        /// </summary>
        public static bool IsLegacy(string projectPath) {
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                return false;
            try {
                using var fs = new FileStream(projectPath, FileMode.Open, FileAccess.Read);
                // Peek first few bytes
                byte[] buffer = new byte[4];
                int read = fs.Read(buffer, 0, buffer.Length);
                fs.Seek(0, SeekOrigin.Begin);
                // reset for StreamReader

                // Check BinaryFormatter magic header
                if (read >= 4 && buffer[0] == 0x00 && buffer[1] == 0x01 && buffer[2] == 0x00 && buffer[3] == 0x00)
                    return true;

                // else skip whitespace and check for JSON start
                using var reader = new StreamReader(fs, Encoding.UTF8, true);
                int ch;
                do {
                    ch = reader.Read();
                } while (ch != -1 && char.IsWhiteSpace((char)ch));

                return !(ch == -1 || ch == '{' || ch == '[');
            }
            catch {
                // check extension if file cant be read
                return Path.GetExtension(projectPath).Equals(".fppj", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        ///     Lazy overload that accepts a Project object and checks its ProjectPath
        /// </summary>
        public static bool IsLegacy(Project project) => project != null && IsLegacy(project.ProjectPath);

    }
    
    // Read and write downloadable base wad settings
    // (int BaseIdx, int Region)
    public class DlBaseWadParser : JsonConverter<(int BaseIdx, int Region)>
    {
        public override (int BaseIdx, int Region) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            return (
                root.GetProperty("BaseIdx").GetInt32(),
                root.GetProperty("Region").GetInt32()
            );
        }

        public override void Write(Utf8JsonWriter writer, (int BaseIdx, int Region) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("BaseIdx", value.BaseIdx);
            writer.WriteNumber("Region", value.Region);
            writer.WriteEndObject();
        }
    }

    // Read and write operations manual settings
    // (int Type, string File)
    public class ManualParser : JsonConverter<(int Type, string File)>
    {
        public override (int Type, string File) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            return (
                root.GetProperty("Type").GetInt32(),
                root.GetProperty("File").GetString()
            );
        }

        public override void Write(Utf8JsonWriter writer, (int Type, string File) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Type", value.Type);
            writer.WriteString("File", value.File);
            writer.WriteEndObject();
        }
    }

    // Read and write banner image path and/or bitmap
    // (string File, Bitmap Bmp[optional base64])
    public class BmpParser : JsonConverter<(string File, System.Drawing.Bitmap Bmp)>
    {
        public override (string File, System.Drawing.Bitmap Bmp) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            string file = root.GetProperty("File").GetString();
            string bmpBase64 = root.TryGetProperty("BmpBase64", out var bmpProp) ? bmpProp.GetString() : null;

            return (file, ImageHelper.BmpFromB64(bmpBase64));
        }

        public override void Write(Utf8JsonWriter writer, (string File, System.Drawing.Bitmap Bmp) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("File", value.File);
            if (value.Bmp != null)
                writer.WriteString("BmpBase64", ImageHelper.BmpToB64(value.Bmp));
            writer.WriteEndObject();
        }
    }

    // Read and write keymap.ini settings
    // (bool Enabled, IDictionary<Buttons, string> List)
    public class KeyParser : JsonConverter<(bool Enabled, IDictionary<Buttons, string> List)>
    {
        public override (bool Enabled, IDictionary<Buttons, string> List) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            bool enabled = root.GetProperty("Enabled").GetBoolean();
            var list = JsonSerializer.Deserialize<Dictionary<Buttons, string>>(root.GetProperty("List").GetRawText(), options);
            return (enabled, list);
        }

        public override void Write(Utf8JsonWriter writer, (bool Enabled, IDictionary<Buttons, string> List) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("Enabled", value.Enabled);
            writer.WritePropertyName("List");
            JsonSerializer.Serialize(writer, value.List, options);
            writer.WriteEndObject();
        }
    }

    // Read and write banner image size settings (stretch or fit)
    // (int, bool) ImageOptions
    public class ImgOptsParser : JsonConverter<(int, bool)>
    {
        public override (int, bool) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            return (
                root.GetProperty("Interpolation").GetInt32(),
                root.GetProperty("FitAspectRatio").GetBoolean()
            );
        }

        public override void Write(Utf8JsonWriter writer, (int, bool) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Interpolation", value.Item1);
            writer.WriteBoolean("FitAspectRatio", value.Item2);
            writer.WriteEndObject();
        }
    }
}