﻿using AuroraLip.Common;
using AuroraLip.Compression.Formats;
using System;
using System.IO;

namespace AuroraLip.Archives.Formats
{
    // From https://zenhax.com/viewtopic.php?f=9&t=7288
    // Thanks to Acewell, aluigi, AlphaTwentyThree, Chrrox
    public class POD5 : Archive, IMagicIdentify, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public string Magic => magic;

        private const string magic = "POD5";

        public POD5() { }

        public POD5(string filename) : base(filename) { }

        public POD5(Stream stream, string filename = null) : base(stream, filename) { }

        public bool IsMatch(Stream stream, in string extension = "")
            => stream.MatchString(magic);

        protected override void Read(Stream stream)
        {
            if (!stream.MatchString(magic))
                throw new Exception($"Invalid Identifier. Expected \"{Magic}\"");
            stream.Seek(0x58, SeekOrigin.Begin);
            uint FileCount = stream.ReadUInt32(Endian.Little);
            stream.Seek(0x108, SeekOrigin.Begin);
            uint InfoTable = stream.ReadUInt32(Endian.Little);
            uint StringTableOffset = (FileCount * 0x1c) + InfoTable;

            Root = new ArchiveDirectory() { OwnerArchive = this };

            stream.Seek(InfoTable, SeekOrigin.Begin);
            for (int i = 0; i < FileCount; i++)
            {
                uint NameOffsetForFile = stream.ReadUInt32(Endian.Little);
                uint SizeForFile = stream.ReadUInt32(Endian.Little);
                uint OffsetForFile = stream.ReadUInt32(Endian.Little);
                uint CompressedSizeForFile = stream.ReadUInt32(Endian.Little);
                uint Compressed = stream.ReadUInt32(Endian.Little);
                uint Unknown1 = stream.ReadUInt32(Endian.Little);
                uint Unknown2 = stream.ReadUInt32(Endian.Little);
                long SavedPosition = stream.Position;

                stream.Seek(NameOffsetForFile + StringTableOffset, SeekOrigin.Begin);
                string Name = stream.ReadString();

                //If Duplicate...
                if (Root.Items.ContainsKey(Name)) Name = Name + i.ToString();

                ArchiveFile Sub = new ArchiveFile() { Parent = Root, Name = Name };
                stream.Seek(OffsetForFile, SeekOrigin.Begin);
                if (SizeForFile == CompressedSizeForFile)
                    Sub.FileData = new SubStream(stream, SizeForFile);
                else
                    Sub.FileData = new MemoryStream(AuroraLip.Compression.Compression<ZLib>.Decompress(stream.Read((int)SizeForFile)));
                Root.Items.Add(Sub.Name, Sub);

                // Read the file, move on to the next one
                stream.Seek(SavedPosition, SeekOrigin.Begin);
            }
        }

        protected override void Write(Stream ArchiveFile)
        {
            throw new NotImplementedException();
        }
    }
}
