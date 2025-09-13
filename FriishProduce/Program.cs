using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace FriishProduce
{
    static class Program {
        [DllImport("user32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool turnOn);

        public static Settings Config { get; set; }
        public static MainForm MainForm { get; private set; }
        public static Language Lang { get; set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        public static bool DebugMode { get => Config?.application?.debug_mode ?? false; }
        public static bool GUI { get => MainForm != null; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Config = new(PathConstants.Config);
            bool invalidOs = Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0);
            bool runCLM = false;//System.Windows.Forms.MessageBox.Show("Do you want to run FriishProduce in command-line mode?", "Select Mode", 
                //System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question) == DialogResult.Yes;
            // Prompt to run in CL mode

            if (runCLM) {
                CLI.Run(args);
                return;
            }
            #if DEBUG
            AllocConsole();
            #endif

            if (invalidOs) {
                System.Windows.Forms.MessageBox.Show("To use this program, please upgrade to Windows 7 or a newer version of Windows.");
                Environment.Exit(-1);
                return;
            }
            else if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) {
                foreach (var Process in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
                    if (Process.Handle != Process.GetCurrentProcess().Handle && Process.MainWindowHandle != IntPtr.Zero) {
                        // System.Windows.Forms.MessageBox.Show("FriishProduce is already running.");
                        SwitchToThisWindow(Process.MainWindowHandle, true);
                        Environment.Exit(0);
                        return;
                    }
            }

            // **********************************************************************************
            Logger.Log("Opening FriishProduce-WME.");
            Lang = new Language();

            System.Globalization.CultureInfo cultureInfo = new(Lang.Current);
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;

            Logger.Log("Running in GUI (graphical) mode.");
            Theme.ChangeScheme(DebugMode ? Config.application.theme : -1);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            CleanTemp();
            MainForm = new MainForm(args);
            System.Windows.Forms.Application.Run(MainForm);
        }

        public static void CleanTemp() {
            if (!Directory.Exists(PathConstants.WorkingFolder))
                Directory.CreateDirectory(PathConstants.WorkingFolder);
            else {
                try {
                    foreach (var folder in Directory.GetDirectories(PathConstants.WorkingFolder))
                        Directory.Delete(folder, true);
                    Logger.INFO("Cleaned temporary files...");
                }
                catch {}
            }
            Injectors.C64.Clean();
        }
    }

    /// <summary>
    ///     Thread-safe logger for storing and printing timestamped messages</summary>
    public static class Logger {
        /// <summary>
        ///     Object used to lock access to the logs list and prevent message corruption/overlaps</summary>
        private static readonly object LockObj = new();
        /// <summary>
        ///     Stores all logged messages in order</summary>
        private static readonly List<string> Logs = new();

        /// <summary>
        ///     Severity levels for log messages</summary>
        public enum Level { INFO, WARN, ERROR, NONE }

        /// <summary>
        ///     Log a message with no level, just an ordinary timestamped message</summary>
        ///         This is primarily for leftover Log messages that need converting to use severity levels
        public static void Log(string msg, bool prntln = false) => Log(Level.NONE, msg, prntln);
        /// <summary>
        ///     Log multiple messages(string array) with no level</summary>
        public static void Log(bool prntln = false, params string[] msgs) => Array.ForEach(msgs, msg => Log(Level.NONE, msg));
        /// <summary>
        ///     Log multiple messages with a severity level</summary>
        public static void Log(Level level, bool prntln = false, params string[] msgs) => Array.ForEach(msgs, msg => Log(level, msg));

        // Specific severity level logging EOA methods
        public static void INFO(string msg, bool prntln = false) => Log(Level.INFO, prntln, msg);
        public static void INFO(bool prntln = false, params string[] msgs) => Log(Level.INFO, prntln, msgs);
        public static void INFO(params string[] msgs) => Log(Level.INFO, false, msgs);

        public static void WARN(string msg, bool prntln = false) => Log(Level.WARN, prntln, msg);
        public static void WARN(bool prntln = false, params string[] msgs) => Log(Level.WARN, prntln, msgs);
        public static void WARN(params string[] msgs) => Log(Level.WARN, false, msgs);

        public static void ERROR(string msg, bool prntln = false) => Log(Level.ERROR, prntln, msg);
        public static void ERROR(bool prntln = false, params string[] msgs) => Log(Level.ERROR, prntln, msgs);
        public static void ERROR(params string[] msgs) => Log(Level.ERROR, false, msgs);

        /// <summary>
        ///     Core log method that formats the message, and adds timestamps, and severity levels</summary>
        public static void Log(Level type, string msg, bool prntln = false) {
            lock (LockObj) {
                string fmtMsg = msg.TrimStart('\r', '\n').Replace("\r\n", "\n").Replace("\n", Environment.NewLine + "> ");
                Logs.Add($"[{DateTime.Now:hh:mtt}]{(type == Level.NONE ? "" : $"[{type}]")} {fmtMsg}");
                if (!Program.Config.application.log_info && type == Level.INFO) return;

                if (Program.DebugMode)
                    Console.WriteLine($"[{DateTime.Now:hh:mmtt}]{(type == Level.NONE ? "" : $"[{type}]")} {fmtMsg}");

                if (prntln) Console.WriteLine();
            }
        }
        // Prints an empty line to the console
        public static void Prnt() {
            lock (LockObj) Console.WriteLine();
        }

        /// <summary>
        ///     Print a single line to console starting with an angle bracket</summary>
        ///         Intended for continuations, but fine for logging general console messages too 
        public static void Sub(string msg) {
            string fmtMsg = msg.TrimStart('\r', '\n').Replace("\r\n", "\n").Replace("\n", Environment.NewLine + "> ");
            lock (LockObj) Console.WriteLine($"> {fmtMsg}");
        }
        public static void Sub(params string[] msgs) {
            lock (LockObj) Array.ForEach(msgs, msg => Sub(msg));
        }

        /// <summary>
        ///     Get a single log message by index</summary>
        public static string GetLog(int index) {
            lock (LockObj) return (index >= 0 && index < Logs.Count) ? Logs[index] : null;
        }
        /// <summary>
        ///     Get all logged messages as a single string</summary>
        public static string GetLogs() {
            lock (LockObj) return string.Join(Environment.NewLine, Logs);
        }
        /// <summary>
        ///     Get a 'to' and 'from' range of logged messages as a list</summary>
        public static List<string> GetLogs(int from, int to) {
            lock (LockObj) {
                from = Math.Max(0, from);
                to = Math.Min(Logs.Count, to);
                return from < to ? Logs.GetRange(from, to - from) : new List<string>();
            }
        }
    }

    static class CLI {
        [DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();
        private static int fieldTop;
        private static int input;

        private static Dictionary<string, string> fields = new() {
            { "Input ROM", null },
            { "ROM patch", null },
            { "Base WAD", null },
            { "Title ID", null },
            { "Channel name", null },
            { "Image", null },
            { "Banner sound", null }
        };

        public static void Run(string[] args) {
            AllocConsole();
            Console.Title = "FriishProduce-WME (CLI)";
            Console.Clear();

            Console.WriteLine("=== FriishProduce CLI ===");
            Console.WriteLine("Type 'set <field> <value>' to update, or 'exit' to quit.");
            Console.WriteLine();

            fieldTop = Console.CursorTop;
            DrawFields();

            input = fieldTop + fields.Count + 3;

            string buffer = "";
            while (true) {
                // draw input line
                Console.SetCursorPosition(0, input);
                Console.Write("> " + buffer + new string(' ', Console.WindowWidth - buffer.Length - 2));
                Console.SetCursorPosition(2 + buffer.Length, input);

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) {
                    string input = buffer.Trim();
                    buffer = "";

                    if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                        break;

                    var parts = input.Split(new[] { ' ' }, 3);
                    if (parts.Length == 3 && parts[0].Equals("set", StringComparison.OrdinalIgnoreCase)) {
                        string field = fields.Keys.FirstOrDefault(k => k.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
                        if (field != null) {
                            fields[field] = parts[2];
                            DrawFields();
                        }
                        else {
                            Console.SetCursorPosition(0, CLI.input + 1);
                            Console.Write(new string(' ', Console.WindowWidth)); // clear old message
                            Console.SetCursorPosition(0, CLI.input + 1);
                            Console.WriteLine("Unknown field.");
                        }
                    }
                }
                else if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
                    buffer = buffer.Substring(0, buffer.Length - 1);
                else if (!char.IsControl(key.KeyChar))
                    buffer += key.KeyChar;
            }
        }

        private static void DrawFields() {
            int top = fieldTop;
            Console.SetCursorPosition(0, top);

            foreach (var kvp in fields)
                Console.WriteLine($"> {kvp.Key}: {kvp.Value ?? "(none)"}");

            Console.WriteLine(); // blank line before prompt
            Console.WriteLine("Is this OK?");
            // Update input line
            input = top + fields.Count + 2;
        }
    }
}
