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
        internal List<object> recordColumns;
        internal Record(int currentOffset)
        {
            recordColumns = new List<object>();
            ulong recordSize = Utils.ParseVarInt(ref currentOffset);
            _ = Utils.ParseVarInt(ref currentOffset);  // rowId for now isn't necessary

            int headerStartOffset = currentOffset;
            ulong headerSize = Utils.ParseVarInt(ref currentOffset);

            List<(SerialType, int)> columnTypeSize = new();
            int endOfHeaderOffset = headerStartOffset + (int)headerSize;        
            while (currentOffset < endOfHeaderOffset)
            {
                ulong columnHeaderVarInt = Utils.ParseVarInt(ref currentOffset);
                (SerialType columnType, int columnSize) columnHeader = ParseColumnHeader(columnHeaderVarInt);
                columnTypeSize.Add(columnHeader);
            }

            foreach ((SerialType contentType, int contentSize) in columnTypeSize)
            {
                byte[] contentBuffer = new byte[contentSize];
                MetaData.databaseFile.Seek(currentOffset, SeekOrigin.Begin);
                MetaData.databaseFile.ReadExactly(contentBuffer, 0, contentSize);
                if (contentType == SerialType.String)
                {
                    recordColumns.Add(System.Text.Encoding.Default.GetString(contentBuffer));
                }
                else
                {
                    recordColumns.Add(contentBuffer);
                }
                currentOffset += contentSize;
            }
        }


        internal static (SerialType, int) ParseColumnHeader(ulong serialType)
        {
            if (serialType >= 12 && serialType % 2 == 0)
            {
                int contentSize = ((int)serialType - 12) / 2;
                return (SerialType.BLOB, contentSize);
            }
            if (serialType >= 13 && serialType % 2 == 1)
            {
                int contentSize = ((int)serialType - 13) / 2;
                return (SerialType.String, contentSize);
            }
            switch (serialType)
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
