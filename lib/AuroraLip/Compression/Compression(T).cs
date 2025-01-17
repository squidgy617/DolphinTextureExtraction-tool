﻿using AuroraLip.Common;
using System.IO;

namespace AuroraLip.Compression
{
    /// <summary>
    /// Universal class with methods for compressing and decompressing.
    /// </summary>
    public static class Compression<T> where T : ICompression, new()
    {

        /// <summary>
        /// Can be decompress.
        /// </summary>
        public static bool CanDecompress { get => new T().CanRead; }

        /// <summary>
        /// Can be compress.
        /// </summary>
        public static bool CanCompress { get => new T().CanWrite; }

        #region Decompress
        /// <summary>
        /// Decompress a File
        /// </summary>
        /// <param name="filename">Full path to the file</param>
        public static void Decompress(in string filename) => Decompress(filename, filename);

        /// <summary>
        /// Decompress a file and save it to a new file
        /// </summary>
        /// <param name="InFilename">Full path to the file</param>
        /// <param name="OutFilename">Full path to the new file</param>
        public static void Decompress(in string InFilename, in string OutFilename) => File.WriteAllBytes(OutFilename, new T().Decompress(File.ReadAllBytes(InFilename)));

        /// <summary>
        /// Decompress a byte[]
        /// </summary>
        /// <param name="Data">Compressed data</param>
        /// <returns>Decompressed data</returns>
        public static byte[] Decompress(in byte[] Data) => new T().Decompress(in Data);

        /// <summary>
        /// Decompress a MemoryStream
        /// </summary>
        /// <param name="memorystream">Compressed MemoryStream</param>
        /// <returns>Decompressed MemoryStream</returns>
        public static MemoryStream Decompress(MemoryStream memorystream) => new MemoryStream(new T().Decompress(memorystream.ToArray()));


        /// <summary>
        /// Decompress a MemoryStream
        /// </summary>
        /// <param name="stream">Compressed MemoryStream</param>
        /// <returns>Decompressed MemoryStream</returns>
        public static MemoryStream Decompress(Stream stream) => new MemoryStream(new T().Decompress(stream.ToArray()));
        #endregion

        #region Compress
        /// <summary>
        /// Compress a File
        /// </summary>
        /// <param name="filename">Full path to the file</param>
        public static void Compress(in string Filename) => Compress(Filename, Filename);

        /// <summary>
        /// Compress a file and save it to a new file
        /// </summary>
        /// <param name="InFilename">Full path to the file</param>
        /// <param name="OutFilename">Full path to the new file</param>
        public static void Compress(in string InFilename, in string OutFilename) => File.WriteAllBytes(OutFilename, new T().Compress(File.ReadAllBytes(InFilename)));

        /// <summary>
        /// Compress a byte[]
        /// </summary>
        /// <param name="Data">Decompressed data</param>
        /// <returns>Compressed data</returns>
        public static byte[] Compress(in byte[] Data) => new T().Compress(in Data);

        /// <summary>
        /// Compress a MemoryStream
        /// </summary>
        /// <param name="memorystream">Decompressed MemoryStream</param>
        /// <returns>Compressed MemoryStream</returns>
        public static MemoryStream Compress(MemoryStream memorystream) => new MemoryStream(new T().Compress(memorystream.ToArray()));
        #endregion

        /// <summary>
        /// Checks if the data compressed with this compression method
        /// </summary>
        /// <param name="Data"></param>
        /// <returns>"True" if it corresponds to the compression method.</returns>
        public static bool IsMatch(in byte[] Data) => new T().IsMatch(in Data);

        /// <summary>
        /// Checks if the data compressed with this compression method
        /// </summary>
        /// <param name="Data"></param>
        /// <returns>"True" if it corresponds to the compression method.</returns>
        public static bool IsMatch(Stream Data) => new T().IsMatch(Data);

    }
}
