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
using Glitter.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glitter
{
    public class Parser
    {
        private readonly Token[] _tokens;
        private int _currentTokenIndex = 0;
        
        public Parser(IEnumerable<Token> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            _tokens = tokens.ToArray();
        }
        
        public EventHandler<ParseErrorEventArgs> OnError { get; set; }

        public Token CurrentToken
        {
            get
            {
                if (_currentTokenIndex >= _tokens.Length)
                {
                    throw new InvalidOperationException("Current token index invalid");
                }

                return _tokens[_currentTokenIndex];
            }
        }

        public Token PreviousToken
        {
            get
            {
                if (_currentTokenIndex < 1 || _currentTokenIndex - 1 >= _tokens.Length)
                {
                    throw new InvalidOperationException("Previous token index invalid");
                }

                return _tokens[_currentTokenIndex - 1];
            }
        }

        public bool TokenStreamEnded
        {
            get { return CurrentToken.Type == TokenType.EndOfFile; }
        }

        public ExpressionNode Parse()
        {
            try
            {
                return Expression();
            }
            catch (ParserException)
            {
                // TODO: Add more error handling?
                return null;
            }
        }

        private ExpressionNode Expression()
        {
            return Equality();
        }

        /// <summary>
        ///  Equality -> Comparison (("!" | "==") Comparison)*
        /// </summary>
        private ExpressionNode Equality()
        {
            var expression = Comparison();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.EqualEqual, TokenType.BangEqual))
            {
                var @operator = PreviousToken;
                var right = Expression();

                expression = new BinaryExpressionNode(expression, @operator, right);
            }

            return expression;
        }

        /// <summary>
        ///  Comparison -> Addition ((">" | ">=" | "&gt" | "&gt;=") Addition)*
        /// </summary>
        private ExpressionNode Comparison()
        {
            var expression = Addition();

            while (
                AdvanceIfCurrentTokenIsOneOf(
                    TokenType.Greater,
                    TokenType.GreaterEqual,
                    TokenType.Less,
                    TokenType.LessEqual))
            {
                var @operator = PreviousToken;
                var right = Addition();
                expression = new BinaryExpressionNode(expression, @operator, right);
            }

            return expression;
        }

        private ExpressionNode Addition()
        {
            var expression = Multiplication();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.Minus, TokenType.Plus))
            {
                var @operator = PreviousToken;
                var right = Multiplication();
                expression = new BinaryExpressionNode(expression, @operator, right);
            }

            return expression;
        }

        private ExpressionNode Multiplication()
        {
            var expression = Unary();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.Slash, TokenType.Star))
            {
                var @operator = PreviousToken;
                var right = Unary();
                expression = new BinaryExpressionNode(expression, @operator, right);
            }

            return expression;
        }

        /// <summary>
        ///  Unary -> ("!" | "-") Unary | Primary
        /// </summary>
        private ExpressionNode Unary()
        {
            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Bang, TokenType.Slash))
            {
                var @operator = PreviousToken;
                var right = Unary();
                return new UnaryNode(@operator, right);
            }

            return Primary();
        }

        /// <summary>
        ///  Primary -> NUMBER | STRING | "false" | "true" | "undefined" | "(" expression ")" ;
        /// </summary>
        /// <returns></returns>
        private ExpressionNode Primary()
        {
            if (AdvanceIfCurrentTokenIsOneOf(TokenType.False))
            {
                return new LiteralNode(false);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.True))
            {
                return new LiteralNode(true);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Undefined))
            {
                return new LiteralNode(null);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Number))
            {
                return new LiteralNode(PreviousToken.LiteralNumber);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.String))
            {
                return new LiteralNode(PreviousToken.LiteralString);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.LeftParen))
            {
                var expression = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return new GroupingNode(expression);
            }

            throw RaiseError("Expected expression", CurrentToken.ToString(), CurrentToken.LineNumber);
        }

        /// <summary>
        ///  Check if the next token is the expected type and move to the following token, otherwise starts
        ///  error recovery.
        /// </summary>
        /// <param name="expectedType">Expected token type.</param>
        /// <param name="messageIfUnexpected">Message if the token type does not match.</param>
        /// <returns></returns>
        private Token Consume(TokenType expectedType, string messageIfUnexpected)
        {
            if (IsCurrentTokenA(expectedType))
            {
                return AdvanceToNextToken();
            }

            throw RaiseError(messageIfUnexpected, CurrentToken.ToString(), CurrentToken.LineNumber);
        }

        /// <summary>
        ///  Synchronizes the parser state after an error happens by discarding tokens until the start of the next
        ///  statement is reached.
        /// </summary>
        private void PerformErrorRecovery()
        {
            AdvanceToNextToken();

            while (!TokenStreamEnded)
            {
                // Has the error recovery reached a semi-colon ending the statement?
                if (PreviousToken.Type == TokenType.Semicolon)
                {
                    return;
                }

                // Also stop recovery once a keyword block is found.
                switch (CurrentToken.Type)
                {
                    case TokenType.Class:
                    case TokenType.Function:
                    case TokenType.Var:
                    case TokenType.For:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.Print:
                    case TokenType.Return:
                        return;
                }

                AdvanceToNextToken();
            }
        }

        private ParserException RaiseError(string message, string what, int lineNumber)
        {
            if (OnError != null)
            {
                OnError.Invoke(this, new ParseErrorEventArgs(){
                    Message = message,
                    What = what,
                    LineNumber = lineNumber
                });
            }

            return new ParserException(message, what, lineNumber);
        }

        // TODO: Optimze by providing common non-params.
        private bool AdvanceIfCurrentTokenIsOneOf(params TokenType[] tokenTypesToMatch)
        {
            foreach (var tokenType in tokenTypesToMatch)
            {
                if (IsCurrentTokenA(tokenType))
                {
                    AdvanceToNextToken();
                    return true;
                }
            }

            return false;
        }

        private bool IsCurrentTokenA(TokenType tokenType)
        {
            if (TokenStreamEnded)
            {
                return false;
            }

            return CurrentToken.Type == tokenType;
        }

        private Token AdvanceToNextToken()
        {
            if (!TokenStreamEnded)
            {
                _currentTokenIndex += 1;
            }

            return PreviousToken;
        }
    }

    public class ParseErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string What { get; set; }
        public int LineNumber { get; set; }
    }
}
