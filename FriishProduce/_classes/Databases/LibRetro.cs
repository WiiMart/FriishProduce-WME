using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Net;
using System.Text;

namespace FriishProduce.Databases
{
    public static class LibRetro
    {
        #region -- PRIVATE VARIABLES --
        private static Platform platform = Platform.NES;

        private static readonly string[] DevRedump = { "developer", "redump" };
        private static readonly string[] DefaultFolders = { "maxusers", "releaseyear", "genre" };

        private class PlatformInfo {
            public Platform Platform { get; }
            public string[] Folders { get; }

            public PlatformInfo(Platform platform, string[] folders) {
                Platform = platform;
                Folders = folders;
            }
        }

        private static readonly Dictionary<string, PlatformInfo> list = new()
        {
            { "Nintendo - Nintendo Entertainment System", new PlatformInfo(Platform.NES, DefaultFolders) },
            { "Nintendo - Super Nintendo Entertainment System", new PlatformInfo(Platform.SNES, DefaultFolders) },
            { "Nintendo - Nintendo 64", new PlatformInfo(Platform.N64, DefaultFolders) },
            { "Sega - Master System - Mark III", new PlatformInfo(Platform.SMS, DefaultFolders) },
            { "Sega - Mega Drive - Genesis", new PlatformInfo(Platform.SMD, DefaultFolders) },
            { "NEC - PC Engine - TurboGrafx 16", new PlatformInfo(Platform.PCE, DevRedump) },
            { "NEC - PC Engine SuperGrafx", new PlatformInfo(Platform.PCE, DefaultFolders) },
            { "NEC - PC Engine CD - TurboGrafx-CD", new PlatformInfo(Platform.PCECD, DevRedump) },
            { "MAME", new PlatformInfo(Platform.NEO, DefaultFolders) },
            { "Commodore - 64", new PlatformInfo(Platform.C64, new[] { "no-intro" }) },
            { "Microsoft - MSX", new PlatformInfo(Platform.MSX, DefaultFolders) },
            { "Microsoft - MSX2", new PlatformInfo(Platform.MSX, DefaultFolders) },
            { "Microsoft - MSX 2", new PlatformInfo(Platform.MSX, DefaultFolders) },
            { "Nintendo - Game Boy", new PlatformInfo(Platform.GB, DefaultFolders) },
            { "Nintendo - Game Boy Color", new PlatformInfo(Platform.GBC, DefaultFolders) },
            { "Nintendo - Game Boy Advance", new PlatformInfo(Platform.GBA, DefaultFolders) },
            { "Nintendo - GameCube", new PlatformInfo(Platform.GCN, Array.Empty<string>()) },
            { "Sega - 32X", new PlatformInfo(Platform.S32X, DefaultFolders) },
            { "Sega - Mega-CD - Sega CD", new PlatformInfo(Platform.SMCD, DevRedump) },
            { "Sony - PlayStation", new PlatformInfo(Platform.PSX, DevRedump) },
        };

        private static string db_name => list.FirstOrDefault(item => item.Value.Platform == platform).Key;
        private static string db_crc(string input) => input.Replace("-", "").Trim().Substring(0, 8).ToUpper();

        private static string db_url(int i) {
            string db_base = "https://raw.githubusercontent.com/libretro/libretro-database/refs/heads/master/metadat/";
            if (!list.TryGetValue(db_name, out var info)) return null;
            string rootDat = db_base.Replace("metadat", "dat") + $"{db_name}.dat";
            return info.Folders.Length == 0 ? rootDat : i < info.Folders.Length ? db_base + info.Folders[i] + $"/{db_name}.dat" : null;
        }

        private static string db_img(string name, int source = 0) {
            string dbUri = Uri.EscapeUriString(db_name);
            string imgUri = Uri.EscapeUriString(name.Replace('/', '_').Replace('&', '_')) + ".png";
            string archiveorg = "https://archive.org/download/No-Intro_Thumbnails_2016-04-10/" + dbUri + ".zip/" + dbUri + "/Named_Titles/" + imgUri;
            string libretro = "https://thumbnails.libretro.com/" + dbUri + "/Named_Titles/" + imgUri;
            return source == 1 ? archiveorg : libretro;
        }
        #endregion

