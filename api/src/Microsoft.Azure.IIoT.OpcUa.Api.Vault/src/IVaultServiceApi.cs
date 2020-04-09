// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Vault Api calls.
    /// </summary>
    public interface IVaultServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Get information about all groups.
        /// </summary>
        /// <param name="nextPageLink">optional, link to next page</param>
        /// <param name="pageSize">optional, the maximum number of result
        /// per page</param>
        /// <param name="ct"></param>
        /// <returns>The configurations</returns>
        Task<TrustGroupListApiModel> ListGroupsAsync(string nextPageLink,
            int? pageSize, CancellationToken ct = default);

        /// <summary>
        /// Get group information.
        /// </summary>
        /// <param name="groupId">The group id</param>
        /// <param name="ct"></param>
        /// <returns>The configuration</returns>
        Task<TrustGroupRegistrationApiModel> GetGroupAsync(string groupId,
            CancellationToken ct = default);

        /// <summary>
        /// Update group configuration.
        /// </summary>
        /// <param name="groupId">The group id</param>
        /// <param name="model">The group configuration</param>
        /// <param name="ct"></param>
        /// <returns>The configuration</returns>
        Task UpdateGroupAsync(string groupId, TrustGroupUpdateRequestApiModel model,
            CancellationToken ct = default);

        /// <summary>
        /// Create new root group.
        /// </summary>
        /// <param name="request">The create request</param>
        /// <param name="ct"></param>
        /// <returns>The group registration response</returns>
        Task<TrustGroupRegistrationResponseApiModel> CreateRootAsync(
            TrustGroupRootCreateRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Create new sub-group.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns>The group registration response</returns>
        Task<TrustGroupRegistrationResponseApiModel> CreateGroupAsync(
            TrustGroupRegistrationRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Create a new Issuer CA Certificate.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="ct"></param>
        /// <returns>The new Issuer CA certificate</returns>
        Task<X509CertificateApiModel> RenewIssuerCertificateAsync(string groupId,
            CancellationToken ct = default);

        /// <summary>
        /// Delete a group configuration.
        /// </summary>
        /// <param name="groupId">The group id</param>
        /// <param name="ct"></param>
        /// <returns>The group configuration</returns>
        Task DeleteGroupAsync(string groupId, CancellationToken ct = default);

        /// <summary>
        /// Get Issuer CA Certificate chain.
        /// </summary>
        /// <returns>The Issuer CA certificate chain</returns>
        Task<X509CertificateChainApiModel> GetIssuerCertificateChainAsync(
            string serialNumber, CancellationToken ct = default);

        /// <summary>
        /// Get Issuer CA CRL chain.
        /// </summary>
        /// <param name="serialNumber">The certificate serial Number</param>
        /// <param name="ct"></param>
        /// <returns>The Issuer CA CRL chain</returns>
        Task<X509CrlChainApiModel> GetIssuerCrlChainAsync(string serialNumber,
            CancellationToken ct = default);

        /// <summary>
        /// Create a certificate request with a certificate signing request (CSR).
        /// </summary>
        /// <param name="model">The signing request parameters</param>
        /// <param name="ct"></param>
        /// <returns>The certificate request id</returns>
        Task<StartSigningRequestResponseApiModel> StartSigningRequestAsync(
            StartSigningRequestApiModel model, CancellationToken ct = default);

        /// <summary>
        /// Fetch certificate signing request result.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="ct"></param>
        /// <returns>
        /// The state, the issued Certificate and the private key, if available.
        /// </returns>
        Task<FinishSigningRequestResponseApiModel> FinishSigningRequestAsync(
            string requestId, CancellationToken ct = default);

        /// <summary>
        /// Create a certificate request with a new key pair.
        /// </summary>
        /// <param name="model">The new key pair request parameters
        /// </param>
        /// <param name="ct"></param>
        /// <returns>The certificate request id</returns>
        Task<StartNewKeyPairRequestResponseApiModel> StartNewKeyPairRequestAsync(
            StartNewKeyPairRequestApiModel model, CancellationToken ct = default);

        /// <summary>
        /// Fetch key pair request result.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="ct"></param>
        /// <returns>
        /// The state, the issued Certificate and the private key,
        /// if available.
        /// </returns>
        Task<FinishNewKeyPairRequestResponseApiModel> FinishKeyPairRequestAsync(
            string requestId, CancellationToken ct = default);

        /// <summary>
        /// Approve the certificate request.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ApproveRequestAsync(string requestId, CancellationToken ct = default);

        /// <summary>
        /// Reject the certificate request.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RejectRequestAsync(string requestId, CancellationToken ct = default);

        /// <summary>
        /// Accept request and delete the private key.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <param name="ct"></param>
        Task AcceptRequestAsync(string requestId, CancellationToken ct = default);

        /// <summary>
        /// Purge request. Physically delete the request.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <param name="ct"></param>
        Task DeleteRequestAsync(string requestId, CancellationToken ct = default);

        /// <summary>
        /// Get a specific certificate request.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <param name="ct"></param>
        /// <returns>The certificate request</returns>
        Task<CertificateRequestRecordApiModel> GetRequestAsync(string requestId,
            CancellationToken ct = default);

        /// <summary>
        /// Query for certificate requests.
        /// </summary>
        /// <param name="query">optional, query filter</param>
        /// <param name="pageSize">optional, the maximum number
        /// of result per page</param>
        /// <param name="ct"></param>
        /// <returns>Matching requests, next page link</returns>
        Task<CertificateRequestQueryResponseApiModel> QueryRequestsAsync(
            CertificateRequestQueryRequestApiModel query, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// List requests
        /// </summary>
        /// <param name="nextPageLink">optional, link to next page </param>
        /// <param name="pageSize">optional, the maximum number
        /// of result per page</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CertificateRequestQueryResponseApiModel> ListRequestsAsync(
            string nextPageLink = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Add a trust relationship for a particular entity
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trustedEntityId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddTrustRelationshipAsync(string entityId, string trustedEntityId,
            CancellationToken ct = default);

        /// <summary>
        /// List trusted certificates for an entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateListApiModel> ListTrustedCertificatesAsync(
            string entityId, string nextPageLink = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Remove trust relationship for a specified entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="untrustedEntityId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveTrustRelationshipAsync(string entityId, string untrustedEntityId,
            CancellationToken ct = default);
    }
}
