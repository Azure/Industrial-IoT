// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Base object extensions
    /// </summary>
    public static class ObjectEx {

        /// <summary>
        /// Make nullable version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="nil"></param>
        /// <returns></returns>
        public static T? ToNullable<T>(this T value, T nil) where T : struct {
            return EqualityComparer<T>.Default.Equals(value, nil) ? (T?)null : value;
        }

        /// <summary>
        /// Safe equals
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool EqualsSafe(this object obj, object that) {
            if (obj == that) {
                return true;
            }
            if (obj == null || that == null) {
                return false;
            }
            return obj.Equals(that);
        }

        /// <summary>
        /// Get default hash code for object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetHashSafe<T>(this T obj) {
            return EqualityComparer<T>.Default.GetHashCode(obj);
        }

        /// <summary>
        /// Get default hash code for object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetHashSafe<T>(this T? obj) where T : struct {
            return obj == null ? 0 : obj.GetHashCode();
        }

        /// <summary>
        /// Using type converter, convert type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T As<T>(this object value) {
            return (T)As(value, typeof(T));
        }

        /// <summary>
        /// Using type converter, convert type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object As(this object value, Type type) {
            if (value == null || value.GetType() == type) {
                return value;
            }
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFrom(value);
        }
    }
}
