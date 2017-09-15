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
using Glitter.AST;

namespace Glitter.AstInterpreter
{
    /// <summary>
    ///  Evaluates an abstract syntax tree using the visitor pattern.
    /// </summary>
    public class AbstractSyntaxTreeInterpreter : IExpressionVisitor<object>, IStatementNodeVisitor<object>
    {
        private Environment _currentEnvironment;
        private IList<Statement> _statements;

        public AbstractSyntaxTreeInterpreter(IList<Statement> statements, Environment environment)
        {
            RootEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
            _currentEnvironment = RootEnvironment;
            _statements = statements ?? throw new ArgumentNullException(nameof(statements));
        }

        public Environment RootEnvironment { get; }

        public object Execute()
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

        public object VisitVariableDeclaration(VariableDeclarationStatement statement)
        {
            // TODO: Return refactor - check that is no pending return request. If so throw exception.

            object initialValue = null;
            
            if (statement.InitializerExpression != null)
            {
                initialValue = Evaluate(statement.InitializerExpression);
            }

            _currentEnvironment.Define(statement.Name, initialValue);
            return null;
        }

        public object VisitFunctionDeclaration(FunctionDeclarationStatement declaration)
        {
            // TODO: Return refactor - check that is no pending return request. If so throw exception.
            var function = new Function(declaration, _currentEnvironment);
            _currentEnvironment.Define(declaration.Name, function);
            return null;
        }

        public object VisitBlock(BlockStatemnt block)
        {
            // TODO: Return refactor - check that is no pending return request. If so throw exception.
            ExecuteBlock(block.Statements, new Environment(_currentEnvironment));
            return null;
        }

        public void ExecuteBlock(IList<Statement> statements, Environment environment)
        {
            var previous = _currentEnvironment;

            try
            {
                _currentEnvironment = environment;

                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                _currentEnvironment = previous;
            }
        }

        public object VisitIf(IfStatement statement)
        {
            if (IsTruthy(Evaluate(statement.Condition)))
            {
                Execute(statement.ThenBranch);
            }
            else if (statement.ElseBranch != null)
            {
                Execute(statement.ElseBranch);
            }

            return null;
        }

        public object VisitWhile(WhileStatement statement)
        {
            while (IsTruthy(Evaluate(statement.Condition)))
            {
                Execute(statement.Body);
            }

            return null;
        }

        public object VisitReturn(ReturnStatement statement)
        {
            object result = null;

            if (statement.Expression != null)
            {
                result = Evaluate(statement.Expression);
            }

            throw new ReturnException(result);
        }

        public object VisitPrint(PrintStatement statement)
        {
            var value = Evaluate(statement.Expression);
            Console.WriteLine(Stringify(value));

            return null;
        }
        
        private object Evaluate(Expression expression)
        {
            return expression.Visit(this);
        }

        /// <summary>
        ///  Apply an operator to the left hand and right hand expressions in a binary expression node
        ///  and return the result.
        /// </summary>
        public object VisitBinary(BinaryExpression binaryNode)
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

        public object VisitLogical(LogicalExpression node)
        {
            // Evaluate the left side first and then check the logical operation type to enable short circuit
            // behavior.
            var left = Evaluate(node.Left);
            
            switch (node.Operator.Type)
            {
                case TokenType.Or:
                    // If left is true then short circuit the or expression by returning the left result without
                    // evaluating the right side.
                    if (IsTruthy(left))
                    {
                        return left;
                    }
                    break;

                case TokenType.And:
                    // If left is false then short circuit the and expression by returning the left result without
                    // evaluating the right side.
                    if (!IsTruthy(left))
                    {
                        return left;
                    }
                    break;

                default:
                    throw new InvalidOperationException("Unkonwn operator");
            }

            // The left expression is not sufficient to complete this node's evaluation. Evaluate the right expression
            // and return it.
            // TODO: When the language switches to strong typing this code needs to be switched. Rather than returning
            //       an object that is truthy, a true or false value must be returned.
            return Evaluate(node.Right);
        }

        /// <summary>
        ///  Grouping nodes represent a parenthesized expression. Each grouping node has an inner expression
        ///  node that should be evaluated and returned.
        /// </summary>
        public object VisitGrouping(GroupingExpression groupNode)
        {
            return Evaluate(groupNode.Node);
        }

        public object VistiCall(CallExpression node)
        {
            var callee = Evaluate(node.Callee);
            var arguments = new List<object>(); // TODO: Check arrity when eval so we can use simple
                                                // array for perf rather than List<>.

            foreach (var argument in node.Arguments)
            {
                arguments.Add(Evaluate(argument));
            }

            // Can only call on objects that support callable.
            if (callee is ICallable function)
            {
                if (arguments.Count != function.Arity)
                {
                    throw new RuntimeException(
                        string.Format(
                            "Expected {0} arguments but got {1}",
                            function.Arity,
                            arguments.Count),
                        null,       // TODO: Fill out
                        -1);        // TODO: Fill out
                }

                return function.Call(this, arguments);
            }
            else
            {
                throw new RuntimeException("Can only call functions and classes", null, -1);
            }
        }

        /// <summary>
        ///  Literal nodes hold atomic values that do not need to be further evaluated, so they can be returned
        ///  immediately.
        /// </summary>
        public object VisitLiteral(LiteralExpression literalNode)
        {
            return literalNode.Value;
        }

        /// <summary>
        ///  Apply a unary operator on the RHS expression.
        /// </summary>
        public object VistUnary(UnaryExpression unaryNode)
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

        public object VisitVariable(VariableExpression node)
        {
            return _currentEnvironment.Get(node.VariableName);
        }

        public object VisitAssignment(AssignmentExpression node)
        {
            var value = Evaluate(node.Value);
            _currentEnvironment.Set(node.VariableName, value);

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

    // TODO: FIX THE WAY RETURN WORKS!!!! The exception method is a huge hack and only here for early
    //       prototyping purposes! Manually return up the call chain rather than use the exception method.
    //       When a return statement is hit, all enqueued statement visitors need to check for pending return
    //       and abort.
    public class ReturnException : Exception
    {
        public ReturnException(object result)
        {
            Result = result;
        }

        public object Result { get; }
    }

}
