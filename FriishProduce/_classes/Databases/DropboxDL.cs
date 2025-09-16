using System;
using System.Collections.Generic;
using System.Linq;

namespace FriishProduce
{
    public class DropboxDL
    {
        public string TID { get; }
        public string Name { get; }
        public string Fi { get; }
        public string RlKey { get; }
        public string St { get; }

        private static readonly List<DropboxDL> dbParams = new()
        {
            // ascii TID, wad name, ("fi/" key), rlkey, st
            new DropboxDL("WZ1E", "Flash-Base-25", "ctk0te6jb4l7u84h4w47i", "73jg0glqjnsbmvhb2isqr84md", "76g3tytc"),
            new DropboxDL("WZ1P", "Flash-Base-25", "n48qzbfjn4h7haqzexk4b", "bv789t33hhopm1cldq4yma4tq", "a14t9ueg"),
            new DropboxDL("XZ1E", "Kirby-TV-Channel-Base-v4", "rlm60x81w011vyk8wl95q", "8qycrnvpicipnttorx4qi3co2", "wcjlreeg"),
            new DropboxDL("XZ1P", "Kirby-TV-Channel-Base-v4", "68yu4pn7ubftt2lruwfgc", "spmnc1kjhukcjg6oztxbcv90v", "nz1jpbjh"),
            new DropboxDL("HCME", "Kirby-TV-Channel-USA", "llfa8dejqgdudxp9t8buo", "mp0m84feovo6urr0vvmpxp2fa", "yznvdz92"),
            new DropboxDL("WNAE", "Flash-Placeholder-USA", "j4rvyxxbaatz1xa6oghwq", "pvawxkk3sd7jg35sqdtpdh7ta", "cqrem2a3"),
            new DropboxDL("FC9E", "Metal-Slader-Glory", "4d5v5sy3n8v04rndloy1z", "zsru8q4t1bcft75arxaab46lu", "nr6tdwcj"),
            new DropboxDL("FC9P", "Metal-Slader-Glory", "0blznhdr6ow7488kseekj", "ufitvehjte1hhnaom6t2fo69w", "7k5afk74")
        };

        public DropboxDL(string tid, string name, string fi, string rlKey, string st) {

            TID = tid;
            Name = name;
            Fi = fi;
            RlKey = rlKey;
            St = st;
        }

        public string BuildUrlFor(string tid) {
            return tid != TID ? null : "https://www.dropbox.com/scl/fi/" + $"{Fi}/{Name}-{TID}.wad?rlkey={RlKey}&st={St}&dl=1";
        }

        // Search provided list for TID match
        public static string FindUrlFor(List<DropboxDL> list, string tid) {
            return list.Select(dbp => dbp.BuildUrlFor(tid)).FirstOrDefault(url => url != null);
        }

        // Search our internal list for a TID match
        public static string FindUrlFor(string tid) {
            return FindUrlFor(dbParams, tid);
        }
    }
}