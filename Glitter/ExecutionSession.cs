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
using System.IO;
using System.Linq;
using System.Text;
using Glitter.AST;
using Glitter.AstInterpreter;

namespace Glitter
{
    /// <summary>
    ///  Glitter run time interpreter.
    /// </summary>
    public class ExecutionSession
    {
        private Environment _rootEnvironment = new Environment();
        private List<UserCodeException> _parseExceptions = new List<UserCodeException>();

        public EventHandler<ExecutionSessionErrorArgs> OnException { get; set; }
        public TextReader StandardIn { get { return _rootEnvironment.StandardInput; } }
        public TextWriter StandardOut { get { return _rootEnvironment.StandardOutput; } }

        public ExecutionSession(TextReader standardIn, TextWriter standardOut)
        {
            _rootEnvironment.StandardInput = standardIn ?? throw new ArgumentNullException(nameof(standardIn));
            _rootEnvironment.StandardOutput = standardOut ?? throw new ArgumentNullException(nameof(standardOut));

            NativeFunctions.Register(_rootEnvironment);
        }

        /// <summary>
        ///  Run Glitter code.
        /// </summary>
        /// <param name="code">Code to execute.</param>
        /// <returns>The final result of running the code.</returns>
        public void Run(string code)
        {
            Run(code, string.Empty);
        }

        public void Run(string code, string sourceFilePath)
        {
            try
            {
                // Convert code text into tokens.
                var scanner = new Scanner(code, sourceFilePath);
                var tokens = scanner.ScanTokens();

                // Reset parser errors before running parser.
                _parseExceptions.Clear();

                // Parse tokens into abstract syntax tree.
                var parser = new Parser(tokens);
                parser.OnError += OnParserError;

                var statements = parser.Parse();

                // Do not execute the AST if there were errors in the code.
                if (_parseExceptions.Count > 0)
                {
                    if (!HandleExceptions(_parseExceptions))
                    {
                        throw _parseExceptions.First();
                    }

                    return;
                }

                // Resolve variable references.
                var resolver = new Resolver();
                resolver.Resolve(statements);

                // Evaluate the abstract syntax tree.
                var evaluator = new AbstractSyntaxTreeInterpreter(statements, _rootEnvironment);
                var result = evaluator.Execute();

                // TODO: Use a callback to allow the caller to potentially print the result.
            }
            catch (UserCodeException e)
            {
                if (!HandleException(e))
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///  Handle parser errors by adding them to a list of errors.
        /// </summary>
        private void OnParserError(object sender, ParseErrorEventArgs e)
        {
            _parseExceptions.Add(e.Exception);
        }

        /// <summary>
        ///  Inform any exception event listeners that an exception happened.
        /// </summary>
        /// <param name="exception">The exception that happened.</param>
        /// <returns>True if at least one listener is called, false otherwise.</returns>
        private bool HandleException(UserCodeException exception)
        {
            return HandleExceptions(new List<UserCodeException>() { exception });
        }

        /// <summary>
        ///  Inform any exception event listeners that an exception happened.
        /// </summary>
        /// <param name="exceptions">A list of exceptions that happened.</param>
        /// <returns>True if at least one listener is called, false otherwise.</returns>
        private bool HandleExceptions(IEnumerable<UserCodeException> exceptions)
        {
            if (OnException != null)
            {
                OnException.Invoke(this, new ExecutionSessionErrorArgs(exceptions));
                return true;
            }

            return false;
        }
    }

    public class ExecutionSessionErrorArgs : EventArgs
    {
        public ExecutionSessionErrorArgs(IEnumerable<UserCodeException> exceptions)
        {
            Exceptions = exceptions;
        }

        public IEnumerable<UserCodeException> Exceptions { get; }
    }
}
