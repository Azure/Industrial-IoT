// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System;
    using Opc.Ua;

    /// <summary>
    /// Datavalue extensions
    /// </summary>
    public static class DataValueEx { 

        /// <summary>
        /// Unpack with a default value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataValue"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(this DataValue dataValue, T defaultValue = default(T)) {
            if (dataValue == null) {
                return defaultValue;
            }
            if (StatusCode.IsNotGood(dataValue.StatusCode)) {
                return defaultValue;
            }
            var value = dataValue.Value;
            while (typeof(T).IsEnum) {
                try {
                    return (T)Enum.ToObject(typeof(T), value);
                }
                catch {
                    break;
                }
            }
            while (!typeof(T).IsInstanceOfType(value)) {
                try {
                    return value.As<T>();
                }
                catch {
                    break;
                }
            }
            try {
                return (T)value;
            }
            catch {
                return defaultValue;
            }
        }

        /// <summary>
        /// Unpack with a default value
        /// </summary>
        /// <param name="dataValue"></param>
        /// <param name="defaultValue"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Get(this DataValue dataValue, object defaultValue, Type type) {
            if (dataValue == null) {
                return defaultValue;
            }
            if (StatusCode.IsNotGood(dataValue.StatusCode)) {
                return defaultValue;
            }
            var value = dataValue.Value;
            while (type.IsEnum) {
                try {
                    return Enum.ToObject(type, value);
                }
                catch {
                    break;
                }
            }
            while (!type.IsInstanceOfType(value)) {
                try {
                    return value.As(type);
                }
                catch {
                    break;
                }
            }
            // TODO: try cast function...
            return defaultValue;
        }
    }
}
