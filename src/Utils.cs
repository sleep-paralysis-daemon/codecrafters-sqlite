using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Buffers.Binary.BinaryPrimitives;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    internal static class Utils
    {
        internal static int ReadTwoBytes(int offset)
        {
            byte[] buffer = new byte[2];
            
            MetaData.databaseFile.Seek(offset, SeekOrigin.Begin);
            MetaData.databaseFile.Read(buffer, 0, 2);
            return ReadUInt16BigEndian(buffer);
        }

        /// <summary>
        /// Slice VarInt (1-9 bytes) from starting offset
        /// </summary>
        /// <param name="varIntOffset"> Starting offset from file's beginning </param>
        /// <returns> A tuple with VarInt and next byte offset</returns>
        internal static (ulong, int) SliceOffVarInt(int varIntOffset)
        {
            int byteCount = 0;
            byte[] byteBuffer = new byte[1];
            while (true)
            {
                MetaData.databaseFile.Seek(varIntOffset + byteCount, SeekOrigin.Begin);
                MetaData.databaseFile.Read(byteBuffer, 0, 1);
                byteCount++;
                if (byteBuffer[0] <= 0x7F || byteCount == 9) // bytes 0111_1111 and lower: most significant bit = 0 means there's no more bytes in VarInt.
                    break;                                  // max number of VarInt bytes is 9
            }
            byte[] outputBuffer = new byte[byteCount];
            MetaData.databaseFile.Seek(varIntOffset, SeekOrigin.Begin);
            MetaData.databaseFile.Read(outputBuffer, 0, byteCount);
            ulong result = ConvertVarInt(byteBuffer);
            int nextByteOffset = varIntOffset + byteCount;
            return (result, nextByteOffset);
        }


        internal static ulong ConvertVarInt(byte[] bytes)
        {
            Stack<bool> strippedBits = new();
            int processedBytes = 0;
            foreach (byte currentByte in bytes)
            {
                bool[] bools = ConvertByteToBits(currentByte);
                processedBytes++;
                for (int i = 0; i < bools.Length; i++)
                {
                    if (i == 0 && processedBytes < 9) continue;  // skip most significant bit for all but 9th byte
                    else strippedBits.Push(bools[i]);
                }
            }
            Stack<bool> reassembledByte = new();
            Stack<byte> resultBytes = new();
            while (strippedBits.Count > 0)
            {
                reassembledByte.Push(strippedBits.Pop());
                if (reassembledByte.Count == 8)
                {
                    resultBytes.Push(ConvertBitsToByte(reassembledByte.ToArray()));
                    reassembledByte.Clear();
                }
            }
            if (reassembledByte.Count > 0)
            {
                while (reassembledByte.Count < 8)
                    reassembledByte.Push(false);
                resultBytes.Push(ConvertBitsToByte(reassembledByte.ToArray()));
            }
            while (resultBytes.Count < 8)
            {
                resultBytes.Push(0x00);
            }
            ReadOnlySpan<byte> result = resultBytes.ToArray();
            return ReadUInt64BigEndian(result);
        }

        /// <summary>
        /// Glues together 8 bits into a byte
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>

        private static bool[] ConvertByteToBits(byte input)
        {
            bool[] output = new bool[8];
            byte mask;
            for (int i = 0; i < 8; i++)
            {
                mask = 0b_0000_0001;
                mask = (byte)(mask << (7 - i));
                output[i] = ((input & mask) != 0);
            }
            return output;
        }
        private static byte ConvertBitsToByte(bool[] bits)
        {
            if (bits.Length != 8) throw new ArgumentOutOfRangeException("Need exactly 8 bits for a byte");
            byte result = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    result |= (byte)(1 << (7 - i));
                }
            }
            return result;
        }
    }
}
