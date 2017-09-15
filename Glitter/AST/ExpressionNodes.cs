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

namespace Glitter.AST
{
    /// <summary>
    ///  Base class for expression nodes.
    /// </summary>
    public abstract class Expression : AbstractSyntaxNode
    {
        public abstract T Visit<T>(IExpressionVisitor<T> visitor);
    }

    /// <summary>
    ///  An expression node with a single operator with a left hand side and a right hand side.
    /// </summary>
    public class BinaryExpression : Expression
    {
        // TODO: Token -> TokenType -> OperatorType
        public BinaryExpression(Expression left, Token @operator, Expression right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));

            if (@operator.Type == TokenType.None) // TODO: Check for exact token types
            {
                throw new ArgumentException(nameof(@operator));
            }

            Operator = @operator;
        }

        public Expression Left { get; private set; }
        public Token Operator { get; private set; }
        public Expression Right { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitBinary(this);
        }
    }
    public class LogicalExpression : Expression
    {
        // TODO: Token -> TokenType -> OperatorType
        public LogicalExpression(Expression left, Token @operator, Expression right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));

            if (@operator.Type != TokenType.And && @operator.Type != TokenType.Or) 
            {
                throw new ArgumentException(nameof(@operator));
            }

            Operator = @operator;
        }

        public Expression Left { get; private set; }
        public Token Operator { get; private set; }
        public Expression Right { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitLogical(this);
        }
    }

    public class GroupingExpression : Expression
    {
        public GroupingExpression(Expression node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public Expression Node { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitGrouping(this);
        }
    }

    public class LiteralExpression : Expression
    {
        public LiteralExpression(object @object)
        {
            Value = @object;
        }

        public object Value { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitLiteral(this);
        }
    }

    public class UnaryExpression : Expression
    {
        public UnaryExpression(Token @operator, Expression right)
        {
            Right = right ?? throw new ArgumentNullException(nameof(right));

            if (@operator.Type == TokenType.None) // TODO: Check for exact token types
            {
                throw new ArgumentException(nameof(@operator));
            }

            Operator = @operator;
        }

        public Token Operator { get; private set; }
        public Expression Right { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VistUnary(this);
        }
    }

    public class VariableExpression : Expression
    {
        public VariableExpression(Token name)     // TODO: Don't take token just take name
        {
            VariableName = name.LiteralIdentifier;
        }

        public string VariableName { get; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitVariable(this);
        }
    }

    public class AssignmentExpression : Expression
    {
        public AssignmentExpression(string variableName, Expression value)
        {
            VariableName = variableName ?? throw new ArgumentNullException(nameof(variableName));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Expression Value { get; }
        public string VariableName { get; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitAssignment(this);
        }
    }

    public class CallExpression : Expression
    {
        // TODO: Need to add location tracking to AST for runtime error reporting.
        public CallExpression(Expression callee, IList<Expression> arguments)
        {
            Callee = callee ?? throw new ArgumentNullException(nameof(callee));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public Expression Callee { get; }
        public IList<Expression> Arguments { get; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VistiCall(this);
        }
    }
}
