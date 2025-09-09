using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    internal class File
    {
        private const int _fileHeaderOffset = 100;
        private const int _pageHeaderOffset = 8;

        private FileStream _databaseFile;
        internal readonly string path;
        internal readonly short pageSize;
        internal int TableCount { get; private set; }


        internal File(string path)
        {
            try
            {
                this.path = path;
                _databaseFile = System.IO.File.OpenRead(path);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            int offset = 16;
            byte[] buffer = GetBytes(offset, 2);
            pageSize = ReadInt16BigEndian(buffer);

            buffer = GetBytes(_fileHeaderOffset + 3, 2);
            TableCount = ReadInt16BigEndian(buffer);
        }

        internal byte[] GetBytes(int offset, int length)
        {
            byte[] buffer = new byte[length];
            _databaseFile.Seek(offset, SeekOrigin.Begin);
            _databaseFile.ReadExactly(buffer, 0, length);
            return buffer;
        }

        internal byte ParseByte(int offset)
        {
            byte[] buffer = new byte[1];
            _databaseFile.Seek(offset, SeekOrigin.Begin);
            _databaseFile.ReadExactly(buffer, 0, 1);
            return (byte)buffer[0];
        }

        internal ushort Parse2Bytes(int offset)
        {
            byte[] buffer = new byte[2];

            _databaseFile.Seek(offset, SeekOrigin.Begin);
            _databaseFile.ReadExactly(buffer, 0, 2);
            return ReadUInt16BigEndian(buffer);
        }
        
        internal int Parse4Bytes

    }
}
