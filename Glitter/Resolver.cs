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

namespace Glitter
{
    // TODO: Also add optimization here to stop looking up variable name in environment dictionary at runtime.
    //       Eg, when resolving a variable here we should set its "table index" as an offset rather than a name
    //       into a map.
    //
    //       Do this by changing how begin/end scope works. When entering a program, new function, or scope a new
    //       variable table should be created. Each enclosed block should create unique variables in the table
    //       rather than pushing new tables.
    //            eg   { a { a { a b } { a } }
    //                 a_0
    //                 a_0_0
    //                 a_0_0_0
    //                 b_0_0_0
    //                 a_0_1
    //
    //       At some point resolver can also do resolution, eg if it notices a is re-used between blocks then clear
    //       the value and re-use it.
    public class Resolver : IExpressionVisitor<object>, IStatementNodeVisitor<object>
    {
        // Track stack of scopes currently in scope (lol).
        private List<Dictionary<string, VariableState>> _scopes = new List<Dictionary<string, VariableState>>();
        private FunctionType _currentFunction = FunctionType.None;  // This will be used more later.

        public Resolver()
        {
            // Push a global scope at start.
            BeginScope();
        }

        public void Resolve(IList<Statement> statements)
        {
            foreach (var statement in statements)
            {
                Resolve(statement);
            }
        }

        private void Resolve(Statement statement)
        {
            statement.Visit(this);
        }

        private void Resolve(Expression expression)
        {
            expression.Visit(this);
        }


        public object VisitBlock(BlockStatemnt block)
        {
            BeginScope();
            Resolve(block.Statements);
            EndScope();

            return null;
        }

        public object VisitExpressionStatement(ExpressionStatement statement)
        {
            Resolve(statement.Expression);
            return null;
        }

        public object VisitVariableDeclaration(VariableDeclarationStatement statement)
        {
            // First declare the existence of a variable, but not its definition (eg initial value).
            Declare(statement.Name);

            // Resolve the initializer expression.
            if (statement.InitializerExpression != null)
            {
                Resolve(statement.InitializerExpression);
            }

            // Now define the variable.
            Define(statement.Name);
            return null;
        }

        public object VisitFunctionDeclaration(FunctionDeclarationStatement function)
        {
            // Declare and immediately define the function (since functions do not have initializers).
            Declare(function.Name);
            Define(function.Name);

            // Use helper method to resolve function (share code with class methods).
            ResolveFunction(function, FunctionType.Function);
            return null;
        }

        private void ResolveFunction(FunctionDeclarationStatement function, FunctionType type)
        {
            // Track enclosing function before replacing current function.
            var enclosingFunction = type;
            _currentFunction = type;

            // Start new scope for function.
            BeginScope();

            // Declare and then define each parameter since parameters do not have initializers.
            foreach (var parameter in function.Parameters)
            {
                Declare(parameter);
                Define(parameter);
            }

            // Resolve statements inside of function.
            Resolve(function.Body);

            // Destroy scope once function exits.
            EndScope();

            // Pop current function and restore the enclosing function.
            _currentFunction = enclosingFunction;
        }

        public object VisitIf(IfStatement statement)
        {
            Resolve(statement.Condition);
            Resolve(statement.ThenBranch);

            if (statement.ElseBranch != null)
            {
                Resolve(statement.ElseBranch);
            }

            return null;
        }

        public object VisitWhile(WhileStatement statement)
        {
            Resolve(statement.Condition);
            Resolve(statement.Body);
            return null;
        }

        public object VisitReturn(ReturnStatement statement)
        {
            // Is it safe to return?
            if (_currentFunction == FunctionType.None)
            {
                // TODO: Better erro.
                throw new InvalidOperationException("Cannot return from top level code");
            }

            if (statement.Expression != null)
            {
                Resolve(statement.Expression);
            }

            return null;
        }

        public object VisitPrint(PrintStatement statement)
        {
            Resolve(statement.Expression);
            return null;
        }
        
        public object VisitBinary(BinaryExpression binaryNode)
        {
            Resolve(binaryNode.Left);
            Resolve(binaryNode.Right);
            return null;
        }

