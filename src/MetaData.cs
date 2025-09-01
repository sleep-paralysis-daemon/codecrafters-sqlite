using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Buffers.Binary.BinaryPrimitives;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    public class MetaData
    {
        internal const int magicStringOffset = 16;
        internal const int fileHeaderOffset = 100;
        internal const int pageHeaderOffset = 8;

        private readonly string _path;
        private readonly int _pageSize;
        private int _tableCount;
        internal Schema schema;
        internal static FileStream databaseFile;

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

            _pageSize = Utils.ReadTwoBytes(magicStringOffset);
            TableCount = Utils.ReadTwoBytes(fileHeaderOffset + 3);

            int[] cellPtrArray = new int[TableCount];
            int arrayStartOffset = fileHeaderOffset + pageHeaderOffset;
            int arrayIndexOffset = 0;
            for (int i = 0; i < TableCount; i++)
            {
                arrayIndexOffset = i * 2; // 2 bytes per array element
                cellPtrArray[i] = Utils.ReadTwoBytes(arrayIndexOffset + arrayStartOffset);
            }
            schema = new Schema(cellPtrArray);
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
