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
using System.Text;

namespace Glitter.ConsoleFrontend
{
    /// <summary>
    ///  Command line interface for Glitter interpreter.
    /// </summary>
    public class Program
    {
        private static ConsoleColor _defaultForegroundColor;
        private static StringBuilder _interactiveModeCode;

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

        /// <summary>
        ///  Load file as text.
        /// </summary>
        /// <param name="fileName">Path to file.</param>
        private static void RunFile(string fileName)
        {
            try
            {
                var code = File.ReadAllText(fileName);
                var interpreter = new ExecutionSession(Console.In, Console.Out)
                {
                    OnException = HandleException
                };

                interpreter.Run(code);
            }
            catch (FileLoadException e)
            {
                Console.Error.WriteLine("Failed to load file: {0}", fileName);
            }
        }

        /// <summary>
        ///  Run Glitter in interactive mode.
        /// </summary>
        private static void RunInteractive()
        {
            var interpreter = new ExecutionSession(Console.In, Console.Out)
            {
                OnException = HandleException
            };

            _interactiveModeCode = new StringBuilder();

            while (true)
            {
                Console.Write("> ");

                var userInput = Console.ReadLine();
                _interactiveModeCode.AppendLine(userInput);

                interpreter.Run(userInput);
            }
        }

        private static void HandleException(object sender, ExecutionSessionErrorArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            var formatter = new ErrorFormatter() { InteractiveModeSourceCode = _interactiveModeCode.ToString() };
            Console.Write(formatter.Format(args.Exceptions));
            
            Console.ForegroundColor = _defaultForegroundColor;
        }
    }
}