        public object VisitLogical(LogicalExpression node)
        {
            Resolve(node.Left);
            Resolve(node.Right);
            return null;
        }
        
        public object VisitGrouping(GroupingExpression groupNode)
        {
            Resolve(groupNode.Expression);
            return null;
        }

        public object VisitCall(CallExpression node)
        {
            Resolve(node.Callee);

            foreach (var argument in node.Arguments)
            {
                Resolve(argument);
            }

            return null;
        }

        public object VisitLiteral(LiteralExpression literalNode)
        {
            return null;
        }
        
        public object VistUnary(UnaryExpression unaryNode)
        {
            Resolve(unaryNode.Right);
            return null;
        }

        public object VisitVariable(VariableExpression node)
        {
            // Check that variable does not reference itself during initialization.
            if (_scopes.Count > 0 && IsDeclaredButNotDefinedInCurrentScope(node.VariableName))
            {
                // TODO: Better error.
                throw new InvalidOperationException("Cannot reference self in initialization");
            }

            node.ScopeDistance = ResolveScopeForLocal(node.VariableName, node);
            return null;
        }

        public object VisitAssignment(AssignmentExpression node)
        {
            Resolve(node.Value);
            node.ScopeDistance = ResolveScopeForLocal(node.VariableName, node);

            return null;
        }

        /// <summary>
        ///  Declare the existence of a variable in the current scope (first stage).
        /// </summary>
        /// <remarks>
        ///  When a scope is created, a variable must first be declared, then its initializer resolved and finally
        ///  the variable defined.
        /// 
        ///  A variable is declared when encountered (var a), but not defined until after the initializer
        ///  expression is resolved. This allows the resolver to detect and raise an error when a variable
        ///  is put into scope and its initializer refers to itself.
        /// </remarks>
        /// <param name="name">Name of the variable.</param>
        private void Declare(string name)
        {
            var currentScope = _scopes[_scopes.Count - 1];

            if (currentScope.ContainsKey(name))
            {
                // TODO: Better error. Also raise error rather than exception? Or something to allow multiple
                //       errors for reporting.
                throw new InvalidOperationException("Variable already declared in this scope");
            }

            // Add to current scope as declared but not defined.
            currentScope.Add(name, VariableState.Declared);
        }

        /// <summary>
        ///  Define the existence of a variable in the current scope (second stage).
        /// </summary>
        /// <remarks>
        ///  When a scope is created, a variable must first be declared, then its initializer resolved and finally
        ///  the variable defined.
        /// 
        ///  A variable is declared when encountered (var a), but not defined until after the initializer
        ///  expression is resolved. This allows the resolver to detect and raise an error when a variable
        ///  is put into scope and its initializer refers to itself.
        /// </remarks>
        /// <param name="name">Name of the variable.</param>
        private void Define(string name)
        {
            // TODO: Check declared first.
            
            var currentScope = _scopes[_scopes.Count - 1];
            currentScope[name] = VariableState.Defined;
        }

        private bool IsDeclaredButNotDefinedInCurrentScope(string name)
        {
            var currentScope = _scopes[_scopes.Count - 1];
            var state = VariableState.None;

            if (currentScope.TryGetValue(name, out state))
            {
                return state == VariableState.Declared;
            }
            else
            {
                return false;
            }
        }

        private int ResolveScopeForLocal(string name, Expression expression)    // TODO: Why is expression passed?
        {
            // Search through active scopes looking for variable definition. Start from the innermost enclosing scope
            // and continue outward.
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                var scope = _scopes[i];

                if (scope.ContainsKey(name))
                {
                    return _scopes.Count - 1 - i;
                }
            }

            // Did not find the name in any of the local enclosing scopes. Assume this a global variable reference.
            return -1;
        }

        private void BeginScope()
        {
            // Push a new scope.
            _scopes.Add(new Dictionary<string, VariableState>());
        }

        private void EndScope()
        {
            // Pop the top scope.
            _scopes.RemoveAt(_scopes.Count - 1);
        }

        private enum VariableState
        {
            None,
            Declared,
            Defined
        }

        private enum FunctionType
        {
            None,
            Function
        }
    }
}
