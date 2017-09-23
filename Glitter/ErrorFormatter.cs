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

namespace Glitter
{
    // TODO: Handle errors from outside of interactive mode

    /// <summary>
    ///  Formats error for views in command line (or log window) view.
    /// </summary>
    public class ErrorFormatter
    {
        public string InteractiveModeSourceCode { get; set; }

        public string Format(IEnumerable<UserCodeException> exceptions)
        {
            var messageText = new StringBuilder();

            foreach (var exception in exceptions)
            {
                bool isInteractiveError = true;     // TODO: Get this from exception.SourceFilePath == empty.

                // TODO: If not interactive mode, load source file code into a cache.
                if (!isInteractiveError)
                {
                    messageText.Append($"FILENAME:{exception.LineNumber} ");
                }

                messageText.Append("error: ");
                messageText.Append(exception.Message);
                messageText.AppendLine();

                // Extract offending text.
                //  TODO: Only extract the 40 or so most relevant characters in that line.
                //  TODO: Handle case where only col info is available.

                if (exception.LineNumber.HasValue)
                {
                    messageText.AppendLine(ExtractLine(InteractiveModeSourceCode, exception.LineNumber.Value));
                }
                

                // TODO:
                // Make an error like this:
                //
                // <file-name>:<line> error: message
                //  code here
                //     ^^^
                //
                // file-name and line should not be shown if not loaded from a file

                if (exception.LineNumber > 0)
                {
                    messageText.AppendFormat(" (line {0})", exception.LineNumber);
                }

                messageText.AppendLine();
            }

            return messageText.ToString();
        }

        // TODO: Convert to utility function, very good for unit testing!
        private string ExtractLine(string text, int lineNumber)
        {
            // Count to requested line.
            var currentLineNumber = 1;            // Lines use one based notation.
            var lineStartPos = 0;

            while (lineStartPos < text.Length && currentLineNumber < lineNumber)
            {
                if (text[lineStartPos++] == '\n')
                {
                    currentLineNumber++;
                }
            }

            // Make sure line start is valid.
            if (lineStartPos == text.Length && currentLineNumber != lineNumber)
            {
                return "<COULD NOT LOAD LINE EXCEPTION INFO WAS BAD>";
            }

            // Find end of current line.
            var lineEndPos = lineStartPos;

            while (lineEndPos < text.Length)
            {
                if (text[lineEndPos++] == '\n')
                {
                    // Move back to prevent capture of \n or \r\n.
                    if (lineEndPos > 1 && text[lineEndPos - 2] == '\r')
                    {
                        lineEndPos -= 2;
                    }
                    else
                    {
                        lineEndPos -= 1;
                    }
                }
            }

            // Extract current line.
            var requestedLine = text.Substring(lineStartPos, lineEndPos - lineStartPos);

            // TODO: Generate the ^^^ formatting for a line given start/end.
            // TODO: Handle case where end is on another line from start.

            return requestedLine;
        }
    }
}
