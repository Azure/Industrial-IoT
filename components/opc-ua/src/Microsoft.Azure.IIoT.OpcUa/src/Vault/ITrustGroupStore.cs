// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Trust group management
    /// </summary>
    public interface ITrustGroupStore {

        /// <summary>
        /// Create a new root trust group with default settings.
        /// Default settings depend on the chosen certificate type.
        /// </summary>
        /// <param name="request">Registration</param>
        /// <param name="ct"></param>
        Task<TrustGroupRegistrationResultModel> CreateRootAsync(
            TrustGroupRootCreateRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Create a new trust group with default settings as sub
        /// group of another trust group.
        /// Default settings depend on the chosen certificate type.
        /// </summary>
        /// <param name="request">Registration</param>
        /// <param name="ct"></param>
        Task<TrustGroupRegistrationResultModel> CreateGroupAsync(
            TrustGroupRegistrationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Get the group registration info.
        /// </summary>
        /// <param name="id">The policy Id</param>
        /// <param name="ct"></param>
        /// <returns>The configuration</returns>
        Task<TrustGroupRegistrationModel> GetGroupAsync(
            string id, CancellationToken ct = default);

        /// <summary>
        /// Update settings of a trust group. The settings are
        /// valid for future group operations.
        /// </summary>
        /// <param name="id">The policy Id</param>
        /// <param name="request">The update request</param>
        /// <param name="ct"></param>
        /// <returns>The updated policy</returns>
        Task UpdateGroupAsync(string id,
            TrustGroupRegistrationUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Get information about all trust groups.
        /// </summary>
        /// <param name="nextPageLink"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns>The configurations</returns>
        Task<TrustGroupRegistrationListModel> ListGroupsAsync(
            string nextPageLink = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Query groups
        /// </summary>
        /// <param name="query"></param>
        /// <param name="maxPageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustGroupRegistrationListModel> QueryGroupsAsync(
            TrustGroupRegistrationQueryModel query, int? maxPageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Delete a trust group if it has no children.
        /// </summary>
        /// <param name="id">The policy Id</param>
        /// <param name="ct"></param>
        Task DeleteGroupAsync(string id,
            CancellationToken ct = default);
    }
}
