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
    public interface IExpressionVisitor<T>
    {
        T VisitBinary(BinaryExpression binaryNode);
        T VisitGrouping(GroupingExpression groupNode);
        T VisitLiteral(LiteralExpression literalNode);
        T VistUnary(UnaryExpression unaryNode);
        T VisitVariable(VariableExpression node);
        T VisitAssignment(AssignmentExpression node);
        T VisitLogical(LogicalExpression node);
        T VistiCall(CallExpression node);
    }
}
