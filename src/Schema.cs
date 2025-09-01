
namespace codecrafters_sqlite.src
{
    /// <summary>
    /// Schema is a table that stores descriptions of other table as its rows
    /// Schema table is always on first page, pointers to its rows come right after page header.
    /// More about schema: https://www.sqlite.org/schematab.html
    /// </summary>
    internal class Schema
    {        
        internal List<Table> tables;
        internal Schema(int[] rowsPtrArray)
        {
            tables = [];
            foreach (int pointer in rowsPtrArray)
            {
                List<object> columns = Utils.ParseRecord(pointer);
                Table table = new Table(
                    (string)columns[0],
                    (string)columns[1],
                    (string)columns[2],
                    (int)(byte)columns[3],
                    (string)columns[4]
                    );
                tables.Add(table);
            }
        }
        internal readonly record struct Table
            (string Type, string Name, string TableName, int RootPage, string SQL);
    }
}
