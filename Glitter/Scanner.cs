/*
 * Copyright 2017 Scott MacDonald
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Glitter
{
    /// <summary>
    ///  Converts Glitter code from text into a stream of tokens.
    /// </summary>
    public class Scanner
    {
        private string _source;
        private string _filePath;
        private int _startIndex = 0;
        private int _currentIndex = 0;
        private int _lineNumber = 0;            // TODO: REMOVE.

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="source">Text to scan.</param>
        public Scanner(string source)
            : this(source, string.Empty, WhitespaceTokenPolicy.None)
        {
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="source">Text to scan.</param>
        public Scanner(string source, string filePath)
            : this(source, filePath, WhitespaceTokenPolicy.None)
        {
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="source">Text to scan.</param>
        /// <param name="whitespacePolicy">Policy for emitting whitespace tokens.</param>
        public Scanner(string source, string filePath, WhitespaceTokenPolicy whitespacePolicy)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            EmitWhitespaceTokens = (whitespacePolicy == WhitespaceTokenPolicy.Emit);
        }

        /// <summary>
        ///  Get the current token's lexeme.
        /// </summary>
        private string CurrentLexeme
        {
            get { return _source.Substring(_startIndex, _currentIndex - _startIndex); }
        }

        /// <summary>
        ///  Get or set if the scanner should emit whitespace tokens.
        /// </summary>
        public bool EmitWhitespaceTokens { get; set; } = true;

        /// <summary>
        ///  Scan the input text and return an enumerable stream of tokens.
        /// </summary>
        /// <returns>An enumerable stream of tokens.</returns>
        public IEnumerable<Token> ScanTokens()
        {
            // Keep scanning for more tokens until the end of the text string is reached.
            while (!IsAtEnd())
            {
                // Start of the next lexeme.
                _startIndex = _currentIndex;
                var nextToken = ScanNextToken();

                // Hide whitespace tokens unless specifically requested to emit whitespace.
                if (EmitWhitespaceTokens || nextToken.Type != TokenType.Whitespace)
                {
                    yield return nextToken;
                }
            }

            // At the end of the text stream.
            yield return CreateToken(TokenType.EndOfFile);
        }

        /// <summary>
        ///  Get the next token from the current position in the string.
        /// </summary>
        /// <returns>The next token.</returns>
        private Token ScanNextToken()
        {
            // TODO: Check that tokens are terminated by whitespace so we don't get weird cases like <== being OK.
            var c = AdvanceOne();

            switch (c)
            {
                case '(':
                    return CreateToken(TokenType.LeftParen);

                case ')':
                    return CreateToken(TokenType.RightParen);

                case '{':
                    return CreateToken(TokenType.LeftBrace);

                case '}':
                    return CreateToken(TokenType.RightBrace);

                case ',':
                    return CreateToken(TokenType.Comma);

                case '.':
                    return CreateToken(TokenType.Dot);

                case '-':
                    if (IsDigit(Peek()))
                    {
                        return ContinueReadingNumberToken();
                    }
                    else
                    {
                        return CreateToken(TokenType.Minus);
                    }

                case '+':
                    return CreateToken(TokenType.Plus);

                case ';':
                    return CreateToken(TokenType.Semicolon);

                case '*':
                    return CreateToken(TokenType.Star);

                case '!':
                    return CreateToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);

                case '=':
                    return CreateToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);

                case '<':
                    return CreateToken(Match('=') ? TokenType.LessEqual : TokenType.Less);

                case '>':
                    return CreateToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);

                case '/':
                    if (Match('/'))
                    {
                        return ContinueReadingLineComment();
                    }
                    else if (Match('*'))
                    {
                        return ContinueReadingBlockComment();
                    }
                    else
                    {
                        return CreateToken(TokenType.Slash);
                    }

                case '"': return ContinueReadingStringToken();                

                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    return ContinueReadingWhitespaceAndOrNewlines(c);

                default:
                    // Look for character ranges that make valid tokens before throwing an unknown exception.
                    if (IsDigit(c))
                    {
                        return ContinueReadingNumberToken();
                    }
                    else if (IsIdentifierStartChar(c))
                    {
                        return ContinueReadingIdentifierOrKeywordToken();
                    }
                    else
                    {
                        throw new UnexpectedCharacterException(c, _startIndex);
                    }
            }
        }

        /// <summary>
        ///  Read next token as an identifier or a keyword.
        /// </summary>
        /// <returns>Identifier or keyword token.</returns>
        private Token ContinueReadingIdentifierOrKeywordToken()
        {
            // Read rest of potential identifier.
            while (IsIdentifierChar(Peek()))
            {
                AdvanceOne();
            }

            var lexemeLength = _currentIndex - _startIndex;
            var name = _source.Substring(_startIndex, lexemeLength);

            // Is this a keyword?
            if (Keywords.ContainsKey(name))
            {
                return CreateToken(Keywords[name]);
            }
            else
            {
                return Token.Identifier(CurrentLexeme, name, _filePath, _lineNumber, _startIndex, lexemeLength);
            }
        }

        /// <summary>
        ///  Read next token as a number.
        /// </summary>
        /// <returns>Number token.</returns>
        private Token ContinueReadingNumberToken()
        {
            // TODO: Handle overflow / underflow with test cases.
            // Check if there is a leading minus to turn this negative.
            bool isNegative = false;

            if (Peek() == '-')
            {
                isNegative = true;
                AdvanceOne();
            }

            // TODO: Allow .123.
            // TODO: Distinguish int from float.
            while (IsDigit(Peek()))
            {
                AdvanceOne();
            }

            // Look for fractional part.
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                AdvanceOne();

                while (IsDigit(Peek()))
                {
                    AdvanceOne();
                }
            }

            // Return number.
            var lexemeLength = _currentIndex - _startIndex;
            var number = Convert.ToDouble(_source.Substring(_startIndex, lexemeLength));
            number *= (isNegative ? -1 : 1);

            return Token.Number(CurrentLexeme, number, _filePath, _lineNumber, _startIndex, lexemeLength);
        }

        /// <summary>
        ///  Read the remainder of a string token.
        /// </summary>
        /// <returns>Next token as a string.</returns>
        private Token ContinueReadingStringToken()
        {
            // TODO: Handle escaped quote value.
            // TODO: Handle other escape values (\n => real newline).

            // Read up to the string termination quote.
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n')
                {
                    _lineNumber++;
                }

                AdvanceOne();
            }

            // Check for unterminated string.
            // TODO: Report the start of the string not the end.
            if (IsAtEnd())
            {
                throw new UnterminatedStringException(_startIndex);
            }

            // Consume the closing quote.
            AdvanceOne();

            // Return a string token without the enclosing quotes.
            var lexemeLength = _currentIndex - _startIndex;
            var value = _source.Substring(_startIndex + 1, lexemeLength - 2);

            return Token.String(CurrentLexeme, value, _filePath, _lineNumber, _startIndex, lexemeLength);
        }

        /// <summary>
        ///  Continue reading whitespace until a non-whitespace character is found and merge all whitespace
        ///  into a single token.
        /// </summary>
        /// <returns>Whitespace token representing all the whitespace.</returns>
        private Token ContinueReadingWhitespaceAndOrNewlines(char firstChar)
        {
            var c = firstChar;

            // TOOD: Clean up.
            // tODO: Use IsWhitespaceOrNewline.
            // Consume the rest of the whitespace and then return a single token.
            if (c == '\n')
            {
                _lineNumber++;
            }

            c = Peek();

            while (!IsAtEnd() && (c == ' ' || c == '\r' || c == '\t' || c == '\n'))
            {
                if (c == '\n')
                {
                    _lineNumber++;
                }

                AdvanceOne();
                c = Peek();
            }

            return CreateToken(TokenType.Whitespace);
        }

        /// <summary>
        ///  Finishes reading a line comment (a comment that continues to the end of the line), with
        ///  the last two characters being '/' and '/'.
        /// </summary>
        /// <returns>Whitespace token representing the comment.</returns>
        private Token ContinueReadingLineComment()
        {
            // Comment goes to the end of the line.
            while (Peek() != '\n' && !IsAtEnd())
            {
                AdvanceOne();
            }

            return CreateToken(TokenType.Whitespace);
        }

        /// <summary>
        ///  Finishes reading a multi line comment (a comment that continues to the end of the line), with
        ///  the previous two characters being '*' and '/'.
        /// </summary>
        /// <returns>Whitespace token representing the comment.</returns>
        private Token ContinueReadingBlockComment()
        {
            // Comment continues until the terminating characters are found.
            while (Peek() != '*' && PeekNext() != '/' && !IsAtEnd())
            {
                AdvanceOne();
            }

            // Ensure comment was terminated correctly before removing comment termination chars.
            // TODO: Report start of comment not end.
            if (IsAtEnd())
            {
                throw new UnterminatedBlockCommentException(_startIndex);
            }

            AdvanceOne();
            AdvanceOne();

            // Report block comment as whitespace.
            return CreateToken(TokenType.Whitespace);
        }

        /// <summary>
        ///  Creates a non literal token.
        /// </summary>
        /// <param name="tokenType">Non literal token type.</param>
        /// <returns>Non literal token.</returns>
        private Token CreateToken(TokenType tokenType)
        {
            return Token.CreateNonLiteral(
                CurrentLexeme,
                tokenType,
                _filePath,
                _lineNumber,
                _startIndex,
                CurrentLexeme.Length);
        }

        /// <summary>
        ///  Advance the scanner to the next character while returning the character prior to advancing.
        /// </summary>
        /// <returns>The character immediately prior to advancing the scanner.</returns>
        private char AdvanceOne()
        {
            _currentIndex += 1;
            return _source[_currentIndex - 1];
        }

        /// <summary>
        ///  Peek at the current character without advancing the scanner or return '\0' if none.
        /// </summary>
        /// <returns>The current character.</returns>
        private char Peek()
        {
            if (IsAtEnd())
            {
                return '\0';
            }

            return _source[_currentIndex];
        }

        /// <summary>
        ///  Peek at the character following the current character or return '\0' if none.
        /// </summary>
        /// </remarks>
        /// <returns>The next character after the current character.</returns>
        private char PeekNext()
        {
            if (_currentIndex + 1 >= _source.Length)
            {
                return '\0';
            }

            return _source[_currentIndex + 1];
        }

        /// <summary>
        ///  Check if the current character matches the given character and if so advances the scanner to the next
        ///  character.
        /// </summary>
        /// <param name="v">Character to match.</param>
        /// <returns>True if the character matched, false otherwise.</returns>
        private bool Match(char v)
        {
            // There are no matches once the scanner has reached the end of the file.
            if (IsAtEnd())
            {
                return false;
            }

            // Check if they match and early return if they do not.
            if (_source[_currentIndex] != v)
            {
                return false;
            }

            // Advance the scanner because the match succeeded.
            AdvanceOne();
            return true;
        }

        /// <summary>
        ///  Check if the current position of the scanner is at the end of the string.
        /// </summary>
        /// <returns>True if the current position is at the end of the string, false otherwise.</returns>
        private bool IsAtEnd()
        {
            return _currentIndex >= _source.Length;
        }

        /// <summary>
        ///  Check if the character is a ASCII digit.
        /// </summary>
        /// <param name="c">Character to check.</param>
        /// <returns>True if the character is an ASCII digit, false otherwise.</returns>
        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        ///  Check if the character is whitespace.
        /// </summary>
        /// <param name="c">Character to check.</param>
        /// <returns>True if the character is whitespace, false otherwise.</returns>
        private static bool IsWhitespaceOrNewline(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        /// <summary>
        ///  Check if character can be the first charcter in a legal identifier.
        /// </summary>
        /// <param name="c">Charcter to check.</param>
        /// <returns>True if character is legal, false otherwise.</returns>
        private static bool IsIdentifierStartChar(char c)
        {
            return Char.IsLetter(c) || c == '_' || Char.IsSymbol(c);
        }

        /// <summary>
        ///  Check if character is part of a legal identifier.
        /// </summary>
        /// <param name="c">Charcter to check.</param>
        /// <returns>True if character is legal, false otherwise.</returns>
        private static bool IsIdentifierChar(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_' || Char.IsSymbol(c);
        }

        // Table of recogonized keywords and their matching token type.
        private static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>()
        {
            { "and", TokenType.And },
            { "base", TokenType.Base },
            { "break", TokenType.Break },
            { "class", TokenType.Class },
            { "continue", TokenType.Continue },
            { "else", TokenType.Else },
            { "false", TokenType.False },
            { "function", TokenType.Function },
            { "for", TokenType.For },
            { "foreach", TokenType.Foreach },
            { "if", TokenType.If },
            { "or", TokenType.Or },
            { "undefined", TokenType.Undefined },
            { "print", TokenType.Print },
            { "return", TokenType.Return },
            { "this", TokenType.This },
            { "true", TokenType.True },
            { "var", TokenType.Var },
            { "let", TokenType.Let },
            { "while", TokenType.While },
        };
    }

    /// <summary>
    ///  How whitespace tokens are generated by the scanner.
    /// </summary>
    public enum WhitespaceTokenPolicy
    {
        None,
        Emit
    }
}
