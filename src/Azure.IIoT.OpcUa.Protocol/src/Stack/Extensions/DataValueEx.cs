// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System;

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
        public static T GetValueOrDefault<T>(this DataValue dataValue,
            T defaultValue = default) {
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
    }
}
