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
     * - Payload Header: 
     * - 1. A VarInt specifying the size of the header in bytes, including itself
     * - 2. A number of VarInts, corresponding to number of fields (columns) in the record,
     * - - - each of them is a SerialType, specifying the content's type and length
     * - Payload:
     * - - The content itself
     */
    internal class Record
    {
        private readonly ulong _recordLength;
        internal readonly ulong rowID;
        internal List<object> payload;
        internal readonly int overflowPage;

        internal Record(File dbFile, int currentOffset)
        {
            payload = new List<object>();

            _recordLength = VarInt.Parse(dbFile, ref currentOffset);
            rowID = VarInt.Parse(dbFile, ref currentOffset);

            int headerStart = currentOffset;
            ulong headerSize = VarInt.Parse(dbFile, ref currentOffset);
            int headerEnd = headerStart + (int)headerSize;

            List<ulong> serialTypes = [];
            while (currentOffset < headerEnd)
            {
                ulong varInt = VarInt.Parse(dbFile, ref currentOffset);
                serialTypes.Add(varInt);
            }

            foreach (ulong type in serialTypes)
            {
                int contentByteLength = 0;
                byte[] contentBuffer;
                switch (type)
                {
                    case 0:
                        payload.Add(null!);
                        break;
                    case 1:
                        contentByteLength = 1;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add((byte)contentBuffer[0]);
                        break;
                    case 2:
                        contentByteLength = 2;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add(ReadInt16BigEndian(contentBuffer));
                        break;
                    case 3:  // there's no stram reader for 24 bit int, so need to add one more empty byte to have 32 bits
                        contentByteLength = 3;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        Stack<byte> fillTo32Buffer = new Stack<byte>(contentBuffer);
                        fillTo32Buffer.Push((byte)0);
                        payload.Add(ReadInt32BigEndian([.. fillTo32Buffer]));
                        break;
                    case 4:
                        contentByteLength = 4;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add(ReadInt32BigEndian(contentBuffer));
                        break;
                    case 5:  // again, no 48 bit reader
                        contentByteLength = 6;
                        contentBuffer = dbFile.GetBytes(contentByteLength, contentByteLength);
                        Stack<byte> fillTo64Buffer = new Stack<byte>(contentBuffer);
                        while (fillTo64Buffer.Count < 8) fillTo64Buffer.Push((byte)0);
                        payload.Add(ReadInt64BigEndian([.. fillTo64Buffer]));
                        break;
                    case 6:
                        contentByteLength = 8;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add(ReadInt64BigEndian(contentBuffer));
                        break;
                    case 7:
                        contentByteLength = 8;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add(ReadDoubleBigEndian(contentBuffer));
                        break;
                    case 8:
                        payload.Add(0);
                        break;
                    case 9:
                        payload.Add(1);
                        break;
                    case 10:
                        goto case 11;
                    case 11:
                        throw new NotImplementedException("Figure out what reserved variable does");
                    case ulong t when (t >= 12 && t % 2 == 0):
                        contentByteLength = (int)(type - 12) / 2;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add(contentBuffer);
                        break;
                    case ulong t when (t >= 13 && t % 2 == 1):
                        contentByteLength = (int)(type - 13) / 2;
                        contentBuffer = dbFile.GetBytes(currentOffset, contentByteLength);
                        payload.Add(Encoding.Default.GetString(contentBuffer));
                        break;
                }
                currentOffset += contentByteLength;
            }
        }
    }
}
