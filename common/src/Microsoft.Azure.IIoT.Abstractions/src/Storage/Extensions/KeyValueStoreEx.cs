// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Microsoft.Azure.IIoT.Exceptions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Key value store extensions
    /// </summary>
    public static class KeyValueStoreEx {

        /// <summary>
        /// Find key value
        /// </summary>
        /// <param name="store"></param>
        /// <param name="key"></param>
        /// <param name="contentType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> FindKeyValueAsync(this IKeyValueStore store,
            string key, string contentType = null, CancellationToken ct = default) {
            try {
                return await store.GetKeyValueAsync(key, contentType, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }
    }
}