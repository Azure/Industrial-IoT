// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4 {
    using global::IdentityServer4.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Api resource repository interface
    /// </summary>
    public interface IResourceRepository {

        /// <summary>
        /// Create resource
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CreateAsync(Resource resource,
            CancellationToken ct = default);

        /// <summary>
        /// Get api resource and etag
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(Resource, string)> GetAsync(string resourceId,
            CancellationToken ct = default);

        /// <summary>
        /// Update resource
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateAsync(Resource resource, string etag = null,
            CancellationToken ct = default);

        /// <summary>
        /// Delete resource
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteAsync(string resourceName, string etag = null,
            CancellationToken ct = default);
    }
}