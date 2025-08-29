using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Buffers.Binary.BinaryPrimitives;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("test-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    public class MetaData
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
        /// Slice VarInt (1-9 bytes) from starting offset
        /// </summary>
        /// <param name="varIntOffset"> Starting offset from file's beginning </param>
        /// <returns> A tuple with VarInt and next byte offset </returns>
        internal (byte[], int) SliceOffVarInt(int varIntOffset)
        {
            int byteCount = 1;
            byte[] buffer = new byte[8];
            while (true)
            {
                databaseFile.Seek(varIntOffset + byteCount - 1, SeekOrigin.Begin);
                databaseFile.Read(buffer, 0, 1);
                byteCount++;
                if (buffer[0] <= 0x7F || byteCount == 9) // bytes 0111_1111 and lower: most significant bit = 0 means there's no more bytes in VarInt.
                    break;                              // max number of VarInt bytes is 9
            }
            databaseFile.Seek(varIntOffset, SeekOrigin.Begin);
            databaseFile.Read(buffer, 0, byteCount);
            int nextByteOffset = varIntOffset + byteCount;
            return (buffer, nextByteOffset);
        }


        internal ulong ConvertVarInt(byte[] bytes)
        {
            byte[] buffer = new byte[1];
            BitArray bits = new BitArray(bytes);
            Stack<bool> VarIntBits = new Stack<bool>();
            for (int i = 0; i < bits.Count; i++)
            {
                if (i % 8 != 0) // transfer alls bit except first bits of every byte
                {
                    VarIntBits.Push(bits[i]);
                }
            }
            Stack<byte> cleanBytes = new Stack<byte>();
            Stack<bool> cleanBits = new Stack<bool>();
            while (VarIntBits.Count > 0)
            {
                cleanBits.Push(VarIntBits.Pop());
                if (cleanBits.Count == 8)
                {
                    cleanBytes.Push(ConvertBitsToByte(cleanBits.ToArray()));
                    cleanBits.Clear();
                }
            }
            if (cleanBits.Count > 0)
            {
                while (cleanBits.Count < 8) cleanBits.Push(false);
                cleanBytes.Push(ConvertBitsToByte(cleanBits.ToArray()));
            }
            while (cleanBytes.Count < 8)
            {
                cleanBytes.Push((byte)0);
            }
            ReadOnlySpan<byte> result = cleanBytes.ToArray();
            return ReadUInt64BigEndian(result);
        }

        /// <summary>
        /// Glues together 8 bits into a byte
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        private byte ConvertBitsToByte(bool[] bits)
        {
            //if (bits.Length != 8) throw new ArgumentOutOfRangeException("Need exactly 8 bits for a byte");
            byte result = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    byte temp = (byte)(1 << (7 - i));
                    result |= (byte)(1 << (7 - i));
                }                    
            }
            return result;
        }
    }
}
