﻿using System;

namespace AuroraLip.Compression.Formats
{
    /// <summary>
    /// Nintendo LZ10 compression algorithm
    /// </summary>
    public class LZ10 : ICompression
    {
        public bool CanWrite { get; } = false;

        public bool CanRead { get; } = false;

        public byte[] Compress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public byte[] Decompress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public bool IsMatch(in byte[] Data)
        {
            return Data.Length > 2 && Data[0] == 31 && Data[1] == 139;
        }
    }
}
