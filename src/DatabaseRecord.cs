using System.Runtime.CompilerServices;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    /* Record format: record starts from the offset specified in leaf page's pointer array
     * 
     * Record contents:
     * - Pointer to left child page (interior pages only): 4 bytes
     * - Number of bytes in the record: VarInt (counted after this VarInt, includes overflow) (leaf pages only)
     * - RowID: VarInt (table trees only)
     * - Payload: described further (On all pages except internal pages of table trees)
     * - First Overflow Page Number: 4 bytes (all pages except internal pages of table trees)
     * 
     * Payload:
     * 
     * - Header: 
     * - - VarInt specifying the size of the header in bytes, including itself
     * - - A number of VarInts, corresponding to number of fields (columns) in the record,
     * - - each of them is a SerialType, specifying the content's type and length
     * - Payload:
     * - - The content itself
     */
    internal class DatabaseRecord
    {
        private readonly ulong _recordLength;
        internal readonly ulong rowID;
        internal List<object> payload;
        internal readonly int overflowPage;

        internal enum SerialType
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

        internal DatabaseRecord(DatabaseFile databaseFile, int currentOffset)
        {
            payload = new List<object>();
            _recordLength = VarInt.Parse(databaseFile, ref currentOffset);
            rowID = VarInt.Parse(databaseFile, ref currentOffset);

            int headerStart = currentOffset;
            ulong headerSize = VarInt.Parse(databaseFile, ref currentOffset);

            List<(SerialType, int)> columnTypeSize = new();
            int headerEnd = headerStart + (int)headerSize;
            List<ulong> serialTypes = [];
            while (currentOffset < headerEnd)
            {
                ulong varInt = VarInt.Parse(databaseFile, ref currentOffset);
                serialTypes.Add(varInt);
            }

            foreach (ulong type in serialTypes)
            {
                int contentByteLength = 0;
                switch (type)
                {
                    case 0:
                        payload.Add(null!);
                        break;
                    case 1:
                        contentByteLength = 1;
                        payload.Add((byte)contentBuffer[0]);
                        break;
                    case SerialType.Int16:
                        payload.Add(ReadInt16BigEndian(contentBuffer));
                        break;
                    case SerialType.Int24:  // there's no stram reader for 24 bit int, so need to add one more empty byte to have 32 bits
                        Stack<byte> fillTo32Buffer = new Stack<byte>(contentBuffer);
                        fillTo32Buffer.Push((byte)0);
                        payload.Add(ReadInt32BigEndian([.. fillTo32Buffer]));
                        break;
                    case SerialType.Int32:
                        payload.Add(ReadInt32BigEndian(contentBuffer));
                        break;
                    case SerialType.Int48:  // again, no 48 bit reader
                        Stack<byte> fillTo64Buffer = new Stack<byte>(contentBuffer);
                        while (fillTo64Buffer.Count < 8) fillTo64Buffer.Push((byte)0);
                        payload.Add(ReadInt64BigEndian([.. fillTo64Buffer]));
                        break;
                    case SerialType.Int64:
                        payload.Add(ReadInt64BigEndian(contentBuffer));
                        break;
                    case SerialType.Float64:
                        payload.Add(ReadDoubleBigEndian(contentBuffer));
                        break;
                    case SerialType.Int_0:
                        payload.Add(0);
                        break;
                    case SerialType.Int_1:
                        payload.Add(1);
                        break;
                    case SerialType.Reserved:
                        throw new NotImplementedException("Figure out what reserved variable does");
                        break;
                    case SerialType.BLOB:
                        payload.Add(contentBuffer);
                        break;
                    case SerialType.String:
                        payload.Add(Encoding.Default.GetString(contentBuffer));
                        break;

                }
                currentOffset += contentByteLength;
            }
        }
        private static (SerialType, int) ParseRecordHeader(ulong serialType)
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
