// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    /// <summary>
    /// Exceptions extensions
    /// </summary>
    public static class AggregateExceptionEx {
        /// <summary>
        /// Returns first exception of specified type in exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static T GetFirstOf<T>(this Exception ex) where T : Exception {
            if (ex is T) {
                return (T)ex;
            }
            if (ex is AggregateException ae) {
                ae = ae.Flatten();
                foreach (var e in ae.InnerExceptions) {
                    var found = GetFirstOf<T>(e);
                    if (found != null) {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}
