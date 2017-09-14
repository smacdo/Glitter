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

namespace Glitter.AST
{
    public abstract class Statement : AbstractSyntaxNode
    {
        public abstract T Visit<T>(IStatementNodeVisitor<T> visitor);
    }

    public class ExpressionStatement : Statement
    {
        public ExpressionStatement(ExpressionNode expression)
        {
            Expression = expression;
        }

        public ExpressionNode Expression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }

    public class VariableDeclarationStatement : Statement
    {
        // TODO: Don't take token just take name?
        public VariableDeclarationStatement(Token name, ExpressionNode initializerExpression)
        {
            Name = name.LiteralIdentifier;
            InitializerExpression = initializerExpression;
        }

        public string Name { get; }
        public ExpressionNode InitializerExpression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitVariableDeclarationStatement(this);
        }
    }

    public class PrintStatement : Statement
    {
        public PrintStatement(ExpressionNode expression)
        {
            Expression = expression;
        }

        public ExpressionNode Expression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitPrintStatement(this);
        }
    }
}
