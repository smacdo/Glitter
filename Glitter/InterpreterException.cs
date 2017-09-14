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
    // TODO: Change from what to a model where we pass the buffer, a start offset and an end offset. The output can
    //       properly display this.
    /// <summary>
    ///  Base class for Glitter interpreter exceptions.
    /// </summary>
    public class InterpreterException : System.Exception
    {
        /// <summary>
        ///  Get what input caused the exception.
        /// </summary>
        public string What { get; set; }

        /// <summary>
        ///  Get what line the exception occured on.
        /// </summary>
        public int LineNumber { get; set; }

        public InterpreterException(string message)
            : this(message, null, 0)
        {
        }

        public InterpreterException(string message, string what)
            : this(message, what, 0)
        {
        }

        public InterpreterException(string message, string what, int lineNumber)
            : base(message)
        {
            What = what;
            LineNumber = lineNumber;
        }
    }

    public class UnexpectedCharacterException : InterpreterException
    {
        public UnexpectedCharacterException(char c, int line)
            : base("Unexpected character", Convert.ToString(c), line)
        {
        }
    }

    public class UnterminatedStringException : InterpreterException
    {
        public UnterminatedStringException(int line)
            : base("Unterminated string", string.Empty, line)
        {
        }
    }

    public class ParserException : InterpreterException
    {
        public ParserException(string message, string what, int line)
            : base(message, what, line)
        {
        }
    }

    public class RuntimeException : InterpreterException
    {
        public RuntimeException(string message, string what, int line)
            : base(message, what, line)
        {
        }
    }
}
