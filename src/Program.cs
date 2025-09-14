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

            File dbFile = new(path);

            switch (command)
            {
                case ".dbinfo":
                    // You can use print statements as follows for debugging, they'll be visible when running tests.
                    Console.WriteLine($"database page size: {dbFile.PageSize}");
                    Console.WriteLine($"number of tables: {dbFile.TableCount}");
                    break;
                case ".tables":
                    foreach (TableSchema table in dbFile.Tables)
                    {
                        if (table.TableName.Contains("sqlite_")) continue; // don't display inner system related tables
                        Console.WriteLine(table.SQL);
                    }
                    break;
                case string s when (s.Contains("SELECT")):
                    List<Token> tokens = Lexer.ParseQuery(command);
                    SelectNode node = ASTBuilder.ParseSelectQuery(tokens);
                    if (node.tables.Count > 1) throw new NotImplementedException("Parsing multiple tables doesn't work yet");
                    int rootPage = 0;
                    string SQL = "";
                    foreach (TableSchema table in dbFile.Tables)
                    {
                        if (String.Equals(table.Name, node.tables[0], StringComparison.OrdinalIgnoreCase))
                        {
                            rootPage = table.RootPage;
                            SQL = table.SQL;
                        }
                    }
                    Console.WriteLine(SQL);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid command: {command}");
            }
            return 0;
        }

    }
}
