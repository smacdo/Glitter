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
using System.Linq;
using Glitter.AST;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Glitter.Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void Assignment_Statement_With_Initializer()
        {

        }

        /// <summary>
        ///  Parse a very simple one statement binary expression.
        /// </summary>
        [TestMethod]
        public void Binary_Expression_Statement()
        {
            var statements = Parse("1 + 2;");

            Assert.AreEqual(1, statements.Count);
            Assert.IsInstanceOfType(statements[0], typeof(ExpressionStatement));
        }

        private IList<Statement> Parse(string code)
        {
            var scanner = new Scanner(code);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            parser.OnError += OnParserError;

            return parser.Parse();
        }

        private void OnParserError(object sender, ParseErrorEventArgs e)
        {
            throw e.Exception;
        }

    }
}
