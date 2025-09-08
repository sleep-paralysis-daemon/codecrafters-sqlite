using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    internal class DatabaseFile
    {
        private FileStream _databaseFile;
        private string _path;

        internal DatabaseFile(string path)
        {
            try
            {
                this._path = path;
                _databaseFile = File.OpenRead(path);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        internal byte[] ReadBytes(int offset, int length)
        {
            byte[] buffer = new byte[length];
            _databaseFile.Seek(offset, SeekOrigin.Begin);
            _databaseFile.ReadExactly(buffer, 0, length);
            return buffer;
        }

        internal int ReadTwoBytes(int offset)
        {
            byte[] buffer = new byte[2];

            _databaseFile.Seek(offset, SeekOrigin.Begin);
            _databaseFile.ReadExactly(buffer, 0, 2);
            return ReadUInt16BigEndian(buffer);
        }

    }
}
