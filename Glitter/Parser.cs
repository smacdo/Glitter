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

        public bool HasParseErrors { get; private set; } = false;

        /// <summary>
        ///  Read:
        ///   Program -> Declaration* EOF
        /// </summary>
        public IList<Statement> Parse()
        {
            var statements = new List<Statement>();     // TODO: Estimate.
            
            while (!TokenStreamEnded)
            {
                statements.Add(Declarations());
            }

            return statements;
        }

        /// <summary>
        ///  Declaration         -> VariableDeclaration | FunctionDeclaration | Statement
        ///  FunctionDeclaration -> "function" Function
        /// </summary>
        /// <returns></returns>
        private Statement Declarations()
        {
            try
            {
                if (AdvanceIfCurrentTokenIsOneOf(TokenType.Var))
                {
                    return VariableDeclaration();
                }
                else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Function))
                {
                    return Function("function");
                }
                else
                {
                    return Statement();
                }
            }
            catch (ParserException e)
            {
                PerformErrorRecovery();
                return null;        // TODO: Filter null from results!
            }
        }

        /// <summary>
        ///  Function   -> IDENTIFIER "(" Parameters ")" block
        ///  Parameters -> IDENTIFIER ( "," IDENTIFIER )*
        /// </summary>
        private Statement Function(string expectedFunctionKind)
        {
            // Parse the function identifier.
            var nameToken = Consume(TokenType.Identifier, $"Expected {expectedFunctionKind} name");

            // Parse the parameter list along with the parenthesis around it.
            Consume(TokenType.LeftParen, $"Expected ( after {expectedFunctionKind} name");
            var parameters = new List<string>();    // TODO: Optimize with presize.

            if (!IsCurrentTokenA(TokenType.RightParen))
            {
                do
                {
                    if (parameters.Count > 32)     // TODO: Magic numbers.
                    {
                        RaiseError("Too many parameters", null, -1);    // TODO: Better error.
                    }

                    parameters.Add(Consume(TokenType.Identifier, "Expected parameter name").Lexeme);
                } while (AdvanceIfCurrentTokenIsOneOf(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ) after parameters");

            // Get the function body as a block statement.
            //  TODO: Do the same with if / for / while.
            Consume(TokenType.LeftBrace, "Expected { before " + expectedFunctionKind + "body"); // TODO: string $.
            var body = Block();

            return new FunctionDeclarationStatement(nameToken.LiteralIdentifier, parameters, body);
        }

        /// <summary>
        ///  VariableDeclaration -> "var" IDENTIFIER ( "=" Expression )? ";"
        /// </summary>
        private Statement VariableDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected variable name");
            Expression initializer = null;

            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Equal))
            {
                initializer = Expression();
            }

            Consume(TokenType.Semicolon, "Expected ; after variable declaration");
            return new VariableDeclarationStatement(name, initializer);
        }

        /// <summary>
        ///  Statement -> ExpressionStatement
        ///             | IfStatement
        ///             | WhileStatement
        ///             | ForStatement
        ///             | ReturnStatement
        ///             | PrintStatement
        ///             | Block
        /// </summary>
        private Statement Statement()
        {
            if (AdvanceIfCurrentTokenIsOneOf(TokenType.LeftBrace))
            {
                return new BlockStatemnt(Block());
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.If))
            {
                return IfStatement();
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.While))
            {
                return WhileStatement();
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.For))
            {
                return ForStatement();
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Return))
            {
                return ReturnStatement();
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Print))
            {
                return PrintStatement();
            }
            else
            {
                // Not a statement.. fallthrough to expression
                // TODO: This feels wrong? Especially because ExpressionStatement only holds Expression?
                return ExpressionStatement();
            }
        }

        /// <summary>
        ///  Return -> "return" expression? ";"
        /// </summary>
        private Statement ReturnStatement()
        {
            Expression value = null;

            // A return expression is optional. If the next token is not a semi-colon then it can the parser will
            // assume there is a return expression to be parsed.
            if (!IsCurrentTokenA(TokenType.Semicolon))
            {
                value = Expression();
            }

            Consume(TokenType.Semicolon, "Expected ; after return expression");
            return new ReturnStatement(value);
        }

        /// <summary>
        ///  If -> "if" "(" expression ")" statement ("else" statement)?
        /// </summary>
        /// <remarks>
        ///  Note that the else branch is eagerly matched such that it is matched to the nearest if statement.
        ///  This avoids the dangling if else problem.
        ///  
        ///  TODO: We should else if or elseif block.
        /// </remarks>
        private Statement IfStatement()
        {
            // Get the conditional part of the if statemnet.
            Consume(TokenType.LeftParen, "Expected ( after if");
            var conditional = Expression();
            Consume(TokenType.RightParen, "Expected ) after if conditional");

            // Read the "then branch" which is the statement to be executed if the conditional is true.
            var thenBranch = Statement();

            // Try to read the "else branch" if it exists, which will be the statement that is executed if the
            // conditional is not true.
            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Else))
            {
                var elseBranch = Statement();
                return new IfStatement(conditional, thenBranch, elseBranch);
            }
            else
            {
                return new IfStatement(conditional, thenBranch, null);
            }
        }

        /// <summary>
        ///  While -> "while" "(" Expression ")" Statement
        /// </summary>
        private Statement WhileStatement()
        {
            Consume(TokenType.LeftParen, "Expected ( after while");
            var conditional = Expression();
            Consume(TokenType.RightParen, "Expected ) after while conditional");

            var body = Statement();

            return new WhileStatement(conditional, body);
        }

        /// <summary>
        ///  For -> "for" "(" (VariableDeclaration | ExpressionStatement | ";")
        ///                   Expression? ";"
        ///                   Expression? ")" Statement
        /// </summary>
        private Statement ForStatement()
        {
            Consume(TokenType.LeftParen, "Expected ( after for");

            // Read the initializer clause.
            Statement initializer = null;

            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Semicolon))
            {
                // Empty initializer clause.
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Var))
            {
                // Variable declaration clause. (eg var a = 2).
                initializer = VariableDeclaration();
            }
            else
            {
                // Expression clause, hopefully an assignment. (eg a = 2).
                //  TODO: Warn if not assignment.
                initializer = ExpressionStatement();
            }

            // Read the conditional clause.
            Expression conditional = null;

            if (!IsCurrentTokenA(TokenType.Semicolon))
            {
                conditional = Expression();
            }

            Consume(TokenType.Semicolon, "Expected ; after loop conditional");

            // Read the increment caluse.
            Expression increment = null;

            if (!IsCurrentTokenA(TokenType.RightParen))
            {
                increment = Expression();
            }

            Consume(TokenType.RightParen, "Expected ) after for clauses");

            // Ready for body.
            var body = Statement();

            // Convert the for loop semantics into a while loop statement. Use the process of desugaring to alter the
            // abstract syntax tree of a new while loop to match the semantics of the for loop that was just parsed.
            //
            // First, if there is an increment clause it should execute after the body of the while. Create a new block
            // statement that holds the while body statement followed by the increment.
            if (increment != null)
            {
                body = new BlockStatemnt(new List<Statement>()
                {
                    body,
                    new ExpressionStatement(increment)
                });
            }

            // Inject a true literal if the for loop did not specify a conditonal because for loops without a
            // conditional expression always default to true.
            if (conditional == null)
            {
                conditional = new LiteralExpression(true);
            }

            // Construct a while statement with the conditional from the for loop.
            body = new WhileStatement(conditional, body);

            // If an initializer was specified it should be executed prior to the start of the loop. Construct
            // another block with the initializer first and the while second.
            if (initializer != null)
            {
                body = new BlockStatemnt(new List<Statement>()
                {
                    initializer,
                    body
                });
            }

            return body;
        }

        private IList<Statement> Block()
        {
            var statements = new List<Statement>();

            while (!IsCurrentTokenA(TokenType.RightBrace) && !TokenStreamEnded)
            {
                statements.Add(Declarations());
            }

            Consume(TokenType.RightBrace, "Expected } after block");
            return statements;
        }

        /// <summary>
        ///  PrintStatement -> "print" expression ";"
        /// </summary>
        private Statement PrintStatement()
        {
            var expression = Expression();
            Consume(TokenType.Semicolon, "Expected ; after value");

            return new PrintStatement(expression);
        }

        /// <summary>
        ///  ExpressionStatement -> Expression ";"
        /// </summary>
        /// <returns></returns>
        private Statement ExpressionStatement()
        {
            var expression = Expression();
            Consume(TokenType.Semicolon, "Expected ; after expression");

            return new ExpressionStatement(expression);
        }

        /// <summary>
        ///  Expression -> Assignment
        /// </summary>
        private Expression Expression()
        {
            return Assignment();
        }

        /// <summary>
        ///  Assignment -> Identifier "=" Assignment | Or
        /// </summary>
        private Expression Assignment()
        {
            // Evaluate the left side of the (potential) assignment expression. Once the lhs expression has been
            // read and if the following token is the "=" operator then treat this as an assignment expression.
            // Otherwise fallthrough to the next rule (equality).
            var leftValue = Or();

            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Equal))
            {
                // This looks like an assignment expression!

                // TODO: Is it OK to check if leftValue is valid BEFORE we try reading the r-value expression?
                // It would be faster but need to make sure it doesn't change how things work...
                var equalsToken = PreviousToken;
                var rightValue = Assignment();

                if (leftValue is VariableExpression leftVarValue)
                {
                    var varName = leftVarValue.VariableName;
                    return new AssignmentExpression(varName, rightValue);
                }
                else
                {
                    throw RaiseError("Unexpected assignment target", leftValue.ToString(), equalsToken.LineNumber);
                }
            }
            else
            {
                // This is not an assignment expression so perform fall through by returning the potential left hand
                // side as the evaluated expression.
                return leftValue;
            }
        }

        /// <summary>
        ///  Or -> Or | ("or" LogicAnd)*
        /// </summary>
        private Expression Or()
        {
            var expression = And();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.Or))
            {
                var @operator = PreviousToken;
                var right = And();
                expression = new LogicalExpression(expression, @operator, right);
            }

            return expression;
        }

        /// <summary>
        ///  And -> Equality ("and" Equality)*
        /// </summary>
        private Expression And()
        {
            var expression = Equality();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.And))
            {
                var @operator = PreviousToken;
                var right = Equality();
                expression = new LogicalExpression(expression, @operator, right);
            }

            return expression;
        }

        /// <summary>
        ///  Equality -> Comparison (("!" | "==") Comparison)*
        /// </summary>
        private Expression Equality()
        {
            var expression = Comparison();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.EqualEqual, TokenType.BangEqual))
            {
                var @operator = PreviousToken;
                var right = Expression();

                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        /// <summary>
        ///  Comparison -> Addition ((">" | ">=" | "&gt" | "&gt;=") Addition)*
        /// </summary>
        private Expression Comparison()
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
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression Addition()
        {
            var expression = Multiplication();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.Minus, TokenType.Plus))
            {
                var @operator = PreviousToken;
                var right = Multiplication();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression Multiplication()
        {
            var expression = Unary();

            while (AdvanceIfCurrentTokenIsOneOf(TokenType.Slash, TokenType.Star))
            {
                var @operator = PreviousToken;
                var right = Unary();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        /// <summary>
        ///  Unary -> ("!" | "-") Unary | Call
        /// </summary>
        private Expression Unary()
        {
            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Bang, TokenType.Slash))
            {
                var @operator = PreviousToken;
                var right = Unary();
                return new UnaryExpression(@operator, right);
            }

            return Call();
        }

        /// <summary>
        ///  Call -> Primary ("(" arguments? ")")* ;
        /// </summary>
        private Expression Call()
        {
            var expression = Primary();

            // Every '(' encountered after the primary expression triggers a call to FinishCall which invokes
            // the current expression as a function call. The returned expression becomes the new expression
            // and it loops again to see if the result should be called.
            while (true)
            {
                if (AdvanceIfCurrentTokenIsOneOf(TokenType.LeftParen))
                {
                    expression = FinishCall(expression);
                }
                else
                {
                    // TODO: Handle call on objects.
                    break;
                }
            }

            return expression;
        }

        private Expression FinishCall(Expression callee)
        {
            var arguments = new List<Expression>();

            if (!IsCurrentTokenA(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(Expression());
                }
                while (AdvanceIfCurrentTokenIsOneOf(TokenType.Comma));
            }

            /*var paren = */Consume(TokenType.RightParen, "Expected ')' after arguments");
            // TODO: handle position info...

            // Ensure maximum call bounds.
            //  TODO: Add as a magic number somewhere.
            //  TODO: add check to callnode.
            if (arguments.Count > 32)
            {
                RaiseError(
                    "Cannot have more than 32 arguments",
                    CurrentToken.Lexeme,
                    CurrentToken.LineNumber);
            }

            return new CallExpression(callee, arguments);
        }

        /// <summary>
        ///  Arguments -> Expression ( "," expression)*
        /// </summary>
        private Expression Arguments()
        {
            return null;
        }

        /// <summary>
        ///  Primary -> NUMBER | STRING | "false" | "true" | "undefined" | "(" expression ")" | IDENTIFIER
        /// </summary>
        /// <returns></returns>
        private Expression Primary()
        {
            if (AdvanceIfCurrentTokenIsOneOf(TokenType.Identifier))
            {
                return new VariableExpression(PreviousToken);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.False))
            {
                return new LiteralExpression(false);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.True))
            {
                return new LiteralExpression(true);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Undefined))
            {
                return new LiteralExpression(null);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.Number))
            {
                return new LiteralExpression(PreviousToken.LiteralNumber);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.String))
            {
                return new LiteralExpression(PreviousToken.LiteralString);
            }
            else if (AdvanceIfCurrentTokenIsOneOf(TokenType.LeftParen))
            {
                var expression = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return new GroupingExpression(expression);
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

            HasParseErrors = true;

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
