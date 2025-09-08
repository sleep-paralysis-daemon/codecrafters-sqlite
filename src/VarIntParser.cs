using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]
namespace codecrafters_sqlite.src
{
    static class VarIntParser
    {
        internal static ulong Parse(DatabaseFile databaseFile, ref int varIntOffset)
        {
            int byteCount = 0;
            byte[] byteBuffer = new byte[1];
            do
            {
                byteBuffer = databaseFile.ReadBytes(varIntOffset + byteCount, byteBuffer.Length);
                byteCount++;
            } while (byteBuffer[0] > 0b_0111_1111 && byteCount < 9);

            byte[] varIntBuffer = databaseFile.ReadBytes(varIntOffset, byteCount);
            varIntOffset += byteCount;

            Stack<bool> cleanedBits = new();
            int processedBytesCount = 0;
            foreach (byte currentByte in byteBuffer)
            {
                bool[] bits = ConvertByteToBits(currentByte);
                processedBytesCount++;
                for (int i = 0; i < bits.Length; i++)
                {
                    if (i == 0 && processedBytesCount < 9) continue;  // don't add most significant bit for all but 9th byte
                    else cleanedBits.Push(bits[i]);
                }
            }

            Stack<bool> reassembledByte = new();
            Stack<byte> resultVarInt = new();
            while (cleanedBits.Count > 0)
            {
                reassembledByte.Push(cleanedBits.Pop());
                if (reassembledByte.Count == 8)
                {
                    byte newByte = ConvertBitsToByte([.. reassembledByte]);
                    resultVarInt.Push(newByte);
                    reassembledByte.Clear();
                }
            }

            if (reassembledByte.Count > 0)
            {
                while (reassembledByte.Count < 8)
                    reassembledByte.Push(false);

                byte newByte = ConvertBitsToByte([.. reassembledByte]);
                resultVarInt.Push(newByte);
            }

            while (resultVarInt.Count < 8)
            {
                resultVarInt.Push(0x00);
            }

            ReadOnlySpan<byte> result = resultVarInt.ToArray();
            return ReadUInt64BigEndian(result);
        }

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
}
