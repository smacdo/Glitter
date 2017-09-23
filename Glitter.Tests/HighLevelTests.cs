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
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace Glitter.Tests
{
    /// <summary>
    /// Summary description for HighLevelTests
    /// </summary>
    [TestClass]
    public class HighLevelTests
    {
        [TestMethod]
        public void Print_Hello_World()
        {
            Assert.AreEqual("Hello World\r\n", Execute("print \"Hello World\";"));
        }

        private string Execute(string code, string input = null)
        {

            var outputWriter = new StringWriter();
            var inputReader = new StringReader(input ?? string.Empty);

            var session = new ExecutionSession(inputReader, outputWriter)
            {
                OnException = OnException
            };

           
            session.Run(code);
            return outputWriter.ToString();
        }

        private void OnException(object sender, ExecutionSessionErrorArgs e)
        {
            throw e.Exceptions.First();
        }
    }
}
