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
    // TODO: Rather than allowing callers to create new environments with references to the parent,
    //       add methods to do this and track the enclosing environments in one class.
    //       Why? This way the class can efficiently add and remove environment scopes, and when referring
    //       to parent scopes via distance indices we can use random indexing rather than linked list walks.
    
    /// <summary>
    ///  Holds variable bindings to bind variables to values.
    /// </summary>
    public class Environment
    {
        private Dictionary<string, object> _values = new Dictionary<string, object>();
        private Environment _enclosing = null;
        private System.IO.TextReader _standardInput = null;
        private System.IO.TextWriter _standardOutput = null;

        /// <summary>
        ///  Constructor.
        /// </summary>
        public Environment()
        {
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="enclosing"></param>
        public Environment(Environment enclosing)
        {
            _enclosing = enclosing ?? throw new ArgumentNullException(nameof(enclosing));
        }

        /// <summary>
        ///  TODO: When native classes are supported switch to storing this in the _values table.
        /// </summary>
        public System.IO.TextReader StandardInput
        {
            get { return _enclosing != null && _standardInput == null ? _enclosing.StandardInput : _standardInput; }
            set { _standardInput = value; }
        }

        /// <summary>
        ///  TODO: When native classes are supported switch to storing this in the _values table.
        /// </summary>
        public System.IO.TextWriter StandardOutput
        {
            get { return _enclosing != null && _standardOutput == null ? _enclosing.StandardOutput : _standardOutput; }
            set { _standardOutput = value; }
        }

        /// <summary>
        ///  Define a new variable with a name and value.
        /// </summary>
        /// <param name="name">Name of the variable.</param>
        /// <param name="value">Value of the variable.</param>
        public void Define(string name, object value)
        {
            // TODO: Consider adding error checking for redefining variables.
            _values[name] = value;
        }

        /// <summary>
        ///  Get a variable by name in the current environment and if it does not exist throws an exception rather
        ///  than go to the enclosing environment.
        /// </summary>
        /// <param name="name">Name of the variable to look up.</param>
        /// <returns>Value of the object.</returns>
        public object Get(string name)
        {
            if (!_values.TryGetValue(name, out object value))
            {
                throw new UndefinedVariableException(name);
            }

            return value;
        }
        
        /// <summary>
        ///  Get a variable by name after traversing through the given number of enclosing environments.
        /// </summary>
        /// <param name="name">Binding name.</param>
        /// <param name="distance">Number of enclosing environments to travel through.</param>
        /// <returns>Value of the object.</returns>
        public object GetAt(string name, int distance)
        {
            var current = this;

            for (int i = 0; i < distance; i++)
            {
                current = current._enclosing;
            }

            if (current._values.TryGetValue(name, out object value))
            {
                return value;
            }
            else
            {
                throw new UndefinedVariableException(name);
            }
        }

        // TODO: Add a recursive Get function and add comment that it is slow.

        /// <summary>
        ///  Set a variable by name after traversing through the given number of enclosing environments.
        /// </summary>
        /// <param name="name">Binding name.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="distance">Number of enclosing environments to travel through.</param>
        /// <returns>Value of the object.</returns>
        public void Set(string variableName, object value)
        {
            if (_values.ContainsKey(variableName))
            {
                _values[variableName] = value;
            }
            else
            {
                throw new UndefinedVariableException(variableName);
            }
        }

        /// <summary>
        ///  Get a variable by name after traversing through the given number of enclosing environments.
        /// </summary>
        /// <param name="variableName">Binding name.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="distance">Number of enclosing environments to travel through.</param>
        /// <returns>Value of the object.</returns>
        public void SetAt(string variableName, object value, int distance)
        {
            var current = this;

            for (int i = 0; i < distance; i++)
            {
                current = current._enclosing;
            }

            if (_values.ContainsKey(variableName))      // TODO: Optimize into one step?
            {
                _values[variableName] = value;
            }
            else
            {
                throw new UndefinedVariableException(variableName);
            }
        }

        // TODO: Add a recursive Set function and add comment that it is slow.
    }

    public class UndefinedVariableException : Exception
    {
        public UndefinedVariableException(string variableName)
            : base($"Undefined variable ${variableName}")
        {
            VariableName = variableName;
        }

        public string VariableName { get;  }
    }
}
