using System;
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
                        // Different offsets for different games
                        if (game == Game.GGS)
                            pos += 58;
                        else if (game == Game.DBFZ)
                            pos += 29;
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
                        // Different offsets for different games
                        if (game == Game.GGS)
                            pos += 58;
                        else if (game == Game.DBFZ)
                            pos += 29;
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
                        return -2;
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
