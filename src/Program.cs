using System.Reflection;
using static System.Buffers.Binary.BinaryPrimitives;
namespace codecrafters_sqlite.src
{
    class Program
    {
        public static int Main(string[] args)
        {
            // Parse arguments
            var (path, command) = args.Length switch
            {
                0 => throw new InvalidOperationException("Missing <database path> and <command>"),
                1 => throw new InvalidOperationException("Missing <command>"),
                _ => (args[0], args[1])
            };

            MetaData metaData = new MetaData(path);

            switch (command)
            {
                case ".dbinfo":
                    // You can use print statements as follows for debugging, they'll be visible when running tests.
                    Console.Error.WriteLine("Logs from your program will appear here!");
                    Console.WriteLine($"database page size: {metaData.PageSize}");
                    Console.WriteLine($"number of tables: {metaData.TableCount}");
                    break;
                case ".tables":
                    foreach (Schema.Table table in metaData.schema.tables)
                    {
                        if (table.TableName.Contains("sqlite_")) continue; // don't display inner system related tables
                        Console.WriteLine(table.TableName);
                    }
                    break;
                case "SELECT COUNT(*) FROM apples": // proper SQL parser is to be implemented in the future
                    int rootPageNumber = metaData.schema.tables
                        .Where(table => table.TableName == "apples")
                        .Select(table => table.RootPage)
                        .FirstOrDefault();
                    int pageStartOffset = (rootPageNumber - 1) * metaData.PageSize;
                    pageStartOffset += 3;
                    int cellNumber = metaData.databaseFile.ReadTwoBytes(pageStartOffset);
                    Console.WriteLine(cellNumber);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid command: {command}");
            }
            return 0;
        }

    }
}







// Parse command and act accordingly
