// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for vault client to adapt to v1.
    /// </summary>
    public static class VaultServiceApiEx {

        /// <summary>
        /// List all groups
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<string>> ListAllGroupsAsync(this IVaultServiceApi service,
            CancellationToken ct = default) {
            var groups = new List<string>();
            var result = await service.ListGroupsAsync(null, null, ct);
            groups.AddRange(result.Groups);
            while (result.NextPageLink != null) {
                result = await service.ListGroupsAsync(result.NextPageLink,
                    null, ct);
                groups.AddRange(result.Groups);
            }
            return groups;
        }

        /// <summary>
        /// Query all requests
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<CertificateRequestRecordApiModel>> QueryAllRequestsAsync(
            this IVaultServiceApi service, CertificateRequestQueryRequestApiModel query,
            CancellationToken ct = default) {
            var requests = new List<CertificateRequestRecordApiModel>();
            var result = await service.QueryRequestsAsync(query, null, ct);
            requests.AddRange(result.Requests);
            while (result.NextPageLink != null) {
                result = await service.ListRequestsAsync(result.NextPageLink,
                    null, ct);
                requests.AddRange(result.Requests);
            }
            return requests;
        }

        /// <summary>
        /// List all requests
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<CertificateRequestRecordApiModel>> ListAllRequestsAsync(
            this IVaultServiceApi service, CancellationToken ct = default) {
            var requests = new List<CertificateRequestRecordApiModel>();
            var result = await service.ListRequestsAsync(null, null, ct);
            requests.AddRange(result.Requests);
            while (result.NextPageLink != null) {
                result = await service.ListRequestsAsync(result.NextPageLink,
                    null, ct);
                requests.AddRange(result.Requests);
            }
            return requests;
        }

        /// <summary>
        /// List all trusted certificates
        /// </summary>
        /// <param name="service"></param>
        /// <param name="entityId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<X509CertificateApiModel>> ListAllTrustedCertificatesAsync(
            this IVaultServiceApi service, string entityId, CancellationToken ct = default) {
            var certificates = new List<X509CertificateApiModel>();
            var result = await service.ListTrustedCertificatesAsync(entityId, null, null, ct);
            certificates.AddRange(result.Certificates);
            while (result.NextPageLink != null) {
                result = await service.ListTrustedCertificatesAsync(entityId, result.NextPageLink,
                    null, ct);
                certificates.AddRange(result.Certificates);
            }
            return certificates;
        }
    }
}