        public static bool Exists(Platform In) => list.Any(item => item.Value.Platform == In);

        public static bool IsWeb(Platform In) {
            if (File.Exists(Path.Combine(PathConstants.Databases, In.ToString().ToLower() + ".xml")))
                return false;

            return Enumerable.Range(0, int.MaxValue).Select(i => db_url(i))
                .TakeWhile(url => !string.IsNullOrWhiteSpace(url)).Any(url => !File.Exists(Path.Combine(PathConstants.Databases, Path.GetFileName(url))));
        }

        public static DataTable Parse(Platform In) {
        Top:
            platform = In;
            DataTable dt = new DataTable(platform.ToString().ToLower());

            // Always create all expected columns first
            string[] columns = { "crc", "name", "serial", "releaseyear", "users", "image", "db_genre" };
            foreach (var col in columns) {
                if (!dt.Columns.Contains(col))
                    dt.Columns.Add(col, typeof(string));
            }

            string path = Path.Combine(PathConstants.Databases, In.ToString().ToLower() + ".xml");

            if (File.Exists(path)) {
                try { dt.ReadXml(path); }
                catch { try { File.Delete(path); } catch { } goto Top; }
            }
            else {
                if (!Directory.Exists(PathConstants.Databases)) Directory.CreateDirectory(PathConstants.Databases);

                string crc = "";
                string name = "";
                string serial = "";
                string releaseyear = "";
                string users = "";
                string image = "";
                string db_genre = "";

                // Retrieve database from URL or file
                // ****************
                List<string[]> db_lines = new();

                for (int i = 0; ; i++)
                {
                    string url = db_url(i);
                    if (string.IsNullOrWhiteSpace(url))
                        break;

                    // prevent maxusers, releaseyear, genre from overwriting by appending db_name each other
                    Uri uri = new Uri(url);
                    string[] segments = uri.Segments;
                    string dbFolder = segments.Length >= 2 ? segments[segments.Length - 2].TrimEnd('/') : "unknown";
                    string localPath = Path.Combine(PathConstants.Databases, $"{dbFolder}_{db_name}.dat");

                    if (!File.Exists(localPath) && IsWeb(In)) {
                        try {
                            string text = Encoding.UTF8.GetString(Web.Get(url));
                            File.WriteAllText(localPath, text);
                        }
                        catch {/*ignore fetch errors*/}
                    }

                    if (File.Exists(localPath))
                        db_lines.Add(File.ReadAllLines(localPath));
                }

                if (db_lines.Count == 0) return null;

                // Scan retrieved database for CRC32 hashes, and add to data table
                // Also add release year, players and others
                // ****************
                for (int x = 0; x < db_lines.Count; x++)
                {
                    for (int y = 0; y < db_lines[x].Length; y++)
                    {
                        string line = db_lines[x][y].TrimStart(' ', '\t');

                        bool nameOrComment = (line.Contains("name \"") || line.Contains("comment \"")) && !line.Contains("rom (") && !line.Contains(db_name);
                        name = nameOrComment ? line.Replace("name \"", "").Replace("comment \"", "").TrimEnd('\"') : name;
                        image = nameOrComment ? db_img(name) : image;

                        serial = line.Contains("serial ") ? line.Substring(line.IndexOf("serial ") + 7) : serial;

                        string date = line.Contains("year ") ? line.Replace("\"", null).Substring(line.IndexOf("year ") + 5, 4) : null;
                        releaseyear = line.Contains("year ") ? (!int.TryParse(date, out int _) ? null : date) : releaseyear;

                        string userCount = line.Contains("users ") ? line.Substring(line.IndexOf("users ") + 6) : null;
                        users = line.Contains("users ") ? (!int.TryParse(userCount, out int _) ? null : userCount) : users;

                        int startIndex = line.Contains("genre ") ? line.IndexOf("genre ") + 6 : 0;
                        db_genre = line.Contains("genre ") ? (startIndex < line.Length ? line.Substring(startIndex).Replace("\"", "").Trim() : db_genre) : db_genre;

                        crc = line.Contains("crc ") ? db_crc(line.Substring(line.IndexOf("crc ") + 4)) : crc;

                        if (line == ")" && !string.IsNullOrEmpty(crc)) {
                            var rows = dt.Select($"crc = '{crc}'");
                            if (rows?.Length > 0) {
                                var row = rows[0];
                                if (!string.IsNullOrWhiteSpace(name)) row["name"] = name;
                                if (!string.IsNullOrWhiteSpace(serial)) row["serial"] = serial;
                                if (!string.IsNullOrWhiteSpace(releaseyear)) row["releaseyear"] = releaseyear;
                                if (!string.IsNullOrWhiteSpace(users)) row["users"] = users;
                                if (!string.IsNullOrWhiteSpace(image)) row["image"] = image;
                                if (!string.IsNullOrWhiteSpace(db_genre)) row["db_genre"] = db_genre;
                            }
                            else // If the row doesn't exist yet (very first dat for this CRC), add it
                                dt.Rows.Add(crc ?? "", name ?? "", serial ?? "", releaseyear ?? "", users ?? "", image ?? "", db_genre ?? "");

                            crc = name = serial = releaseyear = users = image = db_genre = null;
                        }
                    }
                }
                using DataView dv = dt.DefaultView;
                dv.Sort = "name";
                dt = dv.ToTable();
                dt.WriteXml(path, XmlWriteMode.WriteSchema);
            }
            return dt;
        }

