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
        // TODO: Test cases where numbers are followed by invalid chars and make sure they break.
        //       ex: 401a 401sdasd 401$
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Scanner_Throws_Exception_If_Given_Null_Text()
        {
            new Scanner(null);
        }

        [TestMethod]
        public void Whitespace_Policy_Sets_Emit_Whitespace_Property()
        {
            // Disable whitespace emit.
            var s = new Scanner(string.Empty, string.Empty, WhitespaceTokenPolicy.None);
            Assert.IsFalse(s.EmitWhitespaceTokens);

            // Enable whitespace emit.
            s = new Scanner(string.Empty, string.Empty, WhitespaceTokenPolicy.Emit);
            Assert.IsTrue(s.EmitWhitespaceTokens);
        }

        [TestMethod]
        public void Scanner_Adds_Start_And_End_Index_To_Each_Token()
        {
            var tokens = ScanAll("+ 123 \"hello\"\r\n3 4");

            Assert.AreEqual(6, tokens.Length);

            Assert.AreEqual(TokenType.Plus, tokens[0].Type);
            Assert.AreEqual(0, tokens[0].StartIndex);
            Assert.AreEqual(1, tokens[0].Length);

            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual(2, tokens[1].StartIndex);
            Assert.AreEqual(3, tokens[1].Length);

            Assert.AreEqual(TokenType.String, tokens[2].Type);
            Assert.AreEqual(6, tokens[2].StartIndex);
            Assert.AreEqual(7, tokens[2].Length);

            Assert.AreEqual(TokenType.Number, tokens[3].Type);
            Assert.AreEqual(15, tokens[3].StartIndex);
            Assert.AreEqual(1, tokens[3].Length);

            Assert.AreEqual(TokenType.Number, tokens[4].Type);
            Assert.AreEqual(17, tokens[4].StartIndex);
            Assert.AreEqual(1, tokens[4].Length);
        }

        [TestMethod]
        public void Scanner_Always_Emits_End_Of_File_At_End()
        {
            // EOF emitted after valid tokens?
            //  "1 2" => Number, Number, EOF
            var tokens = ScanAll("1 2");

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual(TokenType.EndOfFile, tokens[2].Type);

            // EOF emitted after single token?
            tokens = ScanAll("1");

            Assert.AreEqual(2, tokens.Length);
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual(TokenType.EndOfFile, tokens[1].Type);

            // EOF emitted even if text empty?
            tokens = ScanAll("");

            Assert.AreEqual(1, tokens.Length);
            Assert.AreEqual(TokenType.EndOfFile, tokens[0].Type);
        }

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
            Assert.AreEqual(TokenType.Whitespace, ScanOne(" ", WhitespaceTokenPolicy.Emit).Type);
            Assert.AreEqual(TokenType.Whitespace, ScanOne("\t", WhitespaceTokenPolicy.Emit).Type);
            Assert.AreEqual(TokenType.Whitespace, ScanOne("\n", WhitespaceTokenPolicy.Emit).Type);
            Assert.AreEqual(TokenType.Whitespace, ScanOne("\r\n", WhitespaceTokenPolicy.Emit).Type);
        }

        [TestMethod]
        public void Merges_Multiple_Whitespace_Into_Single_Token()
        {
            var tokens = ScanAll(" \t0  \r\n 1", WhitespaceTokenPolicy.Emit);

            Assert.AreEqual(TokenType.Whitespace, tokens[0].Type);
            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual(TokenType.Whitespace, tokens[2].Type);
            Assert.AreEqual(TokenType.Number, tokens[3].Type);
        }

        [TestMethod]
        public void Only_Emit_Whitespace_Tokens_When_Configured()
        {
            // Enable whitespace handling and check if working as intended.
            var tokens = ScanAll(" \t0  \r\n 1  ", WhitespaceTokenPolicy.Emit);

            Assert.AreEqual(6, tokens.Length);
            Assert.AreEqual(TokenType.Whitespace, tokens[0].Type);
            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual(TokenType.Whitespace, tokens[2].Type);
            Assert.AreEqual(TokenType.Number, tokens[3].Type);
            Assert.AreEqual(TokenType.Whitespace, tokens[4].Type);
            Assert.AreEqual(TokenType.EndOfFile, tokens[5].Type);

            // Disable whitespace handling.
            tokens = ScanAll(" \t0  \r\n 1  ", WhitespaceTokenPolicy.None);

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual(TokenType.EndOfFile, tokens[2].Type);
        }

        [TestMethod]
        public void Scan_Multiple_Values()
        {
            var results = ScanAll("+ - == <");

            Assert.AreEqual(TokenType.Plus, results[0].Type);
            Assert.AreEqual(TokenType.Minus, results[1].Type);
            Assert.AreEqual(TokenType.EqualEqual, results[2].Type);
            Assert.AreEqual(TokenType.Less, results[3].Type);
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

        [TestMethod]
        public void Scan_Multiple_Ints()
        {
            var results = ScanAll("2 -1 33");

            Assert.AreEqual(TokenType.Number, results[0].Type);
            Assert.AreEqual(2, results[0].LiteralNumber);

            Assert.AreEqual(TokenType.Number, results[1].Type);
            Assert.AreEqual(-1, results[1].LiteralNumber);

            Assert.AreEqual(TokenType.Number, results[2].Type);
            Assert.AreEqual(33, results[2].LiteralNumber);
        }

        [TestMethod]
        public void Scan_Strings()
        {
            var results = ScanAll("\"scott\"  \"hello world\" \"emoji🎉\"");

            Assert.AreEqual(4, results.Length);

            Assert.AreEqual(TokenType.String, results[0].Type);
            Assert.AreEqual("scott", results[0].LiteralString);
            Assert.AreEqual("\"scott\"", results[0].Lexeme);

            Assert.AreEqual(TokenType.String, results[1].Type);
            Assert.AreEqual("hello world", results[1].LiteralString);
            Assert.AreEqual("\"hello world\"", results[1].Lexeme);

            Assert.AreEqual(TokenType.String, results[2].Type);
            Assert.AreEqual("emoji🎉", results[2].LiteralString);
            Assert.AreEqual("\"emoji🎉\"", results[2].Lexeme);
        }

        [TestMethod]
        public void Scan_Multiline_String()
        {
            var results = ScanAll("\"hello\nworld\n!!\"");

            Assert.AreEqual(2, results.Length);

            Assert.AreEqual(TokenType.String, results[0].Type);
            Assert.AreEqual("hello\nworld\n!!", results[0].LiteralString);
        }

        [TestMethod]
        [ExpectedException(typeof(UnterminatedStringException))]
        public void Scan_Unterminated_String_Throws_Exception()
        {
            var results = ScanAll("\"hello\nworld\n!! ");
        }

        [TestMethod]
        public void Scan_Identifiers()
        {
            var results = ScanAll("hello_world f00bar");

            Assert.AreEqual(3, results.Length);

            Assert.AreEqual(TokenType.Identifier, results[0].Type);
            Assert.AreEqual("hello_world", results[0].LiteralIdentifier);
            Assert.AreEqual("hello_world", results[0].Lexeme);

            Assert.AreEqual(TokenType.Identifier, results[1].Type);
            Assert.AreEqual("f00bar", results[1].LiteralIdentifier);
            Assert.AreEqual("f00bar", results[1].Lexeme);
        }

        [TestMethod]
        [Ignore("TODO: Support emoji identifiers")]
        public void Scan_Emoji_Identifier()
        {
            var results = ScanAll("🎉 w🎉🎉t");

            Assert.AreEqual(3, results.Length);

            Assert.AreEqual(TokenType.Identifier, results[0].Type);
            Assert.AreEqual("🎉", results[0].LiteralIdentifier);
            Assert.AreEqual("🎉", results[0].Lexeme);

            Assert.AreEqual(TokenType.Identifier, results[1].Type);
            Assert.AreEqual("w🎉🎉t", results[1].LiteralIdentifier);
            Assert.AreEqual("w🎉🎉t", results[1].Lexeme);
        }

        [TestMethod]
        public void All_Text_Until_Line_Comment_End_Of_Line_Is_Whitespace()
        {
            // + .
            var tokens = ScanAll("+ // -\n.");

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual(TokenType.Plus, tokens[0].Type);
            Assert.AreEqual(TokenType.Dot, tokens[1].Type);
        }

        [TestMethod]
        public void All_Text_Until_Block_Comment_Ends_Is_Whitespace()
        {
            var tokens = ScanAll("+ /* 1 2 \n 3 \n*/ 4");

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual(TokenType.Plus, tokens[0].Type);
            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual(4, tokens[1].LiteralNumber);
        }

        [TestMethod]
        [ExpectedException(typeof(UnterminatedBlockCommentException))]
        public void Unterminated_Block_Comment_Throws_Exception()
        {
            ScanAll("+ /* 1 2 \n 3 \n 4");
        }

        private Token[] ScanAll(string text, WhitespaceTokenPolicy? policy = null)
        {
            var s = new Scanner(text, string.Empty, policy ?? WhitespaceTokenPolicy.None);
            return s.ScanTokens().ToArray();
        }

        private Token ScanOne(string text, WhitespaceTokenPolicy? policy = null)
        {
            var a = ScanAll(text, policy);
            return a?.First() ?? throw new Exception("Scan all did not return values");
        }
    }
}
