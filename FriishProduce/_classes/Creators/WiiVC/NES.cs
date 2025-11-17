using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FriishProduce.Injectors
{
    public class NES : InjectorWiiVC
    {
        public int[] saveTPL_offsets { get; set; }

        protected override void Load()
        {
            needsMainDol = true;
            needsManualLoaded = true;
            SaveTextEncoding = Encoding.BigEndianUnicode;
            base.Load();
        }

        /// <summary>
        ///     Inserts ROM into main.dol.
        ///         For NES, this means 'flashing' the ROM by replacing the entire byte array and padding.
        /// </summary>
        protected override void ReplaceROM(ChannelDatabase.ChannelEntry entry = null) {
            Logger.Log("ReplaceROM called for NES inject, attempting to flash ROM inside main.dol");

            // -----------------------
            // Header scan
            // -----------------------
            int headOfs = -1;
            for (int idx = 0; idx < Contents[1].Length - 4; idx++) {
                if (Contents[1][idx] == 0x4E && Contents[1][idx + 1] == 0x45 && Contents[1][idx + 2] == 0x53 && Contents[1][idx + 3] == 0x1A) {
                    headOfs = idx;
                    break;
                }
            }
            if (headOfs == -1) {
                Logger.ERROR("No primary or secondary header signatures could be resolved.");
                throw new Exception(Program.Lang.Msg(2, 1));
            }
            Logger.Log($"Found NES header at offset 0x{headOfs:X}");

            // -----------------------
            // Get PRG/CHR info from header and calc MaxSize
            // -----------------------
            int prgBanks = Contents[1][headOfs + 4];
            int chrBanks = Contents[1][headOfs + 5];
            if ((Contents[1][headOfs + 7] & 0x0C) == 0x08 && headOfs + 9 < Contents[1].Length) {
                // NES2.0 header check and realloc
                int prgMSB = Contents[1][headOfs + 9] & 0x0F;
                int chrMSB = (Contents[1][headOfs + 9] >> 4) & 0x0F;
                prgBanks = (prgMSB << 8) | prgBanks;
                chrBanks = (chrMSB << 8) | chrBanks;
            }
            int PRG = 16384 * prgBanks;
            int CHR = 8192 * chrBanks;
            ROM.MaxSize = PRG + CHR + 16;

            Logger.Log($"PRG:[{PRG}], CHR:[{CHR}], Total Size:[{ROM.MaxSize}], New ROM:[{ROM.Bytes.Length}]");

            // -----------------------
            // Parse the DOL header to get the entire section the ROM is contained within
            // -----------------------
            byte[] targetROM = new byte[ROM.Bytes.Length];
            Array.Copy(ROM.Bytes, targetROM, ROM.Bytes.Length);
            var (flOffsets, sizes, loadAddrs) = DolUtils.ParseHeader(Contents[1]);

            // Locate ROM section using file offsets in DOL header
            int romSectIdx = -1;
            for (int idx = 0; idx < flOffsets.Length; idx++) {
                if (flOffsets[idx] != 0 && sizes[idx] > 0 &&
                    headOfs >= flOffsets[idx] && headOfs < flOffsets[idx] + sizes[idx]) {
                    romSectIdx = idx;
                    break;
                }
            }
            if (romSectIdx < 0) {
                string sectInf = $" (ofs:[{headOfs}], rsi:[{romSectIdx}], eof:[{Contents[1].Length}])";
                Logger.ERROR($"Could not locate ROM section inside DOL!{sectInf}");
                MessageBox.Show(Program.Lang.Msg(16));
                throw new Exception(Program.Lang.Msg(16) + $"{sectInf}");
            }

            // Calculate allocated section bounds
            int sectStart = flOffsets[romSectIdx];
            int sectEnd = DolUtils.GetSectEndFor(Contents[1], sectStart);
            int alloc = sectEnd - sectStart;
            int romEnd = headOfs + targetROM.Length;
            int remain = sectEnd - romEnd;
            Logger.Log($"ROM is in section {romSectIdx} (fileOfs:[0x{sectStart:X}], size:[0x{sizes[romSectIdx]:X}], load:[0x{loadAddrs[romSectIdx]:X}])");
            Logger.Log($"Alloc Size:[{alloc}], New ROM Size:[{targetROM.Length}], ROM End:[0x{romEnd:X}], Sect End:[0x{sectEnd:X}], Remaining:[{remain}]");

            // If ROM fits, overwrite the raw byte array inside the section,
            //      this should hopefully prevent OOB inconsistencies experienced before due to only using PRG and CHR bank sizes
            if (romEnd <= sectEnd) {
                Array.Copy(targetROM, 0, Contents[1], headOfs, targetROM.Length);
                Logger.Log("ROM fits within existing allocated section. Expansion skipped.");
                return; // return and DO NOT touch header/offsets/sizes
            }
            // else expand section for ROM
            Logger.Log();
            Logger.WARN("ROM does NOT fit in allocated section — performing expansion from ROM end.",
                "This will shift all data after original ROM end.", "THIS IS HIGHLY EXPERIMENTAL, AND UNLIKELY TO WORK.");
            Contents[1] = ExpandRomSection(Contents[1], romSectIdx, headOfs, targetROM, flOffsets, sizes, loadAddrs);
        }

        /// <summary>
        ///     Expand the DOL to hold a larger ROM, and shifts everything AFTER the ROM
        ///         Updates section sizes, shifts later sections, patches size references, ROM itself stays at headerOffset (no relocation)
        /// </summary>
        private byte[] ExpandRomSection(byte[] dol, int romSectIdx, int headOfs, byte[] targetROM, int[] fileOfs, int[] sizes, uint[] loadAddrs) {
            // TODO ensure calculation of PRG and CHR
            int sectStart = fileOfs[romSectIdx];
            int sectSize = sizes[romSectIdx];
            int sectEnd = sectStart + sectSize;

            // bytes between section start and the ROM header (MUST be preserved)
            int startLen = headOfs - sectStart;
            if (startLen < 0) throw new InvalidOperationException("headerOffset is before section start!");

            // recorded payload in section after preRomLen, help calculate prev ROM length inside the section
            int recPayload = Math.Max(0, sectSize - startLen);

            // oldRomLen (the ROM typically has padding/code before and after)
            int oldRomLen = Math.Min(recPayload, Math.Max(0, dol.Length - headOfs));

            // ROM section tail, bytes that were in the section after the old ROM
            int sectTailLen = Math.Max(0, recPayload - oldRomLen);

            Logger.Log($"Start:[0x{sectStart:X}], Head Ofs:[0x{headOfs:X}], End:[0x{sectEnd:X}]");
            Logger.Log($"Start Len(preROM):[0x{startLen:X}], Rec Payload:[0x{recPayload:X}], Prev ROM Len:[0x{oldRomLen:X}], Sect Tail:[0x{sectTailLen:X}]");
            Logger.Log();

            int newRomLen = targetROM.Length;
            // how many bytes later sections must be shifted
            int shift = Math.Max(0, newRomLen - oldRomLen);

            Logger.Log($"EXPANDING ROM SECTION {romSectIdx}",
                $"fileOfs:[0x{sectStart:X}], sectEnd:[0x{sectEnd:X}], newRomLen:[0x{newRomLen:X}], shift:[0x{shift:X}]");

            byte[] shifted = new byte[dol.Length + shift];

            // copy everything up to headerOffset (preserve pre-ROM bytes inside section)
            Array.Copy(dol, 0, shifted, 0, headOfs);

            // copy the new ROM at the SAME headerOffset (we do not relocate ROM start)
            Array.Copy(targetROM, 0, shifted, headOfs, newRomLen);

            // copy the in-section tail (preserve bytes that lived between old ROM end and section end)
            if (sectTailLen > 0) {
                int oldTailSrc = headOfs + oldRomLen;
                int newTailDest = headOfs + newRomLen;
                // bounds check
                if (oldTailSrc + sectTailLen <= dol.Length && newTailDest + sectTailLen <= shifted.Length) {
                    Array.Copy(dol, oldTailSrc, shifted, newTailDest, sectTailLen);
                } else {
                    Logger.ERROR($"ROM section tail copy OOB, oldTailSrc:[0x{oldTailSrc:X}]"
                        + $"len:[0x{sectTailLen:X}] dolLen:[0x{dol.Length:X}] newTailDest:[0x{newTailDest:X}] shiftedLen:[0x{shifted.Length:X}]");
                    throw new InvalidOperationException("Section tail copy bounds failure");
                }
            }

            // copy everything after original section end to its shifted location
            int remSrc = sectEnd;
            int remLen = Math.Max(0, dol.Length - remSrc);
            int remDest = remSrc + shift;
            if (remLen > 0) {
                if (remSrc + remLen <= dol.Length && remDest + remLen <= shifted.Length)
                    Array.Copy(dol, remSrc, shifted, remDest, remLen);
                else {
                    Logger.ERROR($"After-section tail copy out-of-bounds src:[0x{remSrc:X}]" 
                        + $"len:[0x{remLen:X}] dolLen:[0x{dol.Length:X}] dest:[0x{remDest:X}] shiftedLen:[0x{shifted.Length:X}]");
                    throw new InvalidOperationException("DOL remainder copy bounds failure");
                }
            }

            // update file offsets for sections AFTER the original section
            for (int idx = 0; idx < fileOfs.Length; idx++) {
                if (fileOfs[idx] != 0 && fileOfs[idx] >= sectEnd)
                    fileOfs[idx] += shift;
                // write file offset into DOL header (file offsets table starts at 0x00)
                DolUtils.WriteU32BE(shifted, idx * 4, (uint)fileOfs[idx]);
            }

            // update size for ROM section in the DOL header (sect start + ROM + sect tail)
            sizes[romSectIdx] = startLen + newRomLen + sectTailLen;
            int sizesBase = 0x90;
            for (int s = 0; s < sizes.Length; s++)
                DolUtils.WriteU32BE(shifted, sizesBase + s * 4, (uint)sizes[s]);

            // update any remaining size refs
            DolUtils.PatchSizeRefs(shifted, (uint)oldRomLen, (uint)newRomLen);
            Logger.Log("Succesfully expanded ROM section in DOL!",
                $"Prev ROM Len:[0x{oldRomLen:X}], New ROM Len:[0x{newRomLen:X}], New DOL Len:[0x{shifted.Length:X}]");

            // Patch VA references — only patch VAs that belong to valid load sections.
            DolUtils.PatchVARefs(shifted, shift);
            return shifted;
        }

        protected override void ModifyEmulatorSettings() =>
            InsertPalette(int.Parse(Settings.ElementAt(0).Value));

        /// <summary>
        /// Inserts palette into main.dol.
        /// </summary>
        public void InsertPalette(int index)
        {
            int offset = 0;

            string pal = null;
            switch (index)
            {
                case 0:
                    return;
                case 1:
                    pal = "B5 AD 80 13 84 11 9C 0F AC 0D B4 00 AC 00 9C 60 90 E0 81 02 81 00 80 E2 80 AC 80 00 80 00 80 00 D2 94 81 17 A0 1E B4 1A C4 16 CC 0A CC A0 C1 00 AD A0 92 00 89 E0 81 E9 81 B0 88 42 80 00 80 00 FF FF B6 9F C5 FF DD DF F5 DF FD B7 FE 2D EA 89 DA 87 C3 00 AB 28 A3 30 AB 39 A9 4A 80 00 80 00 FF FF E7 9F E7 3F EF 3F F7 1F FF 3C FF 59 EF 36 EF 94 EB B6 DF B6 DB 97 D3 59 EF 7B 80 00 80 00";
                    break;
                case 2:
                    pal = "B9 CE 90 71 80 15 A0 13 C4 0E D4 02 D0 00 BC 20 A0 A0 81 00 81 40 80 E2 8C EB 80 00 80 00 80 00 DE F7 81 DD 90 FD C0 1E DC 17 F0 0B EC A0 E5 21 C5 C0 82 40 82 A0 82 47 82 11 88 42 80 00 80 00 FF FF 9E FF AE 5F D2 3F F9 FF FD D6 FD CC FE 67 FA E7 C3 42 A7 69 AF F3 83 BB 9C E7 80 00 80 00 FF FF D7 9F E3 5F EB 3F FF 1F FF 1B FE F6 FF 75 FF 94 F3 F4 D7 D7 DB F9 CF FE C6 31 80 00 80 00";
                    break;
                case 3:
                    pal = "B1 8C 80 11 8C 33 98 4F A8 4C AC 02 A8 20 9C 81 90 C1 85 01 89 02 80 E3 80 AA 80 00 80 00 80 00 D6 B5 85 38 A4 9B B4 59 C8 55 CC 69 C8 C0 B9 40 AD A2 89 E2 8A 01 89 C9 8D 92 80 00 80 00 80 00 FF FF B2 7F C5 FF D9 BF ED BE F1 D5 F2 0B E6 64 D6 C0 BB 00 AF 29 9B 11 A6 F9 A1 08 80 00 80 00 FF FF DF 5F E7 3F EF 1F F7 1F FF 1C FB 38 F3 34 EF 73 E7 93 DF 97 DB B9 DB 9D D6 B5 80 00 80 00";
                    break;
                case 4:
                    pal = "B5 AD 94 53 94 35 AC 72 C4 8E CC 66 C8 C1 B9 00 A1 40 9D 81 9D 81 99 68 99 2E 80 00 80 00 80 00 D2 94 9D 5A 98 DB BC D9 DC D4 E0 CA E1 00 D1 60 BD E0 A2 20 A2 41 A2 2B 99 F2 B5 AD 80 00 80 00 EF 7B AE 3B A1 DB C5 9B E5 5B ED 52 ED A6 EE 20 E2 C3 C7 00 B3 27 AF 10 AA D7 D2 94 80 00 80 00 EF 7B C2 FB C6 9B D6 7B E2 7B EE 78 EE B3 EF 10 EF 2F DB 4E C7 51 C7 76 C7 79 EF 7B 80 00 80 00";
                    break;
                case 5:
                    pal = "B1 8C 80 B1 88 54 9C 14 AC 0F B4 08 B4 00 A8 60 98 C0 85 20 81 40 81 21 81 09 80 00 80 00 80 00 D6 B5 89 7B A1 1F B8 9F D0 79 D8 6F D8 C4 CD 20 B5 A0 9E 00 86 40 82 26 81 F1 80 00 80 00 80 00 FF FF B2 DF CA 5F E1 DF F9 BF FD B9 FE 0E F6 64 DE E0 C7 60 AF 86 A3 90 A7 3B A5 29 80 00 80 00 FF FF E3 7F EB 5F F7 3F FF 1F FF 1D FF 38 FB 74 F3 92 E7 B2 DF D5 DB D9 DB BE DE F7 80 00 80 00";
                    break;
                case 6:
                    pal = "B5 AD 80 92 80 1B B5 3B C8 0D D8 0D D8 80 C9 20 B5 20 91 20 81 A4 82 40 81 29 80 00 80 00 80 00 DA D6 81 BB 81 3F C8 1F D8 1F FC 12 FC 00 ED A0 C9 A0 92 40 82 40 82 CD 82 52 90 84 80 00 80 00 FF FF B6 DF CA 5F ED BF FC 1F FD BF FE 40 FE C0 EF 60 B7 60 83 E0 A7 FB 83 FF A5 29 80 00 80 00 FF FF DB 7F EE DF FE DF FE 5F FE D6 FF 72 FF E9 FF ED DB E9 CB ED A7 FB CB 7F CA 52 80 00 80 00";
                    break;
                case 7:
                    pal = "B9 CE 90 71 80 15 A0 13 C4 0E D4 02 D0 00 BC 20 A0 A0 81 00 81 40 80 E2 8C EB 80 00 80 00 80 00 DE F7 81 DD 90 FD C0 1E DC 17 F0 0B EC A0 E5 21 C5 C0 82 40 82 A0 82 47 82 11 80 00 80 00 80 00 FF FF 9E FF AE 5F E6 3F F9 FF FD D6 FD CC FE 67 FA E7 C3 42 A7 69 AF F3 83 BB BD EF 80 00 80 00 FF FF D7 9F E3 5F EB 3F FF 1F FF 1B FE F6 FF 75 FF 94 F3 F4 D7 D7 DB F9 CF FE E3 18 80 00 80 00";
                    break;
                case 8:
                    pal = "B5 AD 80 71 90 13 A0 11 B0 0C B0 03 AC 20 A4 40 94 C0 81 00 81 21 81 03 80 CB 80 00 80 00 80 00 DA D6 89 5A A0 DD B8 9B CC 77 D4 2C D4 A0 C5 20 B1 A0 92 00 82 20 82 08 81 D2 80 00 80 00 80 00 FF FF B2 DF BE 7F E1 FF F5 DF F9 D9 FA 2D EE 85 DE E1 C7 41 AF 67 A7 70 A7 3A A9 4A 80 00 80 00 FF FF DF 7F EB 5F F3 3F F7 1F FF 1C FF 38 FB 75 F7 94 EB B4 DF D6 DB D9 DB BE DE F7 80 00 80 00";
                    break;
                case 9:
                    pal = "B1 8C 80 4F 8C 11 98 10 A8 0B AC 03 A4 00 9C 60 8C C0 80 E0 81 00 80 E2 80 AA 80 00 80 00 80 00 D6 B5 8D 39 A0 BC B4 7A C8 75 CC 6B CC C0 BD 20 AD 80 91 E0 82 00 81 E7 81 B1 80 00 80 00 80 00 FF FF B2 BF C6 3F D9 DF F1 BF F5 B8 FA 0D EE 65 DE C1 C3 21 AF 47 A7 4F A7 19 A5 29 80 00 80 00 FF FF E3 9F EF 7F F7 5F FF 3F FF 3E FF 59 FF 76 F7 B4 EB D4 E3 F7 DF DA DF DE DE F7 80 00 80 00";
                    break;
            }

            if (pal != null)
            {
                // Search for palette header identifier
                for (int i = 0; i < Contents[1].Length; i++)
                {
                    if (Contents[1][i] == 0x42
                     && Contents[1][i + 1] == 0x59
                     && Contents[1][i + 2] == 0x21
                     && Contents[1][i + 3] == 0xC8
                     && Contents[1][i + 4] == 0x0D
                     && Contents[1][i + 5] == 0x53
                     && Contents[1][i + 6] == 0x41
                     && Contents[1][i + 7] == 0x54
                     && Contents[1][i + 8] == 0x00
                     && Contents[1][i + 9] == 0x00
                     && Contents[1][i + 10] == 0x00
                     && Contents[1][i + 11] == 0x00
                     && Contents[1][i + 12] == 0x00
                     && Contents[1][i + 13] == 0x00
                     && Contents[1][i + 14] == 0x00
                     && Contents[1][i + 15] == 0x00)
                    {
                        offset = i + 16;
                        break;
                    }
                }

                if (offset != 0)
                {
                    // Convert palette to bytes
                    var palBytes = new byte[128];
                    var palStringArray = pal.Split(' ');
                    for (int i = 0; i < 128; i++)
                        palBytes[i] = Convert.ToByte(palStringArray[i], 16);

                    palBytes.CopyTo(Contents[1], offset);
                }
            }
        }

        // ---------- SAVEDATA-RELATED FUNCTIONS ---------- //

        /// <summary>
        /// Searches for offsets of the TPL embedded within main.dol so it is able to be extracted properly.
        /// </summary>
        public int[] DetermineSaveTPLOffsets()
        {
            int[] offsets = new int[2];

            for (int i = Contents[1].Length - 20; i > 0; i--)
            {
                if (Contents[1][i] == 0xA2
                    && Contents[1][i + 1] == 0xDB
                    && Contents[1][i + 2] == 0xA2
                    && Contents[1][i + 3] == 0xDB
                    && Contents[1][i + 4] == 0xA2
                    && Contents[1][i + 5] == 0xDB
                    && Contents[1][i + 6] == 0xA2
                    && Contents[1][i + 7] == 0xDB
                    && Contents[1][i + 8] == 0xA2
                    && Contents[1][i + 9] == 0xDB
                    && Contents[1][i + 10] == 0xA2
                    && Contents[1][i + 11] == 0xDB
                    && Contents[1][i + 12] == 0xA2
                    && Contents[1][i + 13] == 0xDB
                    && Contents[1][i + 14] == 0xA2
                    && Contents[1][i + 15] == 0xDB)
                {
                    offsets[1] = i + 16;
                    for (int x = offsets[1]; x > 0; x--)
                    {
                        if (Contents[1][x] == 0x00
                            && Contents[1][x + 1] == 0x20
                            && Contents[1][x + 2] == 0xAF
                            && Contents[1][x + 3] == 0x30
                            && Contents[1][x + 4] == 0x00
                            && Contents[1][x + 5] == 0x00
                            && Contents[1][x + 6] == 0x00
                            && Contents[1][x + 7] == 0x05
                            && Contents[1][x + 8] == 0x00
                            && Contents[1][x + 9] == 0x00
                            && Contents[1][x + 10] == 0x00
                            && Contents[1][x + 11] == 0x0C
                            && Contents[1][x + 12] == 0x00
                            && Contents[1][x + 13] == 0x00
                            && Contents[1][x + 14] == 0x00
                            && Contents[1][x + 15] == 0x34)
                            offsets[0] = x;
                    }
                    break;
                }
            }

            return offsets;
        }

        /// <summary>
        /// Inserts custom savedata text string & TPL file into main.dol. The function skips TPL replacement if the file doesn't exist or the offsets are not set properly.
        /// </summary>
        /// <param name="lines">Text string array</param>
        /// <param name="tImg">Input title image</param>
        protected override void ReplaceSaveData(string[] lines, ImageHelper Img)
        {
            saveTPL_offsets = DetermineSaveTPLOffsets();

            // -----------------------
            // TEXT
            // -----------------------

            lines = ConvertSaveText(lines);
            string text = lines.Length > 1 ? string.Join("\n", lines) : lines[0];

            // In the two WADs I've tested (SMB3 & Kirby's Adventure), the savedata text is found near the string "VirtualIF.c MEM1 heap allocation error" within the content1 file.
            // In both aforementioned WADs the savetitle text must not be bigger than what the content1 can contain.
            // If trying to increase or decrease the filesize it breaks the WAD
            int end = Byte.IndexOf(Contents[1], "VirtualIF");

            if (end > 0)
            {
                int start = saveTPL_offsets[1] > 100 ? saveTPL_offsets[1] : end - 40;
                int length = end - start;

                for (int i = 0; i < length; i++)
                {
                    try { Contents[1][i + start] = SaveTextEncoding.GetBytes(text)[i]; }
                    catch { Contents[1][i + start] = 0x00; }
                }
            }

            // -----------------------
            // IMAGE
            // -----------------------

            if (saveTPL_offsets[0] != 0 && Img != null)
            {
                var TPL = new byte[1];

                // ----------------------------------------------------------------
                // Extracts savedata TPL from main.dol if offsets have been found.
                // ----------------------------------------------------------------
                if (saveTPL_offsets[0] != 0 && saveTPL_offsets[1] != 0)
                {
                    var tplList = new List<byte>();
                    for (int i = saveTPL_offsets[0]; i < saveTPL_offsets[1]; i++)
                        tplList.Add(Contents[1][i]);
                    TPL = tplList.ToArray();
                }
                if (TPL.Length < 5) return;

                // ----------------------------------------------------------------
                // Replace TPL
                // ----------------------------------------------------------------
                var TPLnew = Img.CreateSaveTPL(TPL).ToByteArray();

                for (int i = saveTPL_offsets[0]; i < saveTPL_offsets[1]; i++)
                    Contents[1][i] = TPLnew[i - saveTPL_offsets[0]];
            }
        }
    }
}