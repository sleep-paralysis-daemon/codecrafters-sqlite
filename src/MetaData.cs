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
        private readonly string _path;
        private readonly int _pageSize;
        private int _tableCount;
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

            databaseFile.Seek(16, SeekOrigin.Begin);
            byte[] buffer = new byte[2];
            databaseFile.Read(buffer, 0, 2);
            _pageSize = ReadUInt16BigEndian(buffer);

            databaseFile.Seek(103, SeekOrigin.Begin);
            databaseFile.Read(buffer, 0, 2);
            TableCount = ReadUInt16BigEndian(buffer);
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
