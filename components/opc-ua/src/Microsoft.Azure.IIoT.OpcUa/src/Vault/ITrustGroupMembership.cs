// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages trust relationships
    /// </summary>
    public interface ITrustGroupMembership {

        /// <summary>
        /// Assign entity to group.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entityId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddMemberAsync(string id, string entityId,
            CancellationToken ct = default);

        /// <summary>
        /// List all members of a group.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="maxPageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EntityListModel> ListMembersAsync(
            string id, string nextPageLink = null,
            int? maxPageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Remove entity from specified group.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entityId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveMemberAsync(string id,
            string entityId, CancellationToken ct = default);
    }
}
