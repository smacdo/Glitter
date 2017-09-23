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
        public Statement(int lineNumber)
        {
            LineNumber = lineNumber;
        }

        public int LineNumber { get; }

        public abstract T Visit<T>(IStatementNodeVisitor<T> visitor);
    }

    public class ExpressionStatement : Statement
    {
        public ExpressionStatement(Expression expression, int lineNumber)
            : base(lineNumber)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public Expression Expression { get; }

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
        /// <param name="lineNumber">Line number for first line of if statement.</param>
        public IfStatement(Expression condition, Statement thenBranch, Statement elseBranch, int lineNumber)
            : base(lineNumber)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            ThenBranch = thenBranch ?? throw new ArgumentNullException(nameof(thenBranch));
            ElseBranch = elseBranch;
        }

        public Expression Condition { get; }
        public Statement ThenBranch { get; }
        public Statement ElseBranch { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitIf(this);
        }
    }

    public class WhileStatement : Statement
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="condition">Required conditional expression.</param>
        /// <param name="body">Required statement body.</param>
        /// <param name="lineNumber">First line of while statement.</param>
        public WhileStatement(Expression condition, Statement body, int lineNumber)
            : base(lineNumber)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public Expression Condition { get; }
        public Statement Body { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitWhile(this);
        }
    }

    public class VariableDeclarationStatement : Statement
    {
        // TODO: Don't take token just take name?
        public VariableDeclarationStatement(Token name, Expression initializerExpression, int lineNumber)
            : base(lineNumber)
        {
            Name = name.LiteralIdentifier;
            InitializerExpression = initializerExpression;
        }

        public string Name { get; }
        public Expression InitializerExpression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitVariableDeclaration(this);
        }
    }

    public class FunctionDeclarationStatement : Statement
    {
        public FunctionDeclarationStatement(
            string name,
            IList<string> parameters,
            IList<Statement> body,
            int lineNumber)
            : base(lineNumber)
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

    public class BlockStatemnt : Statement
    {
        public BlockStatemnt(IList<Statement> statements, int lineNumber)
            : base(lineNumber)
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
        public ReturnStatement(Expression expression, int lineNumber)
            : base(lineNumber)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitReturn(this);
        }
    }

    public class PrintStatement : Statement
    {
        public PrintStatement(Expression expression, int lineNumber)
            : base(lineNumber)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        public override T Visit<T>(IStatementNodeVisitor<T> visitor)
        {
            return visitor.VisitPrint(this);
        }
    }
}
