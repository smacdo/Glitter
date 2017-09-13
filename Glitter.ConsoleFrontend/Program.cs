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
using System.IO;

namespace Glitter.ConsoleFrontend
{
    /// <summary>
    ///  Command line interface for Glitter interpreter.
    /// </summary>
    public class Program
    {
        private static ConsoleColor _defaultForegroundColor;

        public static void Main(string[] args)
        {
            // Configure console.
            _defaultForegroundColor = Console.ForegroundColor;

            // Run input.
            // TODO: Handle interpreter exceptions by pretty printing.
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: glitter.exe [path/to/script.gli]");
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunInteractive();
            }
        }

        private static void RunFile(string fileName)
        {
            // TODO: Handle file load error.
            var code = File.ReadAllText(fileName);
            var interpreter = new Interpreter()
            {
                OnException = HandleException,
                StandardIn = Console.In,
                StandardOut = Console.Out
            };

            interpreter.Run(code);
        }

        private static void RunInteractive()
        {
            var interpreter = new Interpreter()
            {
                OnException = HandleException,
                StandardIn = Console.In,
                StandardOut = Console.Out
            };

            while (true)
            {
                Console.Write("> ");
                interpreter.Run(Console.ReadLine());
            }
        }

        private static void HandleException(object sender, InterpreterExceptionEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Exception: {0}", args.Message);

            if (!string.IsNullOrEmpty(args.What))
            {
                Console.WriteLine(" context: '{0}'", args.What);
            }
            
            if (args.LineNumber > 0)
            {
                Console.WriteLine(" line   : {0}", args.LineNumber);
            }
            
            Console.ForegroundColor = _defaultForegroundColor;
        }
    }
}