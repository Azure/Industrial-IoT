// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Threading.Tasks;
    using System;
    using System.Threading;

    /// <summary>
    /// Cache abstraction
    /// </summary>
    public interface ICache {

        /// <summary>
        /// Set value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SetAsync(string key, byte[] value,
            DateTimeOffset expiration, CancellationToken ct = default);

        /// <summary>
        /// Get value from cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte[]> GetAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Remove value from cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveAsync(string key, CancellationToken ct = default);
    }
}
