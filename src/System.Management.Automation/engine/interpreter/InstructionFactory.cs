/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation.
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A
 * copy of the license can be found in the License.html file at the root of this distribution. If
 * you cannot locate the  Apache License, Version 2.0, please send an email to
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/


using System.Collections.Concurrent;
#if !CLR2
using BigInt = System.Numerics.BigInteger;
#endif

#if CORECLR
// Used for 'GetField' which is not available under 'Type' in CoreClR but provided as an extension method in 'System.Reflection.TypeExtensions'
using System.Reflection;
#endif

using System.Runtime.CompilerServices;
using System.Threading;

//using Microsoft.Scripting.Math;

namespace System.Management.Automation.Interpreter
{
    internal abstract class InstructionFactory
    {
        private static ConditionalWeakTable<Type, InstructionFactory> s_factories;

        internal static InstructionFactory GetFactory(Type type)
        {
            if (s_factories == null)
            {
                var factories = new ConditionalWeakTable<Type, InstructionFactory>();
                factories.Add(typeof(object), InstructionFactory<object>.Factory);
                factories.Add(typeof(bool), InstructionFactory<bool>.Factory);
                factories.Add(typeof(byte), InstructionFactory<byte>.Factory);
                factories.Add(typeof(sbyte), InstructionFactory<sbyte>.Factory);
                factories.Add(typeof(short), InstructionFactory<short>.Factory);
                factories.Add(typeof(ushort), InstructionFactory<ushort>.Factory);
                factories.Add(typeof(int), InstructionFactory<int>.Factory);
                factories.Add(typeof(uint), InstructionFactory<uint>.Factory);
                factories.Add(typeof(long), InstructionFactory<long>.Factory);
                factories.Add(typeof(ulong), InstructionFactory<ulong>.Factory);
                factories.Add(typeof(float), InstructionFactory<float>.Factory);
                factories.Add(typeof(double), InstructionFactory<double>.Factory);
                factories.Add(typeof(char), InstructionFactory<char>.Factory);
                factories.Add(typeof(string), InstructionFactory<string>.Factory);
                factories.Add(typeof(BigInt), InstructionFactory<BigInt>.Factory);

                Interlocked.CompareExchange(ref s_factories, factories, null);
            }

            return s_factories.GetValue(type,
                t => (InstructionFactory)typeof(InstructionFactory<>).MakeGenericType(t).GetField("Factory").GetValue(null));
        }

        protected internal abstract Instruction GetArrayItem();
        protected internal abstract Instruction SetArrayItem();
        protected internal abstract Instruction TypeIs();
        protected internal abstract Instruction TypeAs();
        protected internal abstract Instruction DefaultValue();
        protected internal abstract Instruction NewArray();
        protected internal abstract Instruction NewArrayInit(int elementCount);
    }

    internal sealed class InstructionFactory<T> : InstructionFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly InstructionFactory Factory = new InstructionFactory<T>();

        private Instruction _getArrayItem;
        private Instruction _setArrayItem;
        private Instruction _typeIs;
        private Instruction _defaultValue;
        private Instruction _newArray;
        private Instruction _typeAs;
        private Instruction[] _newArrayInit;
        // This number is somewhat arbitrary - trying to avoid some gc without keeping
        // objects (instructions) around that aren't used that often.
        private const int MaxArrayInitElementCountCache = 32;

        private InstructionFactory() { }

        protected internal override Instruction GetArrayItem()
        {
            return _getArrayItem ?? (_getArrayItem = new GetArrayItemInstruction<T>());
        }

        protected internal override Instruction SetArrayItem()
        {
            return _setArrayItem ?? (_setArrayItem = new SetArrayItemInstruction<T>());
        }

        protected internal override Instruction TypeIs()
        {
            return _typeIs ?? (_typeIs = new TypeIsInstruction<T>());
        }

        protected internal override Instruction TypeAs()
        {
            return _typeAs ?? (_typeAs = new TypeAsInstruction<T>());
        }

        protected internal override Instruction DefaultValue()
        {
            return _defaultValue ?? (_defaultValue = new DefaultValueInstruction<T>());
        }

        protected internal override Instruction NewArray()
        {
            return _newArray ?? (_newArray = new NewArrayInstruction<T>());
        }

        protected internal override Instruction NewArrayInit(int elementCount)
        {
            if (elementCount < MaxArrayInitElementCountCache)
            {
                if (_newArrayInit == null)
                {
                    _newArrayInit = new Instruction[MaxArrayInitElementCountCache];
                }

                return _newArrayInit[elementCount] ?? (_newArrayInit[elementCount] = new NewArrayInitInstruction<T>(elementCount));
            }
            return new NewArrayInitInstruction<T>(elementCount);
        }
    }
}
