using System;
using System.Collections.Generic;

namespace FriishProduce
{
    public class DropboxDL
    {
        public string wTID { get; }
        public string wName { get; }
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
            new DropboxDL("HCME", "Kirby-TV-Channel-USA", "llfa8dejqgdudxp9t8buo", "mp0m84feovo6urr0vvmpxp2fa", "mp0m84feovo6urr0vvmpxp2fa&st=yznvdz92")
        };

        public DropboxDL(string fileName, string wadName, string fi, string rlKey, string st)
        {
            wTID = fileName;
            wName = wadName;
            Fi = fi;
            RlKey = rlKey;
            St = st;
        }

        public string GetUrlFor(string tID)
        {
            if (tID != wTID)
                return null;

            string baseUrl = "https://www.dropbox.com/scl/fi/";
            return $"{baseUrl}{Fi}/{wName}-{wTID}.wad?rlkey={RlKey}&st={St}&dl=1";
        }

        // Search a provided list
        public static string FindUrlFor(List<DropboxDL> list, string tID)
        {
            foreach (var dbp in list)
            {
                var url = dbp.GetUrlFor(tID);
                if (url != null)
                    return url;
            }
            return null;
        }

        // Search our internal list
        public static string FindUrlFor(string tID)
        {
            return FindUrlFor(dbParams, tID);
        }
    }
}