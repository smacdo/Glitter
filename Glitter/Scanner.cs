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
    ///  Scans text and returns a list of tokens.
    /// </summary>
    public class Scanner
    {
        private string _source;
        private int _startIndex = 0;
        private int _currentIndex = 0;
        private int _lineNumber = 0;

        public Scanner(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public string CurrentLexeme
        {
            get { return _source.Substring(_startIndex, _currentIndex - _startIndex); }
        }

        public bool IgnoreWhitespace { get; set; } = true;

        public IEnumerable<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // We are at the start of the next lexeme.
                _startIndex = _currentIndex;
                var nextToken = ScanNextToken();

                if (!IgnoreWhitespace || nextToken.Type != TokenType.Whitespace)
                {
                    yield return nextToken;
                }
            }

            // At the end of the text stream.
            yield return Token.EndOfFile;
        }

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
                    if (IsDigit(PeekNext()))
                    {
                        return ReadNumber();
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
                        // Comment goes to the end of the line.
                        while (Peek() != '\n' && !IsAtEnd())
                        {
                            AdvanceOne();
                        }

                        return CreateToken(TokenType.Whitespace);
                    }
                    else
                    {
                        return CreateToken(TokenType.Slash);
                    }

                case '"': return ReadStringToken();                

                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    {
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

                default:
                    // Look for character ranges that make valid tokens before throwing an unknown exception.
                    if (IsDigit(c))
                    {
                        return ReadNumber();
                    }
                    else if (IsIdentifierStartChar(c))
                    {
                        return ReadIdentifierOrKeyword();
                    }
                    else
                    {
                        throw new UnexpectedCharacterException(c, _lineNumber);
                    }
            }
        }

        private Token ReadIdentifierOrKeyword()
        {
            // Read rest of potential identifier.
            while (IsIdentifierStartChar(Peek()))
            {
                AdvanceOne();
            }

            var name = _source.Substring(_startIndex, _currentIndex - _startIndex);

            // Is this a keyword?
            if (Keywords.ContainsKey(name))
            {
                return CreateToken(Keywords[name]);
            }
            else
            {
                return Token.Identifier(CurrentLexeme, name, _lineNumber);
            }
        }

        private Token ReadNumber()
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
            var number = Convert.ToDouble(_source.Substring(_startIndex, _currentIndex - _startIndex));
            number *= (isNegative ? -1 : 1);

            return Token.Number(CurrentLexeme, number, _lineNumber);
        }

        private Token ReadStringToken()
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
                throw new UnterminatedStringException(_lineNumber);
            }

            // Consume the closing quote.
            AdvanceOne();

            // Return a string token without the enclosing quotes.
            var value = _source.Substring(_startIndex + 1, _currentIndex - _startIndex - 2);
            return Token.String(CurrentLexeme, value, _lineNumber);
        }

        private Token CreateToken(TokenType tokenType)
        {
            return Token.CreateNonLiteral(CurrentLexeme, tokenType, _lineNumber);
        }

        private char AdvanceOne()
        {
            _currentIndex += 1;
            return _source[_currentIndex - 1];
        }

        private char Peek()
        {
            if (IsAtEnd())
            {
                return '\0';
            }

            return _source[_currentIndex];
        }

        private char PeekNext()
        {
            if (_currentIndex + 1 >= _source.Length)
            {
                return '\0';
            }

            return _source[_currentIndex + 1];
        }

        private bool Match(char v)
        {
            if (IsAtEnd())
            {
                return false;
            }

            if (_source[_currentIndex] != v)
            {
                return false;
            }

            _currentIndex++;
            return true;
        }

        private bool IsAtEnd()
        {
            return _currentIndex >= _source.Length;
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool IsWhitespaceOrNewline(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private static bool IsIdentifierStartChar(char c)
        {
            return Char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierChar(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_';
        }

        private static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>()
        {
            { "and", TokenType.And },
            { "base", TokenType.Base },
            { "class", TokenType.Class },
            { "else", TokenType.Else },
            { "false", TokenType.False },
            { "function", TokenType.Function },
            { "for", TokenType.For },
            { "if", TokenType.If },
            { "or", TokenType.Or },
            { "undefined", TokenType.Undefined },
            { "print", TokenType.Print },
            { "return", TokenType.Return },
            { "this", TokenType.This },
            { "true", TokenType.True },
            { "var", TokenType.Var },
            { "while", TokenType.While },
        };
    }
}
