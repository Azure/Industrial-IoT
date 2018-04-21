// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Common.Utils {
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public static class Fallback {

        /// <summary>
        /// Run with fallback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<T> Run<T>(params Func<Task<T>>[] options) {
            var exceptions = new List<Exception>();
            foreach (var option in options) {
                try {
                    return await option();
                }
                catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Run with fallback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task Run(params Func<Task>[] options) {
            var exceptions = new List<Exception>();
            foreach (var option in options) {
                try {
                    await option();
                    return;
                }
                catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
