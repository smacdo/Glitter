﻿/*
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
using Glitter.AST;

namespace Glitter
{
    public static class NativeFunctions
    {
        public static void Register(Environment env)
        {
            Register(env, new ClockNativeFunction());
        }
        
        private static void Register(Environment env, INativeFunction func)
        {
            env.Define(func.Name, func);
        }
    }

    public interface INativeFunction : ICallable
    {
        string Name { get; }
    }

    public class ClockNativeFunction : INativeFunction
    {
        public string Name => "clock";

        public int Arity => 0;

        public object Call(AbstractSyntaxTreeEvaluator evaluator, IList<object> arguments)
        {
            return DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;
        }
    }
}
