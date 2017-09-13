﻿/*
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

namespace Glitter.AST
{
    public abstract class AbstractSyntaxNode
    {
        public abstract T Visit<T>(IAbstractSyntaxNodeVisitor<T> visitor);
    }

    /// <summary>
    ///  Base class for expression nodes.
    /// </summary>
    public abstract class ExpressionNode : AbstractSyntaxNode
    {
    }

    /// <summary>
    ///  An expression node with a single operator with a left hand side and a right hand side.
    /// </summary>
    public class BinaryExpressionNode : ExpressionNode
    {
        public BinaryExpressionNode(ExpressionNode left, Token @operator, ExpressionNode right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));

            if (@operator.Type == TokenType.None) // TODO: Check for exact token types
            {
                throw new ArgumentException(nameof(@operator));
            }

            Operator = @operator;
        }

        public ExpressionNode Left { get; private set; }
        public Token Operator { get; private set; }
        public ExpressionNode Right { get; private set; }

        public override T Visit<T>(IAbstractSyntaxNodeVisitor<T> visitor)
        {
            return visitor.VisitBinaryExpressionNode(this);
        }
    }

    public class GroupingNode : ExpressionNode
    {
        public GroupingNode(ExpressionNode node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public ExpressionNode Node { get; private set; }

        public override T Visit<T>(IAbstractSyntaxNodeVisitor<T> visitor)
        {
            return visitor.VisitGroupingNode(this);
        }
    }

    public class LiteralNode : ExpressionNode
    {
        public LiteralNode(object @object)
        {
            Value = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public object Value { get; private set; }

        public override T Visit<T>(IAbstractSyntaxNodeVisitor<T> visitor)
        {
            return visitor.VisitLiteralNode(this);
        }
    }

    public class UnaryNode : ExpressionNode
    {
        public UnaryNode(Token @operator, ExpressionNode right)
        {
            Right = right ?? throw new ArgumentNullException(nameof(right));

            if (@operator.Type == TokenType.None) // TODO: Check for exact token types
            {
                throw new ArgumentException(nameof(@operator));
            }

            Operator = @operator;
        }

        public Token Operator { get; private set; }
        public ExpressionNode Right { get; private set; }

        public override T Visit<T>(IAbstractSyntaxNodeVisitor<T> visitor)
        {
            return visitor.VisitUnaryNode(this);
        }
    }
}