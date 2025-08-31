using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_sqlite.src
{
    enum SerialType
    {
        Null,
        Int8,
        Int16,
        Int24,
        Int32,
        Int48,
        Int64,
        Float64,
        Int_0,
        Int_1,
        Reserved,
        BLOB,
        String
    }
    internal class Record
    {
        private readonly byte[] recordBuffer;
        internal List<object> recordFields;
        internal Record(int recordOffset)
        {
            recordFields = new List<object>();
            (ulong recordSize, int rowIdOffset) = Utils.SliceOffVarInt(recordOffset);

            (_, int headerStartOffset) = Utils.SliceOffVarInt(rowIdOffset);  // rowId, for now is unnecessary

            (ulong headerSize, int recordHeaderOffset) = Utils.SliceOffVarInt(headerStartOffset);
            int headerBytesScanned = 1; // already scanned headerSize byte
            List<(SerialType, int)> columnHeaders = new();

            // gather header VarInts, each one designates the type and size of a record row
            while (headerBytesScanned < (int)headerSize)
            {
                (ulong columnHeaderValue, int nextOffset) = Utils.SliceOffVarInt(recordHeaderOffset);
                columnHeaders.Add(ParseColumnHeader(columnHeaderValue));
                headerBytesScanned += (nextOffset - recordHeaderOffset);
                recordHeaderOffset = nextOffset;
            }

            int recordBodyOffset = recordHeaderOffset;
            foreach ((SerialType contentType, int contentSize) in columnHeaders)
            {
                byte[] contentBuffer = new byte[contentSize];
                MetaData.databaseFile.Seek(recordBodyOffset, SeekOrigin.Begin);
                MetaData.databaseFile.ReadExactly(contentBuffer, 0, (int)contentSize);
                if (contentType == SerialType.String)
                {
                    recordFields.Add(System.Text.Encoding.Default.GetString(contentBuffer));
                }
                else
                {
                    recordFields.Add(contentBuffer);
                }
                recordBodyOffset += contentSize;
            }
        }


        internal static (SerialType, int) ParseColumnHeader(ulong columnSize)
        {
            if (columnSize >= 12 && columnSize % 2 == 0)
            {
                int contentSize = ((int)columnSize - 12) / 2;
                return (SerialType.BLOB, contentSize);
            }
            if (columnSize >= 13 && columnSize % 2 == 1)
            {
                int contentSize = ((int)columnSize - 13) / 2;
                return (SerialType.String, contentSize);
            }
            switch (columnSize)
            {
                case 0:
                    return (SerialType.Null, 0);
                case 1:
                    return (SerialType.Int8, 1);
                case 2:
                    return (SerialType.Int16, 2);
                case 3:
                    return (SerialType.Int24, 3);
                case 4:
                    return (SerialType.Int32, 4);
                case 5:
                    return (SerialType.Int48, 6);
                case 6:
                    return (SerialType.Int64, 8);
                case 7:
                    return (SerialType.Float64, 8);
                case 8:
                    return (SerialType.Int_0, 0);
                case 9:
                    return (SerialType.Int_1, 0);
                default:
                    return (SerialType.Reserved, -1);
            }
        }
    }
}
