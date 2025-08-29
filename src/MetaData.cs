using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Buffers.Binary.BinaryPrimitives;

namespace codecrafters_sqlite.src
{
    internal class MetaData
    {
        private const int _fileHeaderOffest = 16;
        private const int _firstPageOffset = 100;
        private const int _pageHeaderOffset = 8;

        private readonly string _path;
        private readonly int _pageSize;
        private int _tableCount;
        private int[] cellPtrArray;
        internal FileStream databaseFile;

        internal MetaData(string path)
        {
            try
            {
                databaseFile = File.OpenRead(path);
                this._path = path;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            _pageSize = ReadTwoBytes(_fileHeaderOffest);
            TableCount = ReadTwoBytes(_firstPageOffset + 3);

            cellPtrArray = new int[TableCount];
            int arrayStartOffset = _firstPageOffset + _pageHeaderOffset;
            int arrayIndexOffset = 0;
            for (int i = 0; i < TableCount; i++)
            {
                arrayIndexOffset = i * 2; // 2 bytes per array element
                cellPtrArray[i] = ReadTwoBytes(arrayIndexOffset + arrayStartOffset);
            }            
        }
        internal int TableCount
        {
            get { return _tableCount; }
            set { _tableCount = value; }
        }
        internal int PageSize
        {
            get { return _pageSize; }
        }

        private int ReadTwoBytes(int offset)
        {
            byte[] buffer = new byte[2];
            databaseFile.Seek(offset, SeekOrigin.Begin);
            databaseFile.Read(buffer, 0, 2);
            return ReadUInt16BigEndian(buffer);
        }

        /// <summary>
        /// Slice off the VarInt from the starting offset
        /// VarInt is an unsigned integer 1-9 bytes long
        /// </summary>
        /// <param name="varIntOffset">
        /// Starting offset from file's beginning
        /// </param>
        /// <returns>
        /// A tuple with VarInt and next byte offset
        /// </returns>
        private (byte[], int) SliceOffVarInt(int varIntOffset)
        {
            int byteCount = 0;
            byte[] buffer = new byte[1];
            while (true)
            {
                databaseFile.Seek(varIntOffset + byteCount, SeekOrigin.Begin);
                databaseFile.Read(buffer, 0, 1);
                byteCount++;
                if (buffer[0] <= 0x7F) // bytes 0111_1111 and lower: most significant bit = 0 means there's no more bytes in VarInt
                    break;
            }
            buffer = new byte[byteCount + 1];
            databaseFile.Seek(varIntOffset, SeekOrigin.Begin);
            databaseFile.Read(buffer, 0, byteCount + 1);
        }

        private ulong ConvertVarInt(int offset)
        {
            byte[] buffer = new byte[1];
            BitArray bits;
            Stack<bool> reconstructedInt64 = new Stack<bool>();
            while (true)
            {
                databaseFile.Seek(offset, SeekOrigin.Begin);
                offset += 1;
                databaseFile.Read(buffer, 0, 1);
                bits = new BitArray(buffer);
                for (int i = 1; i < bits.Count; i++)
                {
                    reconstructedInt64.Push(bits[i]);
                }
                if (bits[0] == false) break;    // if most significant bit of the byte is 0, then there's no more bytes in varint
            }
            bool[] bitBuffer = new bool[8];
            int bitBufferIndex = 0;
            Stack<byte> resultBytes = new Stack<byte>();
            foreach (bool bit in reconstructedInt64)
            {
                bitBuffer[bitBufferIndex] = bit;
                bitBufferIndex++;
                if (bitBufferIndex == 8)
                {
                    Array.Reverse(bitBuffer);
                    resultBytes.Push(ConvertBitsToByte(bitBuffer));
                    bitBufferIndex = 0;
                    bitBuffer = new bool[8];
                }
            }
            if (bitBufferIndex != 0)
            {
                Array.Reverse(bitBuffer);
                resultBytes.Push(ConvertBitsToByte(bitBuffer));
            }
            ReadOnlySpan<byte> result = resultBytes.ToArray();
            return ReadUInt64BigEndian(result);
        }

        private byte ConvertBitsToByte(bool[] bits)
        {
            byte result = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    result |= (byte)(1 << (7 - i));
            }
            return result;
        }
    }
}
