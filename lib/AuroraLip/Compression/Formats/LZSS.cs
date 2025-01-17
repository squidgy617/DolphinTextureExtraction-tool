﻿using AuroraLip.Common;
using System;
using System.IO;

namespace AuroraLip.Compression.Formats
{
    /// <summary>
    /// LZSS Lempel–Ziv–Storer–Szymanski algorithm, a derivative of LZ77.
    /// </summary>
    // base on original C implementation from Haruhiko Okumura
    // Mario Party  = EI=10 EJ=6 P=2
    // Pokemon FSYS = EI=12 EJ=4 P=2
    public class LZSS : ICompression, IMagicIdentify
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public string Magic => magic;

        private const string magic = "LZSS";

        public byte EI = 10; // offset bits
        public byte EJ = 6; // length bits
        public byte P = 2; // threshold

        public LZSS() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eI">offset bits</param>
        /// <param name="eJ">length bits</param>
        /// <param name="p">threshold</param>
        public LZSS(byte eI, byte eJ, byte p)
        {
            EI = eI; EJ = eJ; P = p;
        }

        public byte[] Compress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public byte[] Decompress(in byte[] Data)
        {
            MemoryStream stream = new MemoryStream(Data);

            if (!IsMatch(stream))
                throw new InvalidIdentifierException(Magic);
            uint decompressedSize = stream.ReadUInt32(Endian.Big);
            uint compressedSize = stream.ReadUInt32(Endian.Big);
            uint unk = stream.ReadUInt32(Endian.Big);

            return Decompress(stream, (int)compressedSize).ToArray();
        }

        public MemoryStream Decompress(Stream inputStream, int compressedSize)
        {
            int n = 1 << EI;
            int f = 1 << EJ;

            int flags = 0;
            byte[] slidingWindow = new byte[n];
            MemoryStream outStream = new MemoryStream(compressedSize);

            int r = n - f - P;
            n--;
            f--;

            while (outStream.Position < compressedSize)
            {
                //Reads New Code Word from Compressed Stream if Expired
                if ((flags & 0x100) == 0)
                {
                    flags = inputStream.ReadByte();
                    flags |= 0xff00;
                }
                //Copies a Byte from the Source to the Destination and Window Buffer
                if ((flags & 1) != 0)
                {
                    byte c = (byte)inputStream.ReadByte();
                    outStream.WriteByte(c);
                    slidingWindow[r] = c;
                    r = r + 1 & n;
                }
                else
                {
                    //Interpret Next 2 Bytes as an Offset and Length into the Window Buffer
                    int b1 = inputStream.ReadByte();
                    int b2 = inputStream.ReadByte();

                    b1 |= b2 >> EJ << 8;
                    b2 = (b2 & f) + P;

                    //Copy Some Bytes from Window Buffer
                    for (var i = 0; i <= b2; i++)
                    {
                        slidingWindow[r] = slidingWindow[b1 + i & n];
                        outStream.WriteByte(slidingWindow[r]);
                        r = r + 1 & n;
                    }
                }
                flags >>= 1;
            }
            return outStream;
        }

        public bool IsMatch(Stream stream, in string extension = "")
            => stream.Length > 112 && stream.MatchString(Magic);
    }
}
