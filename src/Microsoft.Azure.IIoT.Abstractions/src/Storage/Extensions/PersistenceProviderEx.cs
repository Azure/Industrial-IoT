// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Persistence provider extensions
    /// </summary>
    public static class PersistenceProviderEx {

        /// <summary>
        /// Writes key value to storage.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Task WriteAsync(this IPersistenceProvider provider,
            string key, dynamic value) => provider.WriteAsync(
                new Dictionary<string, dynamic> {
                    [key] = value
                });

        /// <summary>
        /// Run task that requires a parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="provider"></param>
        /// <param name="id"></param>
        /// <param name="task"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static async Task<T> RunTaskAsync<T, V>(this IPersistenceProvider provider,
            string id, Func<V, Task<T>> task, V defaultValue) where V : class {
            var name = (V)await provider.ReadAsync(id);
            if (name == null) {
                name = defaultValue;
            }
            var result = await task(name);
            await provider.WriteAsync(id, name);
            return result;
        }

        /// <summary>
        /// Run task that requires a string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <param name="id"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static Task<T> RunTaskAsync<T>(this IPersistenceProvider provider,
            string id, Func<string, Task<T>> task) =>
            provider.RunTaskAsync(id, task, StringEx.CreateUnique(10, id));

        /// <summary>
        /// Clear given keys
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static Task ClearAsync(this IPersistenceProvider provider,
            params string[] keys) =>
            provider.WriteAsync(keys.ToDictionary(k => k, v => (dynamic)null));
    }
}
