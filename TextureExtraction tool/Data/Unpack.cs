﻿using AuroraLip.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DolphinTextureExtraction_tool
{
    public class Unpack : ScanBase
    {
        public Unpack(string scanDirectory, string saveDirectory, Options options = null) : base(scanDirectory, saveDirectory, options)
        {
        }

        public static Results StartScan(string meindirectory, string savedirectory, Options options)
            => StartScan_Async(meindirectory, savedirectory, options).Result;

        public static async Task<Results> StartScan_Async(string meindirectory, string savedirectory, Options options)
        {
            Unpack Extractor = new Unpack(meindirectory, savedirectory, options);
            return await Task.Run(() => Extractor.StartScan());
        }

        protected override void Scan(ScanObjekt so)
        {
#if !DEBUG
            try
            {
#endif

            switch (so.Format.Typ)
                {
                    case FormatType.Unknown:
                        if (Option.Force && TryForce(so))
                            break;

                        AddResultUnknown(so.Stream, so.Format, so.SubPath.ToString() + so.Extension);
                        if (so.Deep != 0)
                            Save(so.Stream, so.SubPath.ToString(), so.Format);
                        break;
                case FormatType.Rom:
                case FormatType.Archive:
                        if (!TryExtract(so))
                        {
                            Log.Write(FileAction.Unsupported, so.SubPath.ToString() + so.Extension + $" ~{MathEx.SizeSuffix(so.Stream.Length, 2)}", $"Description: {so.Format.GetFullDescription()}");
                            Save(so.Stream, so.SubPath.ToString(), so.Format);
                        }
                        break;
                    default:
                        if (so.Deep != 0)
                            Save(so.Stream, so.SubPath.ToString(), so.Format);
                        break;
                }
#if !DEBUG
            }
            catch (Exception t)
            {
                Log.WriteEX(t, so.SubPath.ToString() + so.Extension);
                if (so.Deep != 0)
                    Save(so.Stream, so.SubPath.ToString(), so.Format);
            }
#endif
        }

        private void AddResultUnknown(Stream stream, FormatInfo FormatTypee, in string file)
        {
            if (FormatTypee.Header == null || FormatTypee.Header?.Magic.Length <= 3)
            {
                Log.Write(FileAction.Unknown, file + $" ~{MathEx.SizeSuffix(stream.Length, 2)}",
                    $"Bytes32:[{BitConverter.ToString(stream.Read(32))}]");
            }
            else
            {
                Log.Write(FileAction.Unknown, file + $" ~{MathEx.SizeSuffix(stream.Length, 2)}",
                    $"Magic:[{FormatTypee.Header.Magic}] Bytes:[{string.Join(",", FormatTypee.Header.Bytes)}] Offset:{FormatTypee.Header.Offset}");
            }
        }

    }
}
