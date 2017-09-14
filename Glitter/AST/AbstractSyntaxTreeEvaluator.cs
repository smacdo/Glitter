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

namespace Glitter.AST
{
    /// <summary>
    ///  Evaluates an abstract syntax tree using the visitor pattern.
    /// </summary>
    public class AbstractSyntaxTreeEvaluator : IExpressionNodeVisitor<object>, IStatementNodeVisitor<object>
    {
        private Environment _environment;
        private IList<Statement> _statements;

        public AbstractSyntaxTreeEvaluator(IList<Statement> statements, Environment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _statements = statements ?? throw new ArgumentNullException(nameof(statements));
        }

        public object Evaluate()
        {
            foreach (var statement in _statements)
            {
                Execute(statement);
            }

            return null;
        }

        private void Execute(Statement statement)
        {
            statement.Visit(this);
        }

        public object VisitExpressionStatement(ExpressionStatement statement)
        {
            Evaluate(statement.Expression);
            return null;
        }

        public object VisitVariableDeclarationStatement(VariableDeclarationStatement statement)
        {
            object initialValue = null;
            
            if (statement.InitializerExpression != null)
            {
                initialValue = Evaluate(statement.InitializerExpression);
            }

            _environment.Define(statement.Name, initialValue);
            return null;
        }

        public object VisitBlock(Block block)
        {
            ExecuteBlock(block.Statements, new Environment(_environment));
            return null;
        }

        private void ExecuteBlock(IList<Statement> statements, Environment environment)
        {
            var previous = _environment;

            try
            {
                _environment = environment;

                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                _environment = previous;
            }
        }

        public object VisitPrintStatement(PrintStatement statement)
        {
            var value = Evaluate(statement.Expression);
            Console.WriteLine(Stringify(value));

            return null;
        }

        /// <summary>
        ///  Get the result of evaluating an abstract syntax tree node.
        /// </summary>
        private object Evaluate(ExpressionNode expression)
        {
            return expression.Visit(this);
        }

        /// <summary>
        ///  Apply an operator to the left hand and right hand expressions in a binary expression node
        ///  and return the result.
        /// </summary>
        public object VisitBinaryExpressionNode(BinaryExpressionNode binaryNode)
        {
            var left = Evaluate(binaryNode.Left);
            var right = Evaluate(binaryNode.Right);

            switch (binaryNode.Operator.Type)
            {
                // +
                case TokenType.Plus:
                    // TODO: Use dot notation for string concatenation rather than overloaded plus.
                    // TODO: Or another notation.
                    if (left is double && right is double)
                    {
                        return (double)left + (double)right;
                    }
                    else if (left is string && right is string)
                    {
                        return (string)left + (string)right;
                    }
                    else
                    {
                        throw new RuntimeException("LHS and RHS must be two numbers or strings", null, 0);
                    }

                // -
                case TokenType.Minus:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left - (double)right;

                // /
                case TokenType.Slash:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left / (double)right;

                // *
                case TokenType.Star:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left * (double)right;

                // >
                case TokenType.Greater:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left > (double)right;

                // >=
                case TokenType.GreaterEqual:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left >= (double)right;

                // <
                case TokenType.Less:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left < (double)right;

                // <=
                case TokenType.LessEqual:
                    ExpectNumber(left, "LHS must be a number");
                    ExpectNumber(right, "RHS must be a number");
                    return (double)left <= (double)right;

                // ==
                case TokenType.EqualEqual:
                    return AreEqual(left, right);

                // !=
                case TokenType.BangEqual:
                    return !AreEqual(left, right);

                default:
                    throw new InvalidOperationException("Unknown operator");    // TODO: Better exception.
            }
        }

        /// <summary>
        ///  Grouping nodes represent a parenthesized expression. Each grouping node has an inner expression
        ///  node that should be evaluated and returned.
        /// </summary>
        public object VisitGroupingNode(GroupingNode groupNode)
        {
            return Evaluate(groupNode.Node);
        }

        /// <summary>
        ///  Literal nodes hold atomic values that do not need to be further evaluated, so they can be returned
        ///  immediately.
        /// </summary>
        public object VisitLiteralNode(LiteralNode literalNode)
        {
            return literalNode.Value;
        }

        /// <summary>
        ///  Apply a unary operator on the RHS expression.
        /// </summary>
        public object VisitUnaryNode(UnaryNode unaryNode)
        {
            var right = Evaluate(unaryNode.Right);

            switch (unaryNode.Operator.Type)
            {
                case TokenType.Minus:
                    ExpectNumber(right, "Unary minus expects a number");
                    return -(double)right;

                case TokenType.Bang:
                    return !IsTruthy(right);

                default:
                    throw new InvalidOperationException("Unknown operator");    // TODO: Better exception.
            }
        }

        public object VisitVariableNode(VariableNode node)
        {
            return _environment.Get(node.VariableName);
        }

        public object VisitAssignmentNode(AssignmentNode node)
        {
            var value = Evaluate(node.Value);
            _environment.Set(node.VariableName, value);

            return value;
        }

        private static void ExpectNumber(object o, string failureMessage)
        {
            if (o is double == false)
            {
                throw new RuntimeException(failureMessage, null, 0); 
            }
        }

        /// <summary>
        ///  Check if two values are equal.
        /// </summary>
        /// <remarks>
        ///  Undefined is only equal to undefined, otherwise C# equality is use for the time being.
        /// </remarks>
        private static bool AreEqual(object left, object right)
        {
            if (left == null)
            {
                return (right == null ? true : false);
            }
            else
            {
                return left.Equals(right);
            }
        }

        /// <summary>
        ///  Test if a value is true or false.
        /// </summary>
        /// <remarks>
        ///   Values are true except for undefined and false.
        /// </remarks>
        private static bool IsTruthy(object value)
        {
            if (value == null)
            {
                return false;
            }
            else if (value is bool)
            {
                return (bool)value;
            }
            else
            {
                return true;
            }
        }


        private static string Stringify(object o)
        {
            if (o == null)
            {
                return "undefined";
            }
            else
            {
                // TODO: Handle int printing.
                return o.ToString();
            }
        }
    }
}
