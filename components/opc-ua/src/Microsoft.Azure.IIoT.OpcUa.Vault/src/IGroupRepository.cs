// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Group repo
    /// </summary>
    public interface IGroupRepository {

        /// <summary>
        /// Add group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustGroupRegistrationModel> AddAsync(
            TrustGroupRegistrationModel group,
            CancellationToken ct = default);

        /// <summary>
        /// Get group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustGroupRegistrationModel> FindAsync(string groupId,
            CancellationToken ct = default);

        /// <summary>
        /// Update group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustGroupRegistrationModel> UpdateAsync(string groupId,
            Func<TrustGroupRegistrationModel, bool> predicate,
            CancellationToken ct = default);

        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustGroupRegistrationModel> DeleteAsync(string groupId,
            Func<TrustGroupRegistrationModel, bool> predicate,
            CancellationToken ct = default);

        /// <summary>
        /// Query groups
        /// </summary>
        /// <param name="query"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustGroupRegistrationListModel> QueryAsync(
            TrustGroupRegistrationQueryModel query,
            string nextPageLink = null, int? maxResults = null,
            CancellationToken ct = default);
    }
}