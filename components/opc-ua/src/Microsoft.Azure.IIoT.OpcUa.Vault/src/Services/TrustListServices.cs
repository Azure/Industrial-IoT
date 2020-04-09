// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Crypto;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Manage trust relationships
    /// </summary>
    public sealed class TrustListServices : ITrustListServices {

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="resolver"></param>
        /// <param name="certificates"></param>
        /// <param name="logger"></param>
        public TrustListServices(ITrustRepository repo, IEntityInfoResolver resolver,
            ICertificateStore certificates, ILogger logger) {
            _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        /// <inheritdoc/>
        public async Task AddTrustRelationshipAsync(string entityId, string trustedId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            if (string.IsNullOrEmpty(trustedId)) {
                throw new ArgumentNullException(nameof(trustedId));
            }

            var entity = await _resolver.FindEntityAsync(entityId, ct);
            if (entity == null) {
                throw new ResourceNotFoundException("Trusting entity not found");
            }
            var trusted = await _resolver.FindEntityAsync(trustedId, ct);
            if (trusted == null) {
                throw new ResourceNotFoundException("Trusted entity not found");
            }
            try {
                var relationship = await _repo.AddAsync(new TrustRelationshipModel {
                    TrustedId = trusted.Id,
                    TrustedType = trusted.Type,
                    TrustingId = entity.Id,
                    TrustingType = entity.Type
                }, ct);
                _logger.Information("{@Entity} now trusting {@Trusted}", entity, trusted);
            }
            catch (ConflictingResourceException) {
                _logger.Debug("{@Entity} already trusting {@Trusted}", entity, trusted);
            }
        }

        /// <inheritdoc/>
        public async Task<X509CertificateListModel> ListTrustedCertificatesAsync(
            string entityId, string nextPageLink, int? maxPageSize, CancellationToken ct) {
            // Get all
            var trusted = await _repo.ListAsync(entityId, TrustDirectionType.Trusting,
                nextPageLink, maxPageSize, ct);

            var result = new X509CertificateListModel {
                NextPageLink = trusted.NextPageLink,
                Certificates = new List<X509CertificateModel>()
            };

            // Foreach entity, resolve certificate chains
            foreach (var relationship in trusted.Relationships) {

                // Get latest certificate from store - it has the id of the entity
                var trustedCert = await _certificates.FindLatestCertificateAsync(
                    relationship.TrustedId, ct);
                if (trustedCert == null) {
                    continue;
                }
                result.Certificates.Add(trustedCert.ToServiceModel());
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task RemoveTrustRelationshipAsync(string entityId, string untrustedId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            if (string.IsNullOrEmpty(untrustedId)) {
                throw new ArgumentNullException(nameof(untrustedId));
            }
            await _repo.DeleteAsync(entityId, TrustDirectionType.Trusting, untrustedId, ct);
            _logger.Information("{entityId} trust to {untrustedId} removed.", entityId,
                untrustedId);
        }

        private readonly ITrustRepository _repo;
        private readonly IEntityInfoResolver _resolver;
        private readonly ILogger _logger;
        private readonly ICertificateStore _certificates;
    }
}
