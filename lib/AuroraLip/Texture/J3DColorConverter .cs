﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AuroraLip.Texture
{

    /*
    * Super Hackio Incorporated
    * "Copyright © Super Hackio Incorporated 2020-2021"
    * https://github.com/SuperHackio/Hack.io
    */

    public static class J3DColorConverter
    {
        #region Color
        public static Color I4ToColor(byte Raw)
        {
            int val = (Raw << 4) | (Raw);
            return Color.FromArgb(val, val, val, val);
        }

        public static Color IA4ToColor(byte Raw)
        {
            int low_nibble = ((Raw & 0xF) << 4) | Raw & 0xF;
            return Color.FromArgb((((Raw >> 4) & 0xF) << 4) | ((Raw >> 4) & 0xF), low_nibble, low_nibble, low_nibble);
        }

        public static Color I8ToColor(byte Raw) => Color.FromArgb(Raw, Raw, Raw, Raw);

        public static Color IA8ToColor(ushort Raw)
        {
            int low_byte = Raw & 0xFF;
            return Color.FromArgb((Raw >> 8) & 0xFF, low_byte, low_byte, low_byte);
        }

        public static Color RGB565ToColor(ushort Raw)
        {
            int Red, Green, Blue, Value;
            Value = ((Raw >> 11) & 0x1F);
            Red = (Value << 3) | (Value >> 2);
            Value = ((Raw >> 5) & 0x3F);
            Green = (Value << 2) | (Value >> 4);
            Value = ((Raw >> 0) & 0x1F);
            Blue = (Value << 3) | (Value >> 2);
            return Color.FromArgb(Red, Green, Blue);
        }

        public static Color RGB5A3ToColor(ushort Raw)
        {
            int Red, Green, Blue, Value;
            if ((Raw & 0x8000) == 0)
            {
                Value = ((Raw >> 8) & 0xF);
                Red = (Value << 4) | (Value >> 0);
                Value = ((Raw >> 4) & 0xF);
                Green = (Value << 4) | (Value >> 0);
                Value = ((Raw >> 0) & 0xF);
                Blue = (Value << 4) | (Value >> 0);
                Value = ((Raw >> 12) & 0x7);
                int Alpha = (Value << 5) | (Value << 2) | (Value >> 1);
                return Color.FromArgb(Alpha, Red, Green, Blue);
            }
            else
            {
                Value = ((Raw >> 10) & 0x1F);
                Red = (Value << 3) | (Value >> 2);
                Value = ((Raw >> 5) & 0x1F);
                Green = (Value << 3) | (Value >> 2);
                Value = ((Raw >> 0) & 0x1F);
                Blue = (Value << 3) | (Value >> 2);
                return Color.FromArgb(Red, Green, Blue);
            }
        }

        public static Color[] GetInterpolatedDXT1Colours(ushort RawLeft, ushort RawRight)
        {
            Color Left = RGB565ToColor(RawLeft), Right = RGB565ToColor(RawRight);
            Color InterpA, InterpB;

            if (RawLeft > RawRight)
            {
                InterpA = Color.FromArgb((int)Math.Floor((2 * Left.R + 1 * Right.R) / 3.0), (int)Math.Floor((2 * Left.G + 1 * Right.G) / 3.0), (int)Math.Floor((2 * Left.B + 1 * Right.B) / 3.0));
                InterpB = Color.FromArgb((int)Math.Floor((1 * Left.R + 2 * Right.R) / 3.0), (int)Math.Floor((1 * Left.G + 2 * Right.G) / 3.0), (int)Math.Floor((1 * Left.B + 2 * Right.B) / 3.0));
            }
            else
            {
                InterpA = Color.FromArgb((int)Math.Floor(Left.R / 2.0) + (int)Math.Floor(Right.R / 2.0), (int)Math.Floor(Left.G / 2.0) + (int)Math.Floor(Right.G / 2.0), (int)Math.Floor(Left.B / 2.0) + (int)Math.Floor(Right.B / 2.0));
                InterpB = Color.FromArgb(1, (int)Math.Floor((1 * Left.R + 2 * Right.R) / 3.0), (int)Math.Floor((1 * Left.G + 2 * Right.G) / 3.0), (int)Math.Floor((1 * Left.B + 2 * Right.B) / 3.0));
            }

            return new Color[4] { Left, Right, InterpA, InterpB };
        }
        #endregion


        public static Color[] SplitToBuckets(List<Color> AllColours, int Depth)
        {
            if (Depth == 0)
                return new Color[1] { AverageColours(AllColours) };

            int RedRange = AllColours.Max(C => C.R) - AllColours.Min(C => C.R);
            int GreenRange = AllColours.Max(C => C.G) - AllColours.Min(C => C.G);
            int BlueRange = AllColours.Max(C => C.B) - AllColours.Min(C => C.B);

            int channel_index_with_highest_range = 0;
            if (GreenRange >= RedRange && GreenRange >= BlueRange)
                channel_index_with_highest_range = 1;
            else if (RedRange >= GreenRange && RedRange >= BlueRange)
                channel_index_with_highest_range = 0;
            else if (BlueRange >= RedRange && BlueRange >= GreenRange)
                channel_index_with_highest_range = 2;

            AllColours = AllColours.OrderBy(C => C.A).ToList();
            AllColours = AllColours.OrderBy(C => channel_index_with_highest_range == 1 ? C.G : (channel_index_with_highest_range == 0 ? C.R : C.B)).ToList();
            List<Color> Palette = new List<Color>();
            int median = (int)Math.Floor(AllColours.Count / 2.0);
            Palette.AddRange(SplitToBuckets(AllColours.GetRange(median, AllColours.Count - median), Depth - 1));
            Palette.AddRange(SplitToBuckets(AllColours.GetRange(0, median), Depth - 1));
            return Palette.ToArray();
        }

        public static Color AverageColours(List<Color> Colours)
        {
            for (int i = 0; i < Colours.Count; i++)
            {
                if (Colours[i].A == 0)
                {
                    // Need to ensure a fully transparent color exists in the final palette if one existed originally.
                    return Colours[i];
                }
            }

            int RedSum = 0, GreenSum = 0, BlueSum = 0, AlphaSum = 0;
            for (int i = 0; i < Colours.Count; i++)
            {
                RedSum += Colours[i].R;
                GreenSum += Colours[i].G;
                BlueSum += Colours[i].B;
                AlphaSum += Colours[i].A;
            }
            return Color.FromArgb((int)Math.Floor(AlphaSum / (double)Colours.Count), (int)Math.Floor(RedSum / (double)Colours.Count), (int)Math.Floor(GreenSum / (double)Colours.Count), (int)Math.Floor(BlueSum / (double)Colours.Count));
        }

        public static Color GetNearestColour(this Color Col, Color[] Palette)
        {
            if (Palette.Contains(Col))
                return Col;

            if (Col.A < 16)
                for (int i = 0; i < Palette.Length; i++)
                    if (Palette[i].A == 0)
                        return Palette[i];

            int min_dist = 0x7FFFFFFF;
            Color best_color = Palette[0];

            for (int i = 0; i < Palette.Length; i++)
            {
                int currentdistance = GetColorDistance(Col, Palette[i]);
                if (currentdistance < min_dist)
                {
                    if (currentdistance == 0)
                        return Palette[i];

                    min_dist = currentdistance;
                    best_color = Palette[i];
                }
            }

            return best_color;
        }

        public static int GetColorDistance(this Color Col1, Color Col2)
        {
            int r_diff = Col1.R - Col2.R;
            int g_diff = Col1.G - Col2.G;
            int b_diff = Col1.B - Col2.B;
            int a_diff = Col1.A - Col2.A;
            double rgb_dist_sqr = (r_diff * r_diff + g_diff * g_diff + b_diff * b_diff) / 3.0;
            return (int)(a_diff * a_diff / 2.0 + rgb_dist_sqr * Col1.A * Col2.A / (255 * 255));
        }

        public static int GetColorDistanceNoAlpha(this Color Col1, Color Col2) => Math.Abs(Col1.R - Col2.R) + Math.Abs(Col1.G - Col2.G) + Math.Abs(Col1.B - Col2.B);

        public static Tuple<Color, Color> GetBestCMPRKeyColours(List<Color> AllColours)
        {
            int MaxDistance = -1;
            Color Col1 = Color.Black, Col2 = Color.White;
            for (int i = 0; i < AllColours.Count; i++)
            {
                for (int j = i + 1; j < AllColours.Count; j++)
                {
                    int curr_dist = GetColorDistance(AllColours[i], AllColours[j]);

                    if (curr_dist > MaxDistance)
                    {
                        MaxDistance = curr_dist;
                        Col1 = Color.FromArgb(AllColours[i].R, AllColours[i].G, AllColours[i].B);
                        Col2 = Color.FromArgb(AllColours[j].R, AllColours[j].G, AllColours[j].B);
                    }
                }
            }
            if (MaxDistance == -1)
            {
                Col1 = Color.FromArgb(0, 0, 0);
                Col2 = Color.FromArgb(255, 255, 255);
            }
            else
            {
                if ((Col1.R >> 3) == (Col2.R >> 3) && (Col1.G >> 2) == (Col2.G >> 2) && (Col1.B >> 3) == (Col2.B >> 3))
                    Col2 = ((Col1.R >> 3) == 0 && (Col1.G >> 2) == 0 && (Col1.B >> 3) == 0) ? Color.FromArgb(255, 255, 255) : Color.FromArgb(0, 0, 0);
            }
            return new Tuple<Color, Color>(Col1, Col2);
        }

        public static Color[] GetInterpolatedCMPRColours(ushort RawColour1, ushort RawColour2)
        {
            Color Col1 = J3DColorConverter.RGB565ToColor(RawColour1), Col2 = J3DColorConverter.RGB565ToColor(RawColour2), Col3, Col4;
            if (RawColour1 > RawColour2)
            {
                Col3 = Color.FromArgb((int)Math.Floor((2 * Col1.R + 1 * Col2.R) / 3.0), (int)Math.Floor((2 * Col1.G + 1 * Col2.G) / 3.0), (int)Math.Floor((2 * Col1.B + 1 * Col2.B) / 3.0));
                Col4 = Color.FromArgb((int)Math.Floor((1 * Col1.R + 2 * Col2.R) / 3.0), (int)Math.Floor((1 * Col1.G + 2 * Col2.G) / 3.0), (int)Math.Floor((1 * Col1.B + 2 * Col2.B) / 3.0));
            }
            else
            {
                Col3 = Color.FromArgb((int)Math.Floor(Col1.R / 2.0) + (int)Math.Floor(Col2.R / 2.0), (int)Math.Floor(Col1.G / 2.0) + (int)Math.Floor(Col2.G / 2.0), (int)Math.Floor(Col1.B / 2.0) + (int)Math.Floor(Col2.B / 2.0));
                Col4 = Color.FromArgb(0, 0, 0, 0);
            }
            return new Color[4] { Col1, Col2, Col3, Col4 };
        }

        #region Colour Converters

        public static byte ToI4(this Color Col) => (byte)((((int)Math.Round(((Col.R * 30) + (Col.G * 59) + (Col.B * 11)) / 100.0)) >> 4) & 0xF);

        public static byte ToI8(this Color Col) => (byte)((int)Math.Round(((Col.R * 30) + (Col.G * 59) + (Col.B * 11)) / 100.0) & 0xFF);

        public static byte ToIA4(this Color Col)
        {
            int Value = (int)Math.Round(((Col.R * 30) + (Col.G * 59) + (Col.B * 11)) / 100.0);
            int Result = 0x00;
            Result |= ((Value >> 4) & 0xF);
            Result |= ((Col.A << 4) & 0xF0);
            return (byte)Result;
        }

        public static ushort ToIA8(this Color Col)
        {
            int Value = (int)Math.Round(((Col.R * 30) + (Col.G * 59) + (Col.B * 11)) / 100.0);
            int Result = 0x0000;
            Result |= Value & 0x00FF;
            Result |= (Col.A << 8) & 0xFF00;
            return (ushort)Result;
        }

        public static ushort ToRGB565(this Color Col)
        {
            int Result = 0x0000;
            Result |= ((Col.R >> 3) & 0x1F) << 11;
            Result |= ((Col.G >> 2) & 0x3F) << 5;
            Result |= (Col.B >> 3) & 0x1F;
            return (ushort)Result;
        }

        public static ushort ToRGB5A3(this Color Col)
        {
            int Result;
            if (Col.A != 255)
            {
                Result = 0x0000;
                Result |= (((Col.A >> 5) & 0x7) << 12);
                Result |= (((Col.R >> 4) & 0xF) << 8);
                Result |= (((Col.G >> 4) & 0xF) << 4);
                Result |= (((Col.B >> 4) & 0xF) << 0);
            }
            else
            {
                Result = 0x8000;
                Result |= (((Col.R >> 3) & 0x1F) << 10);
                Result |= (((Col.G >> 3) & 0x1F) << 5);
                Result |= (((Col.B >> 3) & 0x1F) << 0);
            }
            return (ushort)Result;
        }
        #endregion
    }
}
