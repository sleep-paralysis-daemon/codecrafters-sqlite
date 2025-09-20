using System.Text;
using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]
namespace codecrafters_sqlite.src
{
    internal record Token(TokenType type, string value);
    internal enum TokenType
    {
        Create,
        Table,
        Select,
        From,
        Where,
        Join,
        Star,
        Comma,
        Semicolon,
        Identifier,
        String,
        Number,
        Escape,
        LeftPar,
        RightPar,
    }

    internal static class Lexer
    {
        private static List<string> keywords = ["SELECT", "FROM", "WHERE", "JOIN", "CREATE", "TABLE"];
        private enum LexerState
        {
            Init,
            Identifier,
            String,
            Numeric,
            Comment
        }
        internal static List<Token> GetTokens(string query)
        {
            LexerState currentState = LexerState.Init;
            StringBuilder currentToken = new();
            List<Token> tokens = [];
            foreach (char c in query)
            {
                if (Char.IsDigit(c))
                {
                    if (currentState == LexerState.Init)
                    {
                        currentState = LexerState.Numeric;
                    }
                    currentToken.Append(c);
                }
                else if (Char.IsLetter(c))
                {
                    if (currentState == LexerState.Numeric)
                    {
                        throw new FormatException("Identifier cannot start with a number!");
                    }
                    else if (currentState == LexerState.Init)
                    {
                        currentState = LexerState.Identifier;
                    }
                    currentToken.Append(c);
                }
                else if (c == ' ')
                {
                    if (currentToken.Length == 0) continue;
                    string result = currentToken.ToString();
                    if (currentState == LexerState.Numeric)
                    {
                        
                    }
                    tokens.Add(ParseIdentifier(result));
                    currentToken.Clear();
                    currentState = LexerState.Init;
                }
                else if (c == '*')
                {
                    if (currentState == LexerState.Init)
                        tokens.Add(new Token(TokenType.Star, "*"));
                    else if (currentState != LexerState.String &&
                             currentState != LexerState.Comment)
                    {
                        throw new FormatException("* is invalid identifier/number character");
                    }
                    else currentToken.Append(c);
                }
            }            
            return tokens;
        }

        private static Token ParseIdentifier(string input)
        {
            foreach (string keyword in keywords)
            {
                if (string.Equals(input, keyword, StringComparison.InvariantCultureIgnoreCase)) 
                    return KeywordToToken(input);
            }
            if (input[0] == '\'')
            {
                if (input[input.Length - 1] != '\'') throw new Exception("Matching \' not found");
                return new Token(TokenType.String, input[1..^1]);
            }
            if (input[0] == '\"')
            {
                if (input[input.Length - 1] != '\"') throw new Exception("Matching \" not found");
                return new Token(TokenType.String, input[1..^1]);
            }
            bool isNumber = true;
            foreach (char c in input)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    isNumber = false;
                    break;
                }
            }
            if (isNumber) return new Token(TokenType.Number, input);
            if (!char.IsDigit(input[0])) return new Token(TokenType.Identifier, input);
            throw new ArgumentException("Couldn't find appropriate token type for input");
        }

        private static Token KeywordToToken(string input)
        {
            input = input.ToUpper();
            switch(input)
            {
                case "SELECT":
                    return new Token(TokenType.Select, "SELECT");
                case "FROM":
                    return new Token(TokenType.From, "FROM");
                case "WHERE":
                    return new Token(TokenType.Where, "WHERE");
                case "JOIN":
                    return new Token(TokenType.Join, "JOIN");
                case "CREATE":
                    return new Token(TokenType.Create, "CREATE");
                case "TABLE":
                    return new Token(TokenType.Create, "TABLE");
            }
            throw new Exception("Keyword from keywords list wasn't parsed!");
        }
    }
}
