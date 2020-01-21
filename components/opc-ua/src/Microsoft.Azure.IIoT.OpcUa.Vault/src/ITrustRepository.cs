// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Trust repo
    /// </summary>
    public interface ITrustRepository {

        /// <summary>
        /// Add relationship
        /// </summary>
        /// <param name="relationship"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustRelationshipModel> AddAsync(
            TrustRelationshipModel relationship, CancellationToken ct = default);

        /// <summary>
        /// List relationships where the entity is subject
        /// of a directional relationship defined by direction.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="direction"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TrustRelationshipListModel> ListAsync(string entityId,
            TrustDirectionType? direction = null, string nextPageLink = null,
            int? maxResults = null, CancellationToken ct = default);

        /// <summary>
        /// Delete relationships where subject has a directional
        /// relationship defined by direction with object.  If object
        /// entity is not specified, removes all relationships for the
        /// subject.
        /// </summary>
        /// <param name="subjectId"></param>
        /// <param name="direction"></param>
        /// <param name="objectId"></param>
        /// <param name="ct"></param>
        /// <returns>Relationships deleted</returns>
        Task DeleteAsync(string subjectId, TrustDirectionType? direction = null,
            string objectId = null, CancellationToken ct = default);
    }
}