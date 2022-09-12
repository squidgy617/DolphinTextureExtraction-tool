﻿using AFSLib;
using AuroraLip.Archives;
using AuroraLip.Common;
using AuroraLip.Compression;
using LibCPK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DolphinTextureExtraction_tool
{
    public abstract class ScanBase
    {
        protected readonly string ScanDirectory;

        protected readonly string SaveDirectory;

        internal readonly ScanLogger Log;

        protected readonly Options Option;

        protected Results Result = new Results();

        public class Options
        {
#if DEBUG
            public ParallelOptions Parallel = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
#else
            public ParallelOptions Parallel = new ParallelOptions() { MaxDegreeOfParallelism = 4 };
#endif
            internal ParallelOptions SubParallel => new ParallelOptions() { MaxDegreeOfParallelism = Parallel.MaxDegreeOfParallelism == 1 ? 1 : Parallel.MaxDegreeOfParallelism / 2, CancellationToken = Parallel.CancellationToken, TaskScheduler = Parallel.TaskScheduler };

            /// <summary>
            /// will be executed if progress was made
            /// </summary>
            public Action<Results> ProgressAction;

            private double LastProgressLength = 0;
            internal void ProgressUpdate(Results result)
            {

                if (result.Progress >= result.Worke)
                {
                    //we have to report the last progress!
                    Monitor.Enter(result);
                    try
                    {
                        LastProgressLength = 0;
                        result.ProgressLength = result.WorkeLength;
                        ProgressAction?.Invoke(result);
                    }
                    finally
                    {
                        Monitor.Exit(result);
                    }
                }
                else
                {
                    //Try to tell the Progress
                    if (!Monitor.TryEnter(result))
                        return;

                    try
                    {
                        //is there really progress to report.
                        if (result.ProgressLength < LastProgressLength)
                            return;

                        //when data has been compressed, we can achieve more than 100%... we prevent this.
                        if (result.ProgressLength > result.WorkeLength)
                            result.ProgressLength = result.WorkeLength;

                        LastProgressLength = result.ProgressLength;

                        ProgressAction?.Invoke(result);
                    }
                    finally
                    {
                        Monitor.Exit(result);
                    }
                }

            }
        }


        public class Results
        {
            /// <summary>
            /// count of all files to be searched.
            /// </summary>
            public int Worke { get; internal set; }

            /// <summary>
            /// count of all files already searched.
            /// </summary>
            public int Progress { get; internal set; } = 0;

            /// <summary>
            /// Size of all files to be searched in bytes.
            /// </summary>
            public double WorkeLength { get; internal set; }

            /// <summary>
            /// Size of all already searched files in bytes.
            /// </summary>
            public double ProgressLength { get; internal set; } = 0;

            /// <summary>
            /// Full path to the log file.
            /// </summary>
            public string LogFullPath { get; internal set; }
        }

        protected ScanBase(string scanDirectory, string saveDirectory, Options options = null)
        {
            ScanDirectory = scanDirectory;
            SaveDirectory = saveDirectory;
            Directory.CreateDirectory(saveDirectory);
            Log = new ScanLogger(SaveDirectory);
            Events.NotificationEvent = Log.WriteNotification;
            Result.LogFullPath = Log.FullPath;

            if (options == null)
            {
                Option = new Options();
            }
            else
            {
                Option = options;
            }
        }

        public Results StartScan()
        {
            Scan(new DirectoryInfo(ScanDirectory));
            Log.Flush();
            Log.Dispose();
            return Result;
        }

        protected void Scan(DirectoryInfo directory)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();
            ScanInitialize(directory, fileInfos);
            Result.Worke = fileInfos.Count;
            Result.WorkeLength = directory.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            Option.ProgressUpdate(Result);

            Parallel.ForEach(fileInfos, Option.Parallel, (file, localSum, i) =>
            {
                Scan(file);
                lock (Result)
                {
                    Result.Progress++;
                    Result.ProgressLength += file.Length;
                }
                Option.ProgressUpdate(Result);
            });
            GC.Collect();
        }

        private void ScanInitialize(DirectoryInfo directory, List<FileInfo> fileInfos)
        {
            foreach (DirectoryInfo subdirectory in directory.GetDirectories())
                ScanInitialize(subdirectory, fileInfos);

            foreach (FileInfo file in directory.GetFiles())
                fileInfos.Add(file);
        }

        #region Scan
        protected abstract void Scan(FileInfo file);

        protected abstract void Scan(Stream stream, string subdirectory, in string Extension = "");

        protected void Scan(Archive archiv, in string subdirectory)
            => Scan(archiv.Root, subdirectory);

        protected void Scan(ArchiveDirectory archivdirectory, string subdirectory)
        {
            List<ArchiveFile> fileInfos = new List<ArchiveFile>();
            ArchiveInitialize(archivdirectory, fileInfos);

            double ArchLength = 0;
            Parallel.ForEach(fileInfos, Option.SubParallel, (file) =>
            {

                double Length = file.FileData.Length;
                Scan(file, subdirectory);
                lock (Result)
                {
                    ArchLength += Length;
                    Result.ProgressLength += Length;
                }
                Option.ProgressUpdate(Result);
            });

            lock (Result)
            {
                Result.ProgressLength -= ArchLength;
            }
        }

        private void ArchiveInitialize(ArchiveDirectory archivdirectory, List<ArchiveFile> files)
        {
            foreach (var item in archivdirectory.Items)
            {
                if (item.Value is ArchiveFile file)
                {
                    files.Add(file);
                }
                if (item.Value is ArchiveDirectory directory)
                {
                    ArchiveInitialize(directory, files);
                }
            }
        }

        protected void Scan(ArchiveFile file, in string subdirectory)
        {
            Scan(file.FileData, Path.Combine(subdirectory, file.FullPath), file.Extension.ToLower());
        }
        #endregion

        #region Helper
        /// <summary>
        /// Writes a Steam to a new file.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="destFileName"></param>
        protected void Save(Stream stream, string destFileName)
        {
            string DirectoryName = Path.GetDirectoryName(destFileName);
            //We can't create a folder if a file with the same name exists.
            if (File.Exists(DirectoryName))
                File.Move(DirectoryName, DirectoryName + "_");

            Directory.CreateDirectory(DirectoryName);
            stream.Seek(0, SeekOrigin.Begin);
            using (FileStream file = new FileStream(destFileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }
            stream.Seek(0, SeekOrigin.Begin);
        }

        protected void Save(Stream stream, string subdirectory, FormatInfo FFormat)
            => Save(stream, Path.ChangeExtension(GetFullSaveDirectory(subdirectory), FFormat.Extension));

        protected virtual bool TryExtract(Stream stream, string subdirectory, FormatInfo FFormat)
        {
            if (FFormat.Class == null)
            {
                switch (FFormat.Extension.ToLower())
                {
                    case ".arc":
                    case ".tpl":
                    case ".bti":
                    case ".lz":
                    case ".brres":
                    case ".breff":
                    case ".zlib":
                    case ".lz77":
                    case ".wtm":
                    case ".vld":
                    case ".cxd":
                    case ".cms":
                    case ".cmp":
                    case ".cmparc":
                    case ".cmpres":
                        if (Reflection.Compression.TryToDecompress(stream, out Stream test, out _))
                        {
                            Scan(test, subdirectory);
                            return true;
                        }
                        break;
                }
            }
            else
            {
                if (FFormat.Class.IsSubclassOf(typeof(Archive)))
                {
                    using (Archive archive = (Archive)Activator.CreateInstance(FFormat.Class))
                    {
                        archive.Open(stream);
                        Scan(archive, subdirectory);

                        if (stream.Length > 104857600 * 5) //100MB*5
                            return true;

                        //Reduces problems with multithreading
                        if (archive.TotalFileCount > 0)
                        {
                            var last = archive.Root.Items.Last().Value;
                            while (true)
                            {
                                if (last is ArchiveDirectory dir)
                                {
                                    last = dir.Items.Last().Value;
                                    continue;
                                }
                                else if (last is ArchiveFile file)
                                {
                                    if (file.FileData is SubStream FileData)
                                        if (stream.Position < FileData.Length + FileData.Offset)
                                            stream.Seek(FileData.Length + FileData.Offset, SeekOrigin.Begin);
                                }
                                break;
                            }
                        }

                        //checks if hidden files are present.
                        if (archive is IMagicIdentify identify)
                        {
                            if (stream.Search(identify.Magic))
                            {
                                List<byte[]> ident = new List<byte[]>();
                                ident.Add(identify.Magic.ToByte());
                                using (Archive Cut = new DataCutter(stream, ident))
                                {
                                    foreach (var item in Cut.Root.Items)
                                        ((ArchiveFile)item.Value).Name = ((ArchiveFile)item.Value).Extension;

                                    Scan(Cut, subdirectory);
                                }
                            }
                        }
                    }
                    return true;
                }
                if (FFormat.Class.GetInterface(nameof(ICompression)) != null)
                {
                    Scan(((ICompression)Activator.CreateInstance(FFormat.Class)).Decompress(stream), subdirectory);
                    return true;
                }
                //External classes
                switch (FFormat.Class.Name)
                {
                    case "AFS":
                        using (AFS afs = new AFS(stream))
                        {
                            foreach (Entry item in afs.Entries)
                                if (item is StreamEntry Streamitem)
                                    Scan(Streamitem.GetSubStream(), Path.Combine(subdirectory, Streamitem.SanitizedName), Path.GetExtension(Streamitem.SanitizedName));
                        }
                        break;
                    case "CPK":
                        scanCPK(stream, subdirectory);
                        break;
                }
            }
            return false;
        }

        protected virtual bool TryForce(Stream stream, string subdirectory, FormatInfo FFormat)
        {
            if (Reflection.Compression.TryToDecompress(stream, out Stream test, out _))
            {
                Scan(test, subdirectory);
                return true;
            }

            if (TryCut(stream, subdirectory, FFormat))
                return true;

            return false;
        }

        private (FormatInfo, int) badformats;
        protected bool TryCut(Stream stream, string subdirectory, FormatInfo FFormat)
        {
            try
            {
                if (badformats.Item1 == FFormat)
                    if (badformats.Item2 > 4)
                        return false;

                Archive archive = new DataCutter(stream);
                if (archive.Root.Count > 0)
                {

                    badformats = (FFormat, -1);
                    Scan(archive, subdirectory);
                    return true;
                }
            }
            catch (Exception t)
            {
                Log.WriteEX(t, subdirectory + FFormat.Extension);
            }

            if (badformats.Item1 == FFormat)
            {
                if (badformats.Item2 != -1)
                    badformats.Item2++;
            }
            else
                badformats = (FFormat, 0);
            return false;
        }

        protected virtual void scanCPK(Stream stream, string subdirectory)
        {
            CPK CpkContent = new CPK();
            CpkContent.ReadCPK(stream, Encoding.UTF8);
            BinaryReader CPKReader = new BinaryReader(stream);

            foreach (var entries in CpkContent.fileTable)
            {
                string FullPath;
                if (entries.DirName != null)
                {
                    FullPath = Path.Combine(subdirectory, entries.DirName.ToString(), entries.FileName?.ToString());
                }
                else
                {
                    FullPath = Path.Combine(subdirectory, entries.FileName?.ToString());
                }
                try
                {
                    if (CpkDecompressEntrie(CpkContent, CPKReader, entries, out byte[] chunk))
                    {
                        MemoryStream CpkContentStream = new MemoryStream(chunk);
                        Scan(CpkContentStream, FullPath);
                        CpkContentStream.Dispose();
                    }
                }
                catch (Exception t)
                {
                    Log.WriteEX(t, FullPath);
                }
            }
            CPKReader.Close();
        }

        protected bool CpkDecompressEntrie(CPK CpkContent, BinaryReader CPKReader, LibCPK.FileEntry entrie, out byte[] chunk)
        {
            CPKReader.BaseStream.Seek((long)entrie.FileOffset, SeekOrigin.Begin);

            string isComp = Encoding.ASCII.GetString(CPKReader.ReadBytes(8));
            CPKReader.BaseStream.Seek((long)entrie.FileOffset, SeekOrigin.Begin);

            chunk = CPKReader.ReadBytes(Int32.Parse(entrie.FileSize.ToString()));

            if (isComp == "CRILAYLA")
            {
                int size = Int32.Parse(entrie.ExtractSize.ToString()) == 0 ? Int32.Parse(entrie.FileSize.ToString()) : Int32.Parse(entrie.ExtractSize.ToString());

                if (size != 0)
                {
                    chunk = CpkContent.DecompressLegacyCRI(chunk, size);
                    return true;
                }
            }
            return false;
        }

        protected string GetFullSaveDirectory(string directory)
            => Path.Combine(SaveDirectory, directory);

        protected static string GetDirectoryWithoutExtension(string directory)
            => Path.Combine(Path.GetDirectoryName(directory), Path.GetFileNameWithoutExtension(directory)).Trim();

        private List<FormatInfo> usedformats = new List<FormatInfo>();
        private readonly object Lock = new object();

        protected FormatInfo GetFormatTypee(Stream stream, string extension = "")
        {
            if (FormatDictionary.Header.TryGetValue(new HeaderInfo(stream).Magic, out FormatInfo Info))
            {
                if (Info.IsMatch.Invoke(stream, extension))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return Info;
                }
                stream.Seek(0, SeekOrigin.Begin);
            }

            lock (Lock)
            {
                foreach (var item in usedformats)
                {
                    if (item.IsMatch.Invoke(stream, extension))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        return item;
                    }
                    stream.Seek(0, SeekOrigin.Begin);
                }
                FormatInfo info = FormatDictionary.Identify(stream, extension);
                usedformats.Add(info);

                return info;
            }
        }
        #endregion
    }
}
