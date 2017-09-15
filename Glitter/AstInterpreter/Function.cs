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
    // TODO: This hsould be in AST since it contains AST code and is used by the AST evaluator.
    //  (eg its not generic runtime code).
    // 
    // Plan: move AbstractSyntaxTreeEvaluator to Interpreter (rename old Interpreter to something else like
    //   InterpreterRunner?), place it in Interpreter namespace. Move this class over into that namespace.
    public class Function : ICallable
    {
        private FunctionDeclarationStatement _declaration;
        private Environment _closure;

        public Function(FunctionDeclarationStatement declaration, Environment closure)
        {
            _declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            _closure = closure ?? throw new ArgumentNullException(nameof(closure));
        }

        public int Arity => _declaration.Parameters.Count;

        public object Call(AbstractSyntaxTreeInterpreter evaluator, IList<object> arguments)
        {
            var environment = new Environment(_closure);

            for (int i = 0; i < _declaration.Parameters.Count; i++)
            {
                var parameterName = _declaration.Parameters[i];
                var parameterValue = arguments[i];

                environment.Define(parameterName, parameterValue);
            }

            // TODO: Rewrite this handling to NOT use exceptions.
            try
            {
                evaluator.ExecuteBlock(_declaration.Body, environment);
            }
            catch (ReturnException r)
            {
                return r.Result;
            }

            return null;
        }

        public override string ToString() => $"<function {_declaration.Name}>";
    }
}
