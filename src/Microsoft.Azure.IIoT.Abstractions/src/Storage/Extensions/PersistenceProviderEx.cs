// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
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
