using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FriishProduce.Injectors
{
    public class FlashBase
    {
        public static readonly FlashBase BackToNature = new(0, "file:///content/", "menu.swf");
        public static readonly FlashBase iPlayer = new(1, "startup.swf");
        public static readonly FlashBase YouTube = new(2, "wii_shim.swf");
        public static readonly FlashBase KirbyTV = new(3, "Gumball2.12.RC-1.swf");
        public static readonly FlashBase Invalid = new(-1, "", "");

        public int FlBase { get; }
        public string Domain { get; }
        public string Content { get; }
        public string FullPath { get; }

        private FlashBase(int flBase, string domain, string content) {
            FlBase = flBase;
            Domain = domain;
            Content = content;
            FullPath = Domain + Content;
        }

        private FlashBase(int flBase, string content) {
            // default to 'trusted' unless declared
            new FlashBase(flBase, "file:///trusted/", content);
        }
        
        public static readonly FlashBase[] Bases = { BackToNature, iPlayer, YouTube, KirbyTV, Invalid };
    }
}