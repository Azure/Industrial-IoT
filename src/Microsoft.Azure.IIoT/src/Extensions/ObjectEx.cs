// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.ComponentModel;

    public static class ObjectEx {

        /// <summary>
        /// Using type converter, convert type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T As<T>(this object value) {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)converter.ConvertFrom(value);
        }

        /// <summary>
        /// Using type converter, convert type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object As(this object value, Type type) {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFrom(value);
        }
    }
}
