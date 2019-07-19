// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;

    /// <summary>
    /// Typeinfo extensions
    /// </summary>
    public static class TypeInfoEx {

        /// <summary>
        /// Returns default value for type
        /// </summary>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        public static object GetDefaultValue(this TypeInfo typeInfo) {
            var builtInType = typeInfo.BuiltInType;
            if (typeInfo.ValueRank == ValueRanks.Scalar) {
                // For scalar values, try to retrieve a default.
                return TypeInfo.GetDefaultValue(builtInType);
            }
            if (typeInfo.ValueRank <= 1) {
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
        public static Variant CreateVariant(this TypeInfo typeInfo, object value) {
            if (value == null) {
                value = typeInfo.GetDefaultValue();
            }
            if (!(value is Variant var)) {
                if (typeInfo.BuiltInType == BuiltInType.Enumeration) {
                    typeInfo = new TypeInfo(BuiltInType.Int32,
                        typeInfo.ValueRank);
                }
                var systemType = TypeInfo.GetSystemType(typeInfo.BuiltInType,
                    typeInfo.ValueRank);
                if (typeInfo.BuiltInType == BuiltInType.Null) {
                    if (typeInfo.ValueRank == 1) {
                        systemType = typeof(object[]);
                    }
                    else {
                        return Variant.Null; // Matrix or scalar
                    }
                }
                else if (value is object[] boxed) {
                    try {
                        var array = Array.CreateInstance(
                            TypeInfo.GetSystemType(typeInfo.BuiltInType, -1), boxed.Length);
                        Array.Copy(boxed, array, boxed.Length);
                        value = array;
                    }
                    catch {
                        value = boxed;
                    }
                }
                if (typeInfo.ValueRank >= 2) {
                    systemType = typeof(Matrix);
                }
                var constructor = typeof(Variant).GetConstructor(new Type[] {
                    systemType
                });
                if (constructor != null) {
                    var = (Variant)constructor.Invoke(new object[] { value });
                }
                else {
                    var = new Variant(value, typeInfo);
                }
            }
            return var;
        }
    }
}
