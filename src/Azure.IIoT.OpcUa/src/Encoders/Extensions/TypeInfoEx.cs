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
            if (typeInfo.ValueRank == ValueRanks.Scalar)
            {
                // For scalar values, try to retrieve a default.
                return TypeInfo.GetDefaultValue(builtInType);
            }
            if (typeInfo.ValueRank <= 1)
            {
                return Array.CreateInstance(
                    TypeInfo.GetSystemType(builtInType, -1) ??
                    typeof(object), 0);
            }
            return new Matrix(
                Array.CreateInstance(
                    TypeInfo.GetSystemType(builtInType, -1) ??
                        typeof(object),
                    new int[typeInfo.ValueRank]),
                builtInType);
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
                var systemType = TypeInfo.GetSystemType(typeInfo.BuiltInType,
                    typeInfo.ValueRank);
                if (typeInfo.BuiltInType == BuiltInType.Null)
                {
                    if (typeInfo.ValueRank == 1)
                    {
                        systemType = typeof(object[]);
                    }
                    else
                    {
                        return Variant.Null; // Matrix or scalar
                    }
                }
                else if (value is Array arr)
                {
                    try
                    {
                        var unboxed = Array.CreateInstance(
                            TypeInfo.GetSystemType(typeInfo.BuiltInType, -1), arr.Length);
                        Array.Copy(arr, unboxed, arr.Length);
                        value = unboxed;
                    }
                    catch (Exception ex)
                    {
                        aex.Add(ex);
                        value = arr;
                    }
                }
                if (typeInfo.ValueRank >= 2)
                {
                    systemType = typeof(Matrix);
                }
                var constructor = typeof(Variant).GetConstructor([
                    systemType
                ]);
                try
                {
                    if (constructor != null)
                    {
                        return (Variant)constructor.Invoke([value]);
                    }
                }
                catch (Exception ex)
                {
                    aex.Add(ex);
                }
                try
                {
                    return new Variant(value, typeInfo);
                }
                catch (Exception ex)
                {
                    aex.Add(ex);
                    throw new ArgumentException($"Cannot convert {value} " +
                        $"({value.GetType()}/{systemType}/{typeInfo}) to Variant.",
                        new AggregateException(aex));
                }
            }
            return var;
        }
    }
}
