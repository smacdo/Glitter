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

namespace Glitter
{
    /// <summary>
    ///  A token scanned from Glitter code.
    /// </summary>
    public struct Token
    {
        private object _rawObject;
        private double _rawNumber;
        
        public static Token CreateNonLiteral(string lexeme, TokenType type, int lineNumber)
        {
            if (type == TokenType.String || type == TokenType.Number || type == TokenType.Identifier)
            {
                throw new InvalidOperationException("Literal token types must have accompanying literal value");
            }

            return new Token(lexeme, type, null, 0.0f, lineNumber);
        }

        public static Token Identifier(string lexeme, string literal, int lineNumber)
        {
            if (string.IsNullOrEmpty(lexeme))
            {
                throw new ArgumentNullException(nameof(lexeme));
            }

            if (string.IsNullOrEmpty(literal))
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return new Token(lexeme, TokenType.Identifier, literal, 0.0f, lineNumber);
        }

        public static Token String(string lexeme, string literal, int lineNumber)
        {
            if (string.IsNullOrEmpty(lexeme))
            {
                throw new ArgumentNullException(nameof(lexeme));
            }

            if (string.IsNullOrEmpty(literal))
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return new Token(lexeme, TokenType.String, literal, 0.0f, lineNumber);
        }

        public static Token Number(string lexeme, double literal, int lineNumber)
        {
            if (string.IsNullOrEmpty(lexeme))
            {
                throw new ArgumentNullException(nameof(lexeme));
            }

            return new Token(lexeme, TokenType.Number, null, literal, lineNumber);
        }
        
        private Token(string lexeme, TokenType type, object rawObject, double rawNumber, int lineNumber)
        {
            Lexeme = lexeme;
            LineNumber = lineNumber;
            Type = type;

            _rawObject = rawObject;
            _rawNumber = rawNumber;
        }

        public string Lexeme { get; private set; }
        public int LineNumber { get; private set; }

        public string LiteralIdentifier
        {
            get
            {
                if (Type != TokenType.Identifier)
                {
                    throw new InvalidOperationException("Not a literal identifier");
                }

                return (string)_rawObject;
            }
        }

        public string LiteralString
        {
            get
            {
                if (Type != TokenType.String)
                {
                    throw new InvalidOperationException("Not a literal string");
                }

                return (string)_rawObject;
            }
        }

        public double LiteralNumber
        {
            get
            {
                if (Type != TokenType.Number)
                {
                    throw new InvalidOperationException("Not a literal number");
                }

                return _rawNumber;
            }
        }

        public TokenType Type { get; private set; }

        public static Token EndOfFile
        {
            get { return new Token("\0", TokenType.EndOfFile, null, 0, 0); }
        }

        public static Token None
        {
            get { return new Token(string.Empty, TokenType.None, null, 0, 0); }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case TokenType.Identifier:
                case TokenType.String:
                    return string.Format(
                        "type: {0} lexeme: '{1}' literal: '{2}'",
                        GetName(Type),
                        Lexeme,
                        _rawObject?.ToString() ?? "<null literal>");

                case TokenType.Number:
                    return string.Format(
                        "type: {0} lexeme: '{1}' literal: '{2}'",
                        GetName(Type),
                        Lexeme,
                        _rawNumber);

                default:
                    return string.Format("type: {0}", GetName(Type));
            }
        }

        public static string GetName(TokenType type)
        {
            switch (type)
            {
                // Literals
                case TokenType.Identifier: return "Identifier";
                case TokenType.String: return "String";
                case TokenType.Number: return "Number";

                // Tokens
                case TokenType.LeftParen: return "LeftParen";
                case TokenType.RightParen: return "RightParen";
                case TokenType.LeftBrace: return "LeftBrace";
                case TokenType.RightBrace: return "RightBrace";
                case TokenType.Comma: return "Comma";
                case TokenType.Dot: return "Dot";
                case TokenType.Minus: return "Minus";
                case TokenType.Plus: return "Plus";
                case TokenType.Semicolon: return "Semicolon";
                case TokenType.Slash: return "Slash";
                case TokenType.Star: return "Star";
                case TokenType.Bang: return "Bang";
                case TokenType.BangEqual: return "BangEqual";
                case TokenType.Equal: return "Equal";
                case TokenType.EqualEqual: return "EqualEqual";
                case TokenType.Greater: return "Greater";
                case TokenType.GreaterEqual: return "GreaterEqual";
                case TokenType.Less: return "Less";
                case TokenType.LessEqual: return "LessEqual";

                // Keywords
                case TokenType.And: return "And";
                case TokenType.Base: return "Base";
                case TokenType.Class: return "Class";
                case TokenType.Else: return "Else";
                case TokenType.False: return "False";
                case TokenType.Function: return "Function";
                case TokenType.For: return "For";
                case TokenType.If: return "If";
                case TokenType.Or: return "Or";
                case TokenType.Undefined: return "Undefined";
                case TokenType.Print: return "Print";
                case TokenType.Return: return "Return";
                case TokenType.This: return "This";
                case TokenType.True: return "True";
                case TokenType.Var: return "Var";
                case TokenType.While: return "While";

                // Misc
                case TokenType.Whitespace: return "Whitespace";
                case TokenType.EndOfFile: return "EndOfFile";

                default:
                    throw new InvalidOperationException("Unknown token type");
            }
        }
    }

    /// <summary>
    ///  Type of Glitter token.
    /// </summary>
    public enum TokenType
    {
        None,

        // Literals
        Identifier,
        String,
        Number,

        // Tokens
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        Comma,
        Dot,
        Minus,
        Plus,
        Semicolon,
        Slash,
        Star,
        Bang,
        BangEqual,
        Equal,
        EqualEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,

        // Keywords
        And,
        Base,
        Class,
        Else,
        False,
        Function,
        For,
        If,
        Or,
        Undefined,
        Print,
        Return,
        This,
        True,
        Var,
        While,

        // Misc
        Whitespace,
        EndOfFile
    }
}