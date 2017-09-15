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
    public abstract class Statement : AbstractSyntaxNode
    {
        public abstract T Visit<T>(IStatementNodeVisitor<T> visitor);
    }

    public class ExpressionStatement : Statement
    {
        public ExpressionStatement(ExpressionNode expression)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public ExpressionNode Expression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }

    public class IfStatement : Statement
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="condition">Required if conditional expression.</param>
        /// <param name="thenBranch">Required then statement.</param>
        /// <param name="elseBranch">Optional else statement.</param>
        public IfStatement(ExpressionNode condition, Statement thenBranch, Statement elseBranch)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            ThenBranch = thenBranch ?? throw new ArgumentNullException(nameof(thenBranch));
            ElseBranch = elseBranch;
        }

        public ExpressionNode Condition { get; }
        public Statement ThenBranch { get; }
        public Statement ElseBranch { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitIfStatement(this);
        }
    }

    public class WhileStatement : Statement
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="condition">Required conditional expression.</param>
        /// <param name="body">Required statement body.</param>
        public WhileStatement(ExpressionNode condition, Statement body)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public ExpressionNode Condition { get; }
        public Statement Body { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitWhileStatement(this);
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

    public class FunctionDeclaration : Statement
    {
        public FunctionDeclaration(string name, IList<string> parameters, IList<Statement> body)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public string Name { get; }
        public IList<string> Parameters { get; }        // TODO: Make into array.
        public IList<Statement> Body { get; }           // TODO: Make into array.

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitFunctionDeclaration(this);
        }
    }

    public class Block : Statement
    {
        public Block(IList<Statement> statements)
        {
            Statements = statements ?? throw new ArgumentNullException(nameof(statements));
        }

        public IList<Statement> Statements { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitBlock(this);
        }
    }

    public class ReturnStatement : Statement
    {
        public ReturnStatement(ExpressionNode expression)
        {
            Expression = expression;
        }

        public ExpressionNode Expression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitReturn(this);
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
