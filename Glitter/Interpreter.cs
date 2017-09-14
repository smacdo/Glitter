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
using System.Text;
using Glitter.AST;

namespace Glitter
{
    /// <summary>
    ///  Glitter run time interpreter.
    /// </summary>
    public class Interpreter
    {
        private Environment _environment = new Environment();

        public EventHandler<InterpreterExceptionEventArgs> OnException { get; set; }
        public TextReader StandardIn { get; set; }
        public TextWriter StandardOut { get; set; }

        /// <summary>
        ///  Run Glitter code.
        /// </summary>
        /// <param name="code">Code to execute.</param>
        public void Run(string code)
        {
            var scanner = new Scanner(code);

            try
            {
                var tokens = scanner.ScanTokens();

                var parser = new Parser(tokens);
                parser.OnError += OnParserError;

                var statements = parser.Parse();

                if (!parser.HasParseErrors)
                {
                    try
                    {
                        var evaluator = new AbstractSyntaxTreeEvaluator(statements, _environment);
                        var result = evaluator.Evaluate();

                        //Console.WriteLine(Stringify(result));
                    }
                    catch (RuntimeException e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (InterpreterException e)
            {
                if (!HandleExcepton(e))
                {
                    throw;
                }
            }
        }

        private void PrintSyntaxTree(ExpressionNode root)
        {
            if (root == null)
            {
                Console.Error.WriteLine("Parse result was null!");
            }
            else
            {
                Console.WriteLine(new AbstractSyntaxTreePrinter().Print(root));
            }
        }

        private void OnParserError(object sender, ParseErrorEventArgs e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.What);
        }

        /// <summary>
        ///  Invoke exception event listeners that an exception happened.
        /// </summary>
        /// <param name="e">The exception that happened.</param>
        /// <returns>True if at least one listener is called, false otherwise.</returns>
        private bool HandleExcepton(InterpreterException e)
        {
            if (OnException != null)
            {
                OnException.Invoke(this, new InterpreterExceptionEventArgs()
                {
                    LineNumber = e.LineNumber,
                    Message = e.Message,
                    What = e.What,
                });

                return true;
            }

            return false;
        }

        /// <summary>
        ///  Write a line of text to standard out, if possible.
        /// </summary>
        /// <param name="text">Line of text to write.</param>
        private void WriteLine(string text)
        {
            if (StandardOut != null)
            {
                StandardOut.WriteLine(text);
            }
        }
    }

    public class InterpreterExceptionEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string What { get; set; }
        public int LineNumber { get; set; }
    }
}
