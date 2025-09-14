using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_sqlite.src
{
    internal record SelectNode(List<string> columns, List<string> tables);
    internal static class ASTBuilder
    {
        internal static SelectNode ParseSelectQuery(List<Token> tokens)
        {
            List<string> columns = [];
            List<string> tables = [];
            int tokenCount = 0;
            Token currentToken = tokens[tokenCount];
            if (currentToken.type != TokenType.Select) throw new FormatException("Select query should start with SELECT keyword");
            while (tokens[++tokenCount].type != TokenType.From)
            {
                currentToken = tokens[tokenCount];
                if (currentToken.type == TokenType.From) break;
                if (currentToken.type != TokenType.Identifier && currentToken.type != TokenType.Comma)
                    throw new FormatException("Query format is invalid");
                if (currentToken.type == TokenType.Identifier) columns.Add(currentToken.value);
            }
            while (++tokenCount < tokens.Count)
            {
                currentToken = tokens[tokenCount];
                if (currentToken.type != TokenType.Identifier && currentToken.type != TokenType.Comma)
                    throw new FormatException("Query format is invalid");
                if (currentToken.type == TokenType.Identifier) tables.Add(currentToken.value);
            }
            return new SelectNode(columns, tables);            
        }
    }
}
