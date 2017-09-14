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
    ///  Holds variable bindings to bind variables to values.
    /// </summary>
    public class Environment
    {
        private Dictionary<string, object> _values = new Dictionary<string, object>();
        private Environment _enclosing = null;

        public Environment()
        {
        }

        public Environment(Environment enclosing)
        {
            _enclosing = enclosing ?? throw new ArgumentNullException(nameof(enclosing));
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

        public object Get(string name)
        {
            if (!_values.TryGetValue(name, out object value))
            {
                if (_enclosing != null)
                {
                    return _enclosing.Get(name);
                }
                else
                {
                    throw new RuntimeException("Undefined variable", name, 0);
                }
            }

            return value;
        }

        public void Set(string variableName, object value)
        {
            if (_values.ContainsKey(variableName))
            {
                _values[variableName] = value;
            }
            else if (_enclosing != null)
            {
                _enclosing.Set(variableName, value);
            }
            else
            {
                throw new RuntimeException("Undefined variable", variableName, 0);
            }
        }
    }
}
