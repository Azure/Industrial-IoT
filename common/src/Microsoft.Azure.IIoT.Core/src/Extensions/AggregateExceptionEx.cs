// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Text;

    /// <summary>
    /// Exceptions extensions
    /// </summary>
    public static class AggregateExceptionEx {

        /// <summary>
        /// Combine messages
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        public static string GetCombinedExceptionMessage(this AggregateException ae) {
            if (ae == null) {
                return null;
            }
            var sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions) {
                sb.AppendLine(string.Concat("E: ", e.Message));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Combine stack trace
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        public static string GetCombinedExceptionStackTrace(this AggregateException ae) {
            if (ae == null) {
                return null;
            }
            var sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions) {
                sb.AppendLine(string.Concat("StackTrace: ", e.StackTrace));
            }
            return sb.ToString();
        }

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
