// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;

    public static class Try {

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool Op(Action action) {
            try {
                action.Invoke();
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task<bool> Async(Func<Task> action) {
            try {
                await action.Invoke();
                return true;
            }
            catch {
                return false;
            }
        }


        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<bool> Async(Func<CancellationToken, Task> action, 
            CancellationToken ct) {
            try {
                await action.Invoke(ct);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Try all options until one is working
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<T> Options<T>(params Func<Task<T>>[] options) {
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
        /// Try all options until one is working
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task Options(params Func<Task>[] options) {
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
