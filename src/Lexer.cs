using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
[assembly: InternalsVisibleTo("tests-codecrafters-sqlite")]
namespace codecrafters_sqlite.src
{
    internal record Token(TokenType type, string value);
    internal enum TokenType
    {
        Select,
        From,
        Where,
        Join,
        Star,
        Comma,
        Semicolon,
        Identifier,
        String,
        Number
    }
    internal static class Lexer
    {
        private static List<string> keywords = ["SELECT", "FROM", "WHERE", "JOIN"];
        internal static List<Token> ParseQuery(string input)
        {
            List<Token> tokens = [];            
            input = input.ToUpper();
            int substringStart = 0;
            for (int i = 0; i < input.Length; i++)
            {
                switch (input[i])
                {
                    case '*':
                        tokens.Add(new Token(TokenType.Star, "*"));
                        substringStart = i + 1;
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.Comma, ","));
                        substringStart = i + 1;
                        break;
                    case ';':
                        tokens.Add(new Token(TokenType.Semicolon, ";"));
                        substringStart = i + 1;
                        break;
                    case ' ':
                        string substring = input[substringStart..i];
                        tokens.Add(ConvertToToken(substring));
                        substringStart = i + 1;
                        break;
                }
                if (i == input.Length - 1)
                {
                    string substring = input[substringStart..input.Length];
                    tokens.Add(ConvertToToken(substring));
                }
            }
            return tokens;
        }

        private static Token ConvertToToken(string input)
        {
            if (keywords.Contains(input)) return KeywordToToken(input);
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
            }
            throw new Exception("Keyword from keywords list wasn't parsed!");
        }
    }
}
