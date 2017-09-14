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

namespace Glitter.AST
{
    public class AbstractSyntaxTreePrinter : IExpressionNodeVisitor<string>
    {
        public string Print(ExpressionNode expression)
        {
            return expression.Visit(this);
        }

        public string VisitBinaryExpressionNode(BinaryExpressionNode node)
        {
            return Parenthesize(node.Operator.Lexeme, node.Left, node.Right);
        }

        public string VisitGroupingNode(GroupingNode node)
        {
            return Parenthesize("group", node.Node);
        }

        public string VisitLiteralNode(LiteralNode node)
        {
            if (node.Value == null)
            {
                return "undefined";
            }

            return node.Value.ToString();
        }

        public string VisitUnaryNode(UnaryNode node)
        {
            return Parenthesize(node.Operator.Lexeme, node.Right);
        }

        public string VisitVariableNode(VariableNode node)
        {
            return Parenthesize(node.VariableName);
        }

        public string VisitAssignmentNode(AssignmentNode node)
        {
            return Parenthesize("=");
        }

        private string Parenthesize(string name, params ExpressionNode[] nodes)
        {
            var builder = new StringBuilder();
            builder.Append("(").Append(name);

            foreach (var n in nodes)
            {
                builder.Append(" ");
                builder.Append(n.Visit(this));
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}
