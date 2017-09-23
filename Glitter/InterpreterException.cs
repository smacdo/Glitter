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
    ///  Base class for Glitter interpreter exceptions.
    /// </summary>
    public class UserCodeException : System.Exception
    {
        /// <summary>
        ///  Get index of first character in the code that generated this exception.
        /// </summary>
        public int? FirstCharIndex { get; }

        /// <summary>
        ///  Get the number of characters psat that first char index in the code that generated this exception.
        /// </summary>
        public int? CharLength { get; }

        /// <summary>
        ///  Get the line number in the code that generated this exception.
        /// </summary>
        public int? LineNumber { get; }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public UserCodeException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="firstCharIndex">First character in code causing this error.</param>
        /// <param name="charLength">Number of characters in the code causingg this error.</param>
        public UserCodeException(string message, int firstCharIndex, int charLength)
            : base(message)
        {
            FirstCharIndex = firstCharIndex;
            CharLength = charLength;
        }

        public UserCodeException(string message, int lineNumber)
            : base(message)
        {
            LineNumber = lineNumber;
        }
    }

    #region Scanner exceptions
    public class ScannerException : UserCodeException
    {
        public ScannerException(string message)
            : base(message)
        {
        }

        public ScannerException(string message, int firstCharIndex, int charLength)
            : base(message, firstCharIndex, charLength)
        {
        }

        public ScannerException(string message, Token token)
            : base(message, token.StartIndex, token.Length)
        {
        }
    }

    public class UnexpectedCharacterException : ScannerException
    {
        public UnexpectedCharacterException(char c, int firstCharIndex)
            : base($"Unexpected character '{c}'", firstCharIndex, 1)
        {
        }
    }

    public class UnterminatedStringException : ScannerException
    {
        public UnterminatedStringException(int firstCharIndex)
            : base("Unterminated string", firstCharIndex, 1)
        {
        }
    }

    public class UnterminatedBlockCommentException : ScannerException
    {
        public UnterminatedBlockCommentException(int firstCharIndex)
            : base("Unterminated block comment", firstCharIndex, 2)
        {
        }
    }
    #endregion
    #region Parser exceptions
    public class ParserException : UserCodeException
    {
        public ParserException(string message, Token token)
            : base(message, token.StartIndex, token.Length)
        {
        }
    }
    #endregion

    public class RuntimeException : UserCodeException
    {
        public RuntimeException(string message, int lineNumber)
            : base(message, lineNumber)
        {
        }
    }
}
