// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Typeinfo extensions
    /// </summary>
    public static class TypeInfoEx
    {
        /// <summary>
        /// Returns default value for type
        /// </summary>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        public static object GetDefaultValue(this TypeInfo typeInfo)
        {
            var builtInType = typeInfo.BuiltInType;
            var elementType = TypeInfo.GetSystemType(builtInType)?.Type ??
                typeof(object);
            if (typeInfo.ValueRank == ValueRanks.Scalar)
            {
                // For scalar values, try to retrieve a default.
                return TypeInfo.GetDefaultValue(builtInType);
            }
            if (typeInfo.ValueRank <= 1)
            {
                return Array.CreateInstance(elementType, 0);
            }
            //
            // A matrix is built from a flat element array and its dimensions
            // rather than from a multidimensional array, because creating one
            // from a runtime type requires dynamic code and is not available
            // when the application is compiled ahead of time. Every dimension
            // is zero here, so the flat array is empty either way.
            //
            return new Matrix(Array.CreateInstance(elementType, 0), builtInType,
                new int[typeInfo.ValueRank]);
        }

        /// <summary>
        /// Create a variant from a value whose type is only known at run time.
        /// </summary>
        /// <remarks>
        /// The single argument <see cref="Variant"/> constructor falls back to
        /// reflection for types it cannot cast directly, which requires dynamic
        /// code and is not available when the application is compiled ahead of
        /// time. Deriving the type info first selects the same non-reflective
        /// cast the constructor uses for every type it can handle.
        /// </remarks>
        /// <param name="value">Value to wrap.</param>
        /// <returns>The variant holding the value.</returns>
        public static Variant ToVariant(object value)
        {
            if (value is null)
            {
                return Variant.Null;
            }
            if (value is Variant variant)
            {
                return variant;
            }
            return new Variant(value, TypeInfo.Construct(value));
        }

        /// <summary>
        /// Create Variant
        /// </summary>
        /// <param name="typeInfo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Variant CreateVariant(this TypeInfo typeInfo, object value)
        {
            value ??= typeInfo.GetDefaultValue();
            if (value is not Variant var)
            {
                var aex = new List<Exception>();
                if (typeInfo.BuiltInType == BuiltInType.Enumeration)
                {
                    typeInfo = new TypeInfo(BuiltInType.Int32, typeInfo.ValueRank);
                }
                if (typeInfo.BuiltInType == BuiltInType.Null)
                {
                    if (typeInfo.ValueRank != 1)
                    {
                        return Variant.Null; // Matrix or scalar
                    }
                }
                else if (value is Array arr)
                {
                    try
                    {
                        var unboxed = Array.CreateInstance(
                            TypeInfo.GetSystemType(typeInfo.BuiltInType)?.Type ??
                                typeof(object), arr.Length);
                        Array.Copy(arr, unboxed, arr.Length);
                        value = unboxed;
                    }
                    catch (Exception ex)
                    {
                        aex.Add(ex);
                        value = arr;
                    }
                }
                //
                // The variant is constructed from the value and its type info
                // rather than by locating the matching constructor overload
                // for the runtime system type. Constructing an array type from
                // a runtime type and invoking a constructor reflectively both
                // require dynamic code, which is not available when the
                // application is compiled ahead of time, and the type info
                // already carries everything the variant needs.
                //
                try
                {
                    return new Variant(value, typeInfo);
                }
                catch (Exception ex)
                {
                    aex.Add(ex);
                    throw new ArgumentException($"Cannot convert {value} " +
                        $"({value.GetType()}/{typeInfo}) to Variant.",
                        new AggregateException(aex));
                }
            }
            return var;
        }
    }
}
