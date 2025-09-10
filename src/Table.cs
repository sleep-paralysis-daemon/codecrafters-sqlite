using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]

namespace codecrafters_sqlite.src
{
    internal class Table
    {
        internal string Type { get; private set; }
        internal string Name { get; private set; }
        internal string TableName { get; private set; }
        internal byte RootPage { get; private set; }
        internal string SQL { get; private set; }
        internal Table(File dbFile, Record schemaRecord)
        {
            try
            {
                Type = (string)schemaRecord.payload[0];
                Name = (string)schemaRecord.payload[1];
                TableName = (string)schemaRecord.payload[2];
                RootPage = (byte)schemaRecord.payload[3];
                SQL = (string)schemaRecord.payload[4];
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidCastException("Couldn't convert schema record entries! Possibly wrong record.");
            }
        }
    }
}
