// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Persistence provider extensions
    /// </summary>
    public static class PersistenceProviderEx {

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
            string id, Func<string, Task<T>> task) {
            return provider.RunTaskAsync(id, task, StringEx.CreateUnique(10, id));
        }
    }
}
