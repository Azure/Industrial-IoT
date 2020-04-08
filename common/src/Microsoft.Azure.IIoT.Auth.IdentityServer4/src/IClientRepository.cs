// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4 {
    using global::IdentityServer4.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client repository interface
    /// </summary>
    public interface IClientRepository {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CreateAsync(Client client,
            CancellationToken ct = default);

        /// <summary>
        /// Delete client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteAsync(string clientId, string etag = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get client and etag
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(Client, string)> GetAsync(string clientId,
            CancellationToken ct = default);

        /// <summary>
        /// Update client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateAsync(Client client, string etag = null,
            CancellationToken ct = default);
    }
}