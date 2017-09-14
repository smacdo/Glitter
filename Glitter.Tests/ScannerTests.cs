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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Glitter.Tests
{
    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public void Scan_Special_Tokens()
        {
            Assert.AreEqual(TokenType.LeftParen, ScanOne("(").Type);
            Assert.AreEqual(TokenType.RightParen, ScanOne(")").Type);
            Assert.AreEqual(TokenType.LeftBrace, ScanOne("{").Type);
            Assert.AreEqual(TokenType.RightBrace, ScanOne("}").Type);
            Assert.AreEqual(TokenType.Comma, ScanOne(",").Type);
            Assert.AreEqual(TokenType.Dot, ScanOne(".").Type);
            Assert.AreEqual(TokenType.Minus, ScanOne("-").Type);
            Assert.AreEqual(TokenType.Plus, ScanOne("+").Type);
            Assert.AreEqual(TokenType.Semicolon, ScanOne(";").Type);
            Assert.AreEqual(TokenType.Star, ScanOne("*").Type);
            Assert.AreEqual(TokenType.Bang, ScanOne("!").Type);
            Assert.AreEqual(TokenType.Less, ScanOne("<").Type);
            Assert.AreEqual(TokenType.Greater, ScanOne(">").Type);
            Assert.AreEqual(TokenType.Equal, ScanOne("=").Type);
            Assert.AreEqual(TokenType.Slash, ScanOne("/").Type);

            Assert.AreEqual(TokenType.EqualEqual, ScanOne("==").Type);
            Assert.AreEqual(TokenType.BangEqual, ScanOne("!=").Type);
            Assert.AreEqual(TokenType.LessEqual, ScanOne("<=").Type);
            Assert.AreEqual(TokenType.GreaterEqual, ScanOne(">=").Type);
        }

        [TestMethod]
        public void Scan_Whitespace()
        {
            Assert.AreEqual(TokenType.Whitespace, ScanOne(" ").Type);
            Assert.AreEqual(TokenType.Whitespace, ScanOne("\t").Type);
            Assert.AreEqual(TokenType.Whitespace, ScanOne("\n").Type);
            Assert.AreEqual(TokenType.Whitespace, ScanOne("\r\n").Type);
        }

        [TestMethod]
        public void Scan_Multiple_Values()
        {
            var results = ScanAll("+ - == <");

            Assert.AreEqual(TokenType.Plus, results[0].Type);
            Assert.AreEqual(TokenType.Whitespace, results[1].Type);
            Assert.AreEqual(TokenType.Minus, results[2].Type);
            Assert.AreEqual(TokenType.Whitespace, results[3].Type);
            Assert.AreEqual(TokenType.EqualEqual, results[4].Type);
            Assert.AreEqual(TokenType.Whitespace, results[5].Type);
            Assert.AreEqual(TokenType.Less, results[6].Type);
        }

        [TestMethod]
        public void Scan_Zero_As_A_Number()
        {
            var a = ScanOne("0");

            Assert.AreEqual(TokenType.Number, a.Type);
            Assert.AreEqual(0, a.LiteralNumber);
        }

        [TestMethod]
        public void Scan_Integer_As_A_Number()
        {
            var a = ScanOne("42");

            Assert.AreEqual(TokenType.Number, a.Type);
            Assert.AreEqual(42, a.LiteralNumber);
        }

        [TestMethod]
        public void Scan_Negative_Integer_As_A_Number()
        {
            var a = ScanOne("-22");

            Assert.AreEqual(TokenType.Number, a.Type);
            Assert.AreEqual(-22, a.LiteralNumber);
        }

        [TestMethod]
        public void Scan_Decimal_As_A_Number()
        {
            var a = ScanOne("2.04");

            Assert.AreEqual(TokenType.Number, a.Type);
            Assert.AreEqual(2.04, a.LiteralNumber);
        }

        private Token[] ScanAll(string text)
        {
            var s = new Scanner(text);
            s.IgnoreWhitespace = false;

            return s.ScanTokens().ToArray();
        }

        private Token ScanOne(string text)
        {
            var a = ScanAll(text);
            return a?.First() ?? throw new Exception("Scan all did not return values");
        }
    }
}
