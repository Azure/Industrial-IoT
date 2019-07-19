// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manage trust lists for entities
    /// </summary>
    public interface ITrustListServices {

        /// <summary>
        /// Adds a trust relationship between the entity and a
        /// trustee.  The result will be that the entity will
        /// trust the certificate of the trusted entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trustedId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddTrustRelationshipAsync(string entityId,
            string trustedId, CancellationToken ct = default);

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="untrustedId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveTrustRelationshipAsync(string entityId,
            string untrustedId, CancellationToken ct = default);

        /// <summary>
        /// Lists trusted certificates
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="maxPageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateListModel> ListTrustedCertificatesAsync(
            string entityId, string nextPageLink = null,
            int? maxPageSize = null, CancellationToken ct = default);
    }
}
