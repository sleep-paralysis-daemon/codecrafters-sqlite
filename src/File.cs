using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    internal class File
    {
        private const int _MagicStringOffset = 16;
        private const int _fileHeaderOffset = 100;
        private const int _pageHeaderOffset = 8;

        private FileStream _databaseFile;
        internal readonly string path;
        internal ushort PageSize { get; private set; }
        internal ushort TableCount { get; private set; }
        internal TableSchema[] Tables { get; private set; }

        internal File(string path)
        {
            this.path = path;
            try
            {
                _databaseFile = System.IO.File.OpenRead(path);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            PageSize = Parse2Bytes(_MagicStringOffset);
            TableCount = Parse2Bytes(_fileHeaderOffset + 3);
            Tables = new TableSchema[TableCount];
            int arrayStartOffset = _fileHeaderOffset + _pageHeaderOffset; // schema pointer array is always located on the first page, right after the page header
            int arrayRecordOffset = 0;
            for (int i = 0; i < TableCount; i++)
            {
                arrayRecordOffset = i * 2; // 2 bytes per record pointer
                int tableSchemaPointer = this.Parse2Bytes(arrayStartOffset + arrayRecordOffset);
                Record schemaRecord = new Record(this, tableSchemaPointer);
                Tables[i] = new TableSchema(this, schemaRecord);
            }

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
        
        internal uint Parse4Bytes(int offset)
        {
            byte[] buffer = new byte[4];
            _databaseFile.Seek(offset, SeekOrigin.Begin);
            _databaseFile.ReadExactly(buffer, 0, 4);
            return ReadUInt32BigEndian(buffer);
        }

    }
}
