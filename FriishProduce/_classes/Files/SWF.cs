using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FriishProduce
{
    /// <summary>
    ///     Standard compression types of SWF  </summary>
    public enum CompressType {
        NONE, ZLIB, LZMA, UNKNOWN
    }
    /// <summary>
    ///     Object that stores information gathered from an SWF  </summary>
    public class SWFMeta {
        public string Path { get; set; }
        public string Signature { get; set; }
        public byte Version { get; set; }
        public uint FileLen { get; set; }
        public CompressType CompressType { get; set; }
        public bool ContainsAS3 { get; set; }
        public int DoABC { get; set; }
        public byte[] DecompSWF { get; set; }
        public bool ContainsLAS { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    ///     ROM inherited SWF class for handling standard SWF functions and files  </summary>
    public class SWF : ROM {

        public SWF() : base() {
            SWFMeta = null;
        }

        public SWFMeta SWFMeta { get; set; }

        /// <summary>
        ///     Checks that the SWF exists and is a valid SWF file and is not AS3  </summary>
        public override bool CheckValidity(string path) {
            SWFMeta = GetMeta(path);
            return SWFMeta.Signature is "FWS" or "CWS" or "ZWS" && !IsAS3(SWFMeta);
        }

        /// <summary>
        ///     Checks the given swfMeta for ActionScript 3 signatures, or DoABC tags  </summary>
        public static bool IsAS3(SWFMeta info) => info.ContainsAS3 || info.DoABC != 0;
        /// <summary>
        ///     Checks the given file path for ActionScript 3 signatures, or DoABC tags  </summary>
        public static bool IsAS3(string path) => IsAS3(GetMeta(path));

        /// <summary>
        ///     Checks if the given swfMeta contains Legacy ActionScript and does NOT contain AS3 signatures or DoABC tags  </summary>
        public static bool IsAS2(SWFMeta info) => info.ContainsLAS && !info.ContainsAS3 && info.DoABC <= 0;
        /// <summary>
        ///     Checks if the given swfMeta contains Legacy ActionScript and does NOT contain AS3 signatures or DoABC tags  </summary>
        public static bool IsAS2(string path) => IsAS2(GetMeta(path));

        /// <summary>
        ///     Reads the SWF header of a given file path, decompresses if needed, and extracts info  </summary>
        public static SWFMeta GetMeta(string path) {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            string sig = Encoding.ASCII.GetString(reader.ReadBytes(3));
            byte ver = reader.ReadByte();
            uint lens = reader.ReadUInt32();

            CompressType cType = sig switch {
                "FWS" => CompressType.NONE, "CWS" => CompressType.ZLIB, "ZWS" => CompressType.LZMA, _ => CompressType.UNKNOWN
            };
            byte[] data = cType switch {
                CompressType.NONE => ReadDecomp(stream), CompressType.ZLIB => DecompCws(stream), CompressType.LZMA => DecompZws(stream, lens), _ => Array.Empty<byte>()
            };
            SWFMeta meta = new() {
                Path = path, DecompSWF = data, Version = ver, Signature = sig, CompressType = cType, FileLen = lens
            };
            return GetProperties(meta);
        }

        /// <summary>
        ///     Scans through the SWF and gathers AS2/AS3 tags  </summary>
        private static SWFMeta GetProperties(SWFMeta meta) {
            int offset = 8; // after Signature(3) + Version(1) + FileLen(4)
            var reader = new SwfBitReader(meta.DecompSWF, offset);
            int nBits;

            try {
                nBits = reader.ReadBits(5); // orthorect bits per field
            } catch {
                return meta; // malformed header
            }
            int xMin = reader.ReadBits(nBits);
            int xMax = reader.ReadBits(nBits);
            int yMin = reader.ReadBits(nBits);
            int yMax = reader.ReadBits(nBits);
            // SWF units are in TWIPS (20 per pixel)
            meta.Width = (xMax - xMin) / 20;
            meta.Height = (yMax - yMin) / 20;

            // Set offset past orthorect
            int rectBytes = (int)Math.Ceiling((5 + 4 * nBits) / 8.0);
            offset += rectBytes + 2 + 2;
            int pos = offset;

            // Scan for AS3/AS2 tags
            while (pos + 2 <= meta.DecompSWF.Length) {
                ushort tagData = (ushort)(meta.DecompSWF[pos] | (meta.DecompSWF[pos + 1] << 8));
                pos += 2;

                int tagId = tagData >> 6;
                int tagLens = tagData & 0x3F;

                if (tagLens == 0x3F) {
                    if (pos + 4 > meta.DecompSWF.Length) break;
                    tagLens = meta.DecompSWF[pos] | (meta.DecompSWF[pos + 1] << 8) | (meta.DecompSWF[pos + 2] << 16) | (meta.DecompSWF[pos + 3] << 24);
                    pos += 4;
                }
                if (tagId == 82 || tagId == 72) { // DoABC tags
                    meta.ContainsAS3 = true;
                    meta.DoABC = tagId;
                }
                if (tagId == 12 || tagId == 59) // Legacy ActionScript
                    meta.ContainsLAS = true;

                pos += tagLens;
            }

            return meta;
        }

        /// <summary>
        ///     Reads a decompressed SWF straight from the stream  </summary>
        private static byte[] ReadDecomp(Stream stream) {
            stream.Seek(0, SeekOrigin.Begin);
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            return memStream.ToArray();
        }

        /// <summary>
        ///     Decompresses a CWS (zlib) SWF  </summary>
        private static byte[] DecompCws(Stream stream) {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[8];
            stream.Read(header, 0, 8);

            using var memStream = new MemoryStream();
            try {
                long bodyStart = stream.Position;
                byte[] peek = new byte[2];
                stream.Read(peek, 0, 2);
                stream.Seek(bodyStart, SeekOrigin.Begin);

                // If zlib header (78 9C / 78 01 / etc), skip first 2 bytes
                if (peek[0] == 0x78)
                    stream.Seek(2, SeekOrigin.Current);

                using var dfStream = new DeflateStream(stream, CompressionMode.Decompress, true);
                dfStream.CopyTo(memStream);
            }
            catch (Exception e) {
                Logger.INFO($"Zlib decompression failed: {e.Message}");
                return Array.Empty<byte>();
            }

            var body = memStream.ToArray();
            using var combined = new MemoryStream();
            combined.Write(header, 0, 8);
            combined.Write(body, 0, body.Length);
            return combined.ToArray();
        }

        /// <summary>
        ///     Decompresses a ZWS (LZMA) SWF  </summary>
        private static byte[] DecompZws(Stream fs, uint fileLength) {
            fs.Seek(0, SeekOrigin.Begin);
            using var reader = new BinaryReader(fs);
            string sig = Encoding.ASCII.GetString(reader.ReadBytes(3));
            if (sig != "ZWS") throw new InvalidDataException("Not a ZWS SWF");
            byte version = reader.ReadByte();
            uint length = reader.ReadUInt32();

            long posAfterHeader = 8;
            fs.Seek(posAfterHeader, SeekOrigin.Begin);

            byte propByte = reader.ReadByte();
            byte[] dictSizeBytes = reader.ReadBytes(4);
            long remaining = fs.Length - fs.Position;
            byte[] compressedData = reader.ReadBytes((int)remaining);

            using var lzmaStream = new MemoryStream();
            lzmaStream.WriteByte(propByte);
            lzmaStream.Write(dictSizeBytes, 0, 4);
            lzmaStream.Write(compressedData, 0, compressedData.Length);
            lzmaStream.Seek(0, SeekOrigin.Begin);

            using var outStream = new MemoryStream();
            try {
                var decoder = new SevenZip.Sdk.Compression.Lzma.Decoder();
                decoder.SetDecoderProperties(new byte[] { propByte });
                decoder.Code(lzmaStream, outStream, compressedData.Length, fileLength - 8, null);
            } catch (Exception e) {
                Logger.INFO($"LZMA decoding failed: {e.Message}");
            }

            using var result = new MemoryStream();
            result.Write(new byte[] { (byte)'Z', (byte)'W', (byte)'S', version }, 0, 4);
            result.Write(BitConverter.GetBytes(fileLength), 0, 4);
            outStream.Seek(0, SeekOrigin.Begin);
            outStream.CopyTo(result);
            return result.ToArray();
        }

        /// <summary>
        ///     Simple bit-reader for pulling fields from the SWF header  </summary>
        private class SwfBitReader {
            private readonly byte[] _data;
            private int bytePos;
            private int bitPos;

            public SwfBitReader(byte[] data, int startOffset) {
                _data = data;
                bytePos = startOffset;
                bitPos = 0;
            }
            public int ReadBits(int n) {
                int val = 0;
                for (int idx = 0; idx < n; idx++) {
                    if (bytePos >= _data.Length) throw new EndOfStreamException();
                    val = (val << 1) | ((_data[bytePos] >> (7 - bitPos)) & 1);
                    bitPos++;
                    if (bitPos == 8) {
                        bitPos = 0;
                        bytePos++;
                    }
                }
                return val;
            }
        }
    }
}