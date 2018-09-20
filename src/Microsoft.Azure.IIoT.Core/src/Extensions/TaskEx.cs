// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Threading.Tasks {

    /// <summary>
    /// Task extensions
    /// </summary>
    public static class TaskEx {

        /// <summary>
        /// Execute fallback task when this task fails.
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
