using System;
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

            byte[] buffer = new byte[2]; // most of the data chunks are 2 bytes long

            databaseFile.Seek(_fileHeaderOffest, SeekOrigin.Begin);
            databaseFile.Read(buffer, 0, 2);
            _pageSize = ReadUInt16BigEndian(buffer);

            databaseFile.Seek(_firstPageOffset + 3, SeekOrigin.Begin);
            databaseFile.Read(buffer, 0, 2);
            TableCount = ReadUInt16BigEndian(buffer);

            cellPtrArray = new int[TableCount];
            int arrayStartOffset = _firstPageOffset + _pageHeaderOffset;
            int arrayIndexOffset = 0;
            for (int i = 0; i < TableCount; i++)
            {
                arrayIndexOffset = i * 2; // 2 bytes per array element
                databaseFile.Seek(arrayStartOffset + arrayIndexOffset, SeekOrigin.Begin);
                databaseFile.Read(buffer, 0, 2);
                cellPtrArray[i] = ReadUInt16BigEndian(buffer);
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
    }
}
