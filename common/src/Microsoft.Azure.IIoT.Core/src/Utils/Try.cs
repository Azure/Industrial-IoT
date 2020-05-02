// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Helper class to wrap operations in try catch
    /// </summary>
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
        public static T Op<T>(Func<T> action) {
            try {
                return action.Invoke();
            }
            catch {
                return default;
            }
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Task<bool> Async(Func<Task> action) {
            return action.Invoke()
                .ContinueWith(t => t.IsCompletedSuccessfully);
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<bool> Async(Func<CancellationToken, Task> action,
            CancellationToken ct) {
            return action.Invoke(ct)
                .ContinueWith(t => t.IsCompletedSuccessfully);
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Task<T> Async<T>(Func<Task<T>> action) {
            return action.Invoke()
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : default);
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<T> Async<T>(Func<CancellationToken, Task<T>> action,
            CancellationToken ct) {
            return action.Invoke(ct)
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : default);
        }

        /// <summary>
        /// Try all options until one is working
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
        public static async Task<T> Options<T>(params Func<Task<T>>[] options) {
#pragma warning restore IDE1006 // Naming Styles
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
        /// <param name="options"></param>
        /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
        public static async Task Options(params Func<Task>[] options) {
#pragma warning restore IDE1006 // Naming Styles
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
