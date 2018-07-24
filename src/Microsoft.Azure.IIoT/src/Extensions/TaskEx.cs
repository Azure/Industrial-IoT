// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Threading.Tasks {

    public static class TaskEx {

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(this CancellationToken cancellationToken) {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static async Task<T> FallbackWhen<T>(this Task<T> task,
            Func<T, bool> condition, Func<Task<T>> fallback) {
            try {
                var result = await task;
                if (!condition(result)) {
                    return result;
                }
                return await fallback();
            }
            catch {
                return await fallback();
            }
        }
    }
}
