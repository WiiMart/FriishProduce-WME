using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FriishProduce.Injectors
{
    public class FlashBase
    {
        public static readonly FlashBase BackToNature = new(0, "content/menu.swf");
        public static readonly FlashBase iPlayer = new(1, "trusted/startup.swf");
        public static readonly FlashBase YouTube = new(2, "trusted/wii_shim.swf");
        public static readonly FlashBase KirbyTV = new(3, "trusted/Gumball2.12.RC-1.swf");
        public static readonly FlashBase Invalid = new(-1, "");

        public int FlBase { get; }
        public string ContentPath { get; }

        public string FullPath { get; }

        private FlashBase(int flBase, string contentPath)
        {
            FlBase = flBase;
            ContentPath = contentPath;
            FullPath = "file:///" + ContentPath;
        }
        
        public static readonly FlashBase[] Bases = { BackToNature, iPlayer, YouTube, KirbyTV, Invalid };
    }
}