        public static (string Name, string Serial, string Year, string Players, string Image, string Genre, bool Complete) Read(string file, Platform platform)
        {
            string crc32 = null;
            DataTable dt = Parse(platform);

            if (dt != null) {
                using (FileStream fileStream = File.OpenRead(file)) {
                    var crc = new Crc32();
                    crc.Append(fileStream);
                    var hash_array = crc.GetCurrentHash();
                    Array.Reverse(hash_array);
                    crc32 = db_crc(BitConverter.ToString(hash_array));
                }

                var rows = dt.Select($"crc = '{crc32}'");
                if (rows?.Length > 0) {
                    var row = rows[0];

                    // read columns by name, more stable and reliable than indexes...
                    string name = row.Table.Columns.Contains("name") ? row["name"]?.ToString() : null;
                    string serial = row.Table.Columns.Contains("serial") ? row["serial"]?.ToString() : null;
                    string year = row.Table.Columns.Contains("releaseyear") ? row["releaseyear"]?.ToString() : null;
                    string players = row.Table.Columns.Contains("users") ? row["users"]?.ToString() : null;
                    string image = row.Table.Columns.Contains("image") ? row["image"]?.ToString() : null;
                    string db_genre = row.Table.Columns.Contains("db_genre") ? row["db_genre"]?.ToString() : null;
                    string locsaveDir = Program.Config.application.locsave_banner_tb;
                    string bannersDir = string.IsNullOrEmpty(locsaveDir) ? PathConstants.DefaultLocSaveBanners : locsaveDir;

                    // Check for and use local banner if it exists else verify url
                    if (!string.IsNullOrEmpty(image)) {
                        string localPath = Path.Combine(bannersDir, Path.GetFileName(new Uri(image).LocalPath));

                        if (File.Exists(localPath)) {
                            Logger.Log($"Using existing local banner:\n{localPath}");
                            image = localPath;
                        } 
                        else if (!Web.CheckHttp(image, null)) {
                            Logger.Log($"Failed to fetch banner image for:\n{image}");
                            image = null;
                        }
                    }
                    if (string.IsNullOrEmpty(image) && !string.IsNullOrEmpty(name)) {
                        string[] imgdbs = { db_img(name, 0), db_img(name, 1) };
                        foreach (string imgdb in imgdbs) {
                            try {
                                image = imgdb;
                                if (row.Table.Columns.Contains("image"))
                                    row["image"] = image;
                                break;
                            }
                            catch (Exception ex) {
                                Logger.Log($"Failed to retrieve {imgdb}: {ex.Message}");
                            }
                        }
                        if (string.IsNullOrEmpty(image) && row.Table.Columns.Contains("image"))
                            row["image"] = null;
                    }

                    bool complete = !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(players) && !string.IsNullOrEmpty(year) && !string.IsNullOrEmpty(image);
                    if (platform == Platform.C64 || platform == Platform.PCECD)
                        complete = !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(image);

                    return (name, serial, year, players, image, db_genre, complete);
                }
            }

            return (null, null, null, null, null, null, false);
        }
    }
}