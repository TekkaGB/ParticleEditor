﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static ParticleEditor.MainWindow;

namespace ParticleEditor
{
    public static class ColorOverLifeExtensions
    {
        public static int OverwriteData(this ColorOverLife color, ref byte[] fileBytes, int pos, Map<string, int> NameTable, Game game)
        {
            try
            {
                // Get data depending on type
                switch (NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])])
                {
                    case "MinValue":
                        BitConverter.GetBytes(color.MinValue).CopyTo(fileBytes, pos + 25);
                        pos += 29;
                        break;
                    case "MaxValue":
                        BitConverter.GetBytes(color.MaxValue).CopyTo(fileBytes, pos + 25);
                        pos += 29;
                        break;
                    case "MinValueVec":
                        BitConverter.GetBytes(color.MinValueVec.R).CopyTo(fileBytes, pos + 49);
                        BitConverter.GetBytes(color.MinValueVec.G).CopyTo(fileBytes, pos + 53);
                        BitConverter.GetBytes(color.MinValueVec.B).CopyTo(fileBytes, pos + 57);
                        pos += 61;
                        break;
                    case "MaxValueVec":
                        BitConverter.GetBytes(color.MaxValueVec.R).CopyTo(fileBytes, pos + 49);
                        BitConverter.GetBytes(color.MaxValueVec.G).CopyTo(fileBytes, pos + 53);
                        BitConverter.GetBytes(color.MaxValueVec.B).CopyTo(fileBytes, pos + 57);
                        pos += 61;
                        break;
                    case "Distribution":
                        // Different offsets for different games
                        if (game == Game.GGS)
                            pos += 78;
                        else if (game == Game.DBFZ)
                            pos += 180;
                        break;
                    case "TimeScale":
                        pos += 29;  // Originally 58 for GGS and 29 for DBZ, but it caused an exception for GGS. This might only affect some ParticleModuleColorOverLife with TimeScale property in the Table. Will need to test for other cases.
                        break;
                    case "TimeBias":
                        pos += 29;  // When testing other cases, found that TimeBias is another factor that can break the process. This is why I believe it was initially set to 58, since TimeScale and TimeBias together makes 58. This is not the case for all cases!
                        break;
                    case "Values":
                        pos += 37;
                        foreach (var value in color.Values)
                        {
                            BitConverter.GetBytes(value).CopyTo(fileBytes, pos);
                            pos += 4;
                        }
                        pos = -1;
                        break;
                    default:
                        if (color.Values == null)
                            return -1;
                        else
                            return -2;
                }
                return pos;
            }
            catch (Exception)
            {
                return -2;
            }
        }
        public static int AddData(this ColorOverLife color, byte[] fileBytes, int pos, Map<string, int> NameTable, Game game)
        {
            try
            {
                // Write data depending on type
                switch (NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])])
                {
                    case "MinValue":
                        color.MinValue = BitConverter.ToSingle(fileBytes[(pos + 25)..(pos + 29)]);
                        pos += 29;
                        break;
                    case "MaxValue":
                        color.MaxValue = BitConverter.ToSingle(fileBytes[(pos + 25)..(pos + 29)]);
                        pos += 29;
                        break;
                    case "MinValueVec":
                        var minrgb = new RGB()
                        {
                            R = BitConverter.ToSingle(fileBytes[(pos + 49)..(pos + 53)]),
                            G = BitConverter.ToSingle(fileBytes[(pos + 53)..(pos + 57)]),
                            B = BitConverter.ToSingle(fileBytes[(pos + 57)..(pos + 61)])
                        };
                        color.MinValueVec = minrgb;
                        pos += 61;
                        break;
                    case "MaxValueVec":
                        var maxrgb = new RGB()
                        {
                            R = BitConverter.ToSingle(fileBytes[(pos + 49)..(pos + 53)]),
                            G = BitConverter.ToSingle(fileBytes[(pos + 53)..(pos + 57)]),
                            B = BitConverter.ToSingle(fileBytes[(pos + 57)..(pos + 61)])
                        };
                        color.MaxValueVec = maxrgb;
                        pos += 61;
                        break;
                    case "Distribution":
                        // Different offsets for different games
                        if (game == Game.GGS)
                            pos += 78;
                        else if (game == Game.DBFZ)
                            pos += 180;
                        break;
                    case "TimeScale":
                        pos += 29;  // Originally 58 for GGS and 29 for DBZ, but it caused an exception for GGS. This might only affect some ParticleModuleColorOverLife with TimeScale property in the Table.
                        break;
                    case "TimeBias":
                        pos += 29;  // When testing other cases for GGS, found that TimeBias is another factor that can break the process. This is why I believe TimeScale was initially set to 58 for GGST, since TimeScale and TimeBias together makes 58.
                        break;
                    case "Values":
                        pos += 37;
                        var stop = new byte[] { (byte)NameTable.Forward["Op"], 0x00, 0x00, 0x00 };
                        if (game == Game.DBFZ)
                            stop = new byte[] { (byte)NameTable.Forward["None"], 0x00, 0x00, 0x00 };
                        var values = new List<float>();
                        while (!ByteArrayCompare(fileBytes[pos..(pos + 4)], stop))
                        {
                            values.Add(BitConverter.ToSingle(fileBytes[pos..(pos + 4)]));
                            pos += 4;
                        }
                        color.Values = values;
                        pos = -1;
                        break;
                    default:
                        if (color.Values == null)
                            return -1;
                        else
                            return -2;
                }
                return pos;
            }
            catch (Exception)
            {
                return -2;
            }
        }
        public static int AddData(this VectorColor color, byte[] fileBytes, int pos, Map<string, int> NameTable, Game game, ref bool finished)
        {
            try
            {
                // Write data depending on type
                switch (NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])])
                {
                    case "Name":
                        pos += 25;
                        color.name = NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])];
                        pos += 86;
                        break;
                    case "ParameterName":
                        pos += 25;
                        color.name = NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])];
                        pos += 8;
                        break;
                    case "ParameterValue":
                        pos += 8;
                        break;
                    case "StructProperty":
                        pos += 41;
                        color.offset = pos;
                        color.R = BitConverter.ToSingle(fileBytes[pos..(pos + 4)]);
                        pos += 4;
                        color.G = BitConverter.ToSingle(fileBytes[pos..(pos + 4)]);
                        pos += 4;
                        color.B = BitConverter.ToSingle(fileBytes[pos..(pos + 4)]);
                        pos += 4;
                        color.A = BitConverter.ToSingle(fileBytes[pos..(pos + 4)]);
                        if (game == Game.GGS)
                            pos += 126;
                        else if (game == Game.DBFZ)
                            pos += 77;
                        finished = true;
                        break;
                    default:
                        return -1;
                }
                return pos;
            }
            catch (Exception)
            {
                return -2;
            }
        }
        public static int AddData(this Scalar scalar, byte[] fileBytes, int pos, Map<string, int> NameTable, Game game, ref bool finished)
        {
            try
            {
                // Write data depending on type
                switch (NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])])
                {
                    case "Name":
                        pos += 25;
                        scalar.name = NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])];
                        pos += 86;
                        break;
                    case "ParameterName":
                        pos += 25;
                        scalar.name = NameTable.Reverse[BitConverter.ToInt32(fileBytes[pos..(pos + 4)])];
                        pos += 8;
                        break;
                    case "ParameterValue":
                        pos += 8;
                        break;
                    case "FloatProperty":
                        pos += 17;
                        scalar.offset = pos;
                        scalar.value = BitConverter.ToSingle(fileBytes[pos..(pos + 4)]);
                        if (game == Game.GGS)
                            pos += 126;
                        else if (game == Game.DBFZ)
                            pos += 77;
                        finished = true;
                        break;
                    default:
                        return -1;
                }
                return pos;
            }
            catch (Exception)
            {
                return -2;
            }
        }
        static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
        {
            return a1.SequenceEqual(a2);
        }
    }
}
