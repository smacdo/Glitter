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
        private static string _userCode = string.Empty;
        //private static StringBuilder _interactiveModeCode = new StringBuilder();

        /// <summary>
        ///  Program entry point.
        /// </summary>
        /// <param name="args">Program arguments.</param>
        public static void Main(string[] args)
        {
            // Configure console.
            _defaultForegroundColor = Console.ForegroundColor;

            // Run input.
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

                // TODO: Fix this hack by reading the source file in error formatter and loading it rather than shoving
                // it into the interactive source buffer.
                _userCode = code;

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
            Console.WriteLine(FormatInteractiveIntro());

            // Create an interpreter for this interactive console session.
            var interpreter = new ExecutionSession(Console.In, Console.Out)
            {
                OnException = HandleException
            };

            // Keep asking for input until the user quits.
            var keepRunning = true;

            while (keepRunning)
            {
                // Get user input.
                Console.Write(">>> ");
                var userInput = Console.ReadLine();

                // Keep track of code history.
                _userCode = userInput;

                // Did the user input any special interactive keywords?
                var cleanUserInput = userInput.Trim().ToLower();

                if (cleanUserInput == "!quit")
                {
                    // User would like to quit the console and exit.
                    keepRunning = false;
                }
                else
                {
                    // Nope, this looks like regular glitter code. Try running it and see what happens!
                    interpreter.Run(userInput);
                }
            }
        }

        /// <summary>
        ///  Pretty print exceptions when they happen.
        /// </summary>
        private static void HandleException(object sender, ExecutionSessionErrorArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            var formatter = new ErrorFormatter() { SourceCode = _userCode };
            Console.Write(formatter.Format(args.Exceptions));
            
            Console.ForegroundColor = _defaultForegroundColor;
        }

        /// <summary>
        ///  Return a formatted introduction to the interactive console when started.
        /// </summary>
        private static string FormatInteractiveIntro()
        {
            var output = new StringBuilder();

            var majorVersion = Constants.MajorVersion;
            var minorVersion = Constants.MinorVersion;
            var patchVersion = Constants.PatchVersion;

            output.Append(Constants.GlitterLangName);
            output.AppendFormat(" {0}.{1}.{2}", majorVersion, minorVersion, patchVersion);
            output.AppendLine(" Interactive Console");

            output.AppendLine("Type \"!quit\" to exit the interactive console");

            return output.ToString();
        }
    }
}