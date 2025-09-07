using System;

namespace FriishProduce.Injectors
{
    public class FlashBase
    {
        public static readonly FlashBase BackToNature = new(0, "content\\menu.swf");
        public static readonly FlashBase iPlayer = new(1, "trusted\\startup.swf");
        public static readonly FlashBase YouTube = new(2, "trusted\\wii_shim.swf");
        public static readonly FlashBase KirbyTV = new(3, "trusted\\Gumball2.12.RC-1.swf");
        public static readonly FlashBase Invalid = new(-1, "");

        public int FlBase { get; }
        public string Path { get; } // Windows path (double backslash)
        public string Domain { get; } // URL folder only forward slashes
        public string FullPath { get; } // URL full path forward slashes

        private FlashBase(int flBase, string path) {
            FlBase = flBase;
            Path = path;
            string forwardPath = path.Replace("\\", "/");
            int lastSlash = forwardPath.LastIndexOf('/');
            Domain = $"file:///{(lastSlash >= 0 ? forwardPath.Substring(0, lastSlash + 1) : "")}";
            FullPath = $"file:///{forwardPath}";
        }

        public static readonly FlashBase[] Bases = { BackToNature, iPlayer, YouTube, KirbyTV, Invalid };
    }
}