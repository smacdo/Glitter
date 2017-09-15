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

namespace Glitter.AST
{
    public interface IStatementNodeVisitor<T>
    {
        T VisitPrint(PrintStatement statement);
        T VisitExpressionStatement(ExpressionStatement statement);
        T VisitVariableDeclaration(VariableDeclarationStatement statement);
        T VisitBlock(BlockStatemnt statement);
        T VisitIf(IfStatement statement);
        T VisitWhile(WhileStatement statement);
        T VisitFunctionDeclaration(FunctionDeclarationStatement statement);
        T VisitReturn(ReturnStatement staetment);
    }
}
