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
    /// <summary>
    ///  Formats error for views in command line (or log window) view.
    /// </summary>
    public class ErrorFormatter
    {
        // TODO: This will need to be fixed when Glitter supports importing other files.
        //  (eg there will be more than one source code file).
        public string SourceCode { get; set; }

        /// <summary>
        ///  Format user code errors.
        /// </summary>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public string Format(IEnumerable<UserCodeException> exceptions)
        {
            var messageText = new StringBuilder();

            // Iterate through each exception that was reported and format.
            foreach (var exception in exceptions)
            {
                // TODO: If not interactive mode, load source file code into a cache.
                messageText.AppendLine($"error: {exception.Message}");

                // Extract offending line of text from the exception.
                //  TODO: Only extract the 40 or so most relevant characters in that line.
                var sourceCode = SourceCode;
                int lineNumber = 0;

                if (exception.LineNumber.HasValue)
                {
                    lineNumber = exception.LineNumber.Value;
                }
                else if (exception.FirstCharIndex.HasValue)
                {
                    lineNumber = GetLineNumberForOffset(sourceCode, exception.FirstCharIndex.Value);
                }

                // Extract the line of text.
                if (lineNumber > 0)
                {
                    var extractedLine = ExtractLine(sourceCode, lineNumber);
                    messageText.AppendLine(extractedLine);
                }

                // If the error contains character offset info, generate a "carrot highlighter" to show the user where
                // the error happened.
                if (exception.FirstCharIndex.HasValue)
                {
                    messageText.AppendLine(
                        GenerateErrorCarrotHighlighter(
                            sourceCode,
                            exception.FirstCharIndex.Value,
                            exception.CharLength.Value));
                }                
            }

            return messageText.ToString();
        }

        /// <summary>
        ///  Generate a carrot highlighter, which is a set of ^^^ characters that appear in the same position as a
        ///  specified substring.
        ///  TODO: Handle cases where carrotStartIndex + carrotLength is longer the current line.
        /// </summary>
        /// <param name="text">Text to highlight.</param>
        /// <param name="carrotStartIndex">Index of the first character in the carrot highlighter.</param>
        /// <param name="carrotLength">Length of the ccarrot highlighter.</param>
        /// <returns>String that shows the carrot highlighter from the start of the line.</returns>
        public static string GenerateErrorCarrotHighlighter(
            string text,
            int carrotStartIndex,
            int carrotLength)
        {
            // Check parameters.
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (carrotStartIndex < 0 || carrotStartIndex > text.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(carrotStartIndex));
            }

            if (carrotLength < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(carrotLength));
            }
            
            // Find where the carrot highlighter begins from the start of the line that contains carrotStartIndex.
            // Make sure to account for both spaces and tabs.
            var output = new StringBuilder();

            GetLineOffsetsForPosition(
                        text,
                        carrotStartIndex,
                        out int spaceOffset,
                        out int tabOffset);

            for (int i = 0; i < spaceOffset; i++)
            {
                output.Append(" ");
            }

            for (int i = 0; i < tabOffset; i++)
            {
                output.Append("\t");
            }

            for (int i = 0; i < carrotLength; i++)
            {
                output.Append("^");
            }

            return output.ToString();
        }

        /// <summary>
        ///  Get the number of single character spaces and tab character spaces preceding a position in text for that
        ///  line.
        /// </summary>
        /// <param name="text">Text to search.</param>
        /// <param name="position">Position in the text.</param>
        /// <param name="spaceOffset">Number of single character spaces before position on position's line.</param>
        /// <param name="tabOffset">Number of tab character spaces before position on position's line.</param>
        public static void GetLineOffsetsForPosition(
            string text,
            int position,
            out int spaceOffset,
            out int tabOffset)
        {
            // Check parameters.
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (position < 0 || position > text.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            // Iterate through all characters in the input text until the offset is reached. Keep track of the
            // number of characters past the last seen newline.
            int currentPosition = 0;

            spaceOffset = 0;
            tabOffset = 0;

            while (currentPosition < position)
            {
                char c = text[currentPosition++];

                switch (c)
                {
                    case '\n':
                        spaceOffset = 0;
                        tabOffset = 0;
                        break;

                    case '\t':
                        tabOffset++;
                        break;

                    default:
                        spaceOffset++;
                        break;
                }
            }
        }

        /// <summary>
        ///  Get the line number for an offset in a text buffer.
        /// </summary>
        /// <param name="text">Text buffer to search.</param>
        /// <param name="offset">Character offset in the text buffer.</param>
        /// <returns>The number of newlines before the character offset.</returns>
        public static int GetLineNumberForOffset(string text, int offset)
        {
            // Check parameters.
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (offset < 0 || offset > text.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            // Iterate through all characters in the input text until the offset is reached. Count the number of
            // newlines seen and return it as the line number.
            int currentPosition = 0;
            int newlineCount = 1;               // Line numbering is one based, not zero based.

            while (currentPosition < offset)
            {
                if (text[currentPosition++] == '\n')
                {
                    newlineCount++;
                }
            }

            return newlineCount;
        }

        /// <summary>
        ///  Extract the text for a given line number in a text buffer.
        ///  TODO: Handle case where end is on another line from start.
        ///  TODO: Set max length for the extracted line.
        /// </summary>
        /// <param name="text">The text buffer to search.</param>
        /// <param name="lineNumber">The line number to extract.</param>
        /// <returns>Line of text from the text buffer.</returns>
        public static string ExtractLine(string text, int lineNumber)
        {
            // Check valid arguments.
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

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
                return null;
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
                        break;
                    }
                    else
                    {
                        lineEndPos -= 1;
                        break;
                    }
                }
            }

            // Extract current line.
            return text.Substring(lineStartPos, lineEndPos - lineStartPos);
        }
    }
}
