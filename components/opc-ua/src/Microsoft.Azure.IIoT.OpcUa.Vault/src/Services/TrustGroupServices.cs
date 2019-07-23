// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Provides management services and certificate renewal for trust groups,
    /// aka. certificate groups
    /// </summary>
    public sealed class TrustGroupServices : ITrustGroupStore, ITrustGroupServices {

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="issuer"></param>
        /// <param name="logger"></param>
        public TrustGroupServices(IGroupRepository groups, ICertificateIssuer issuer,
            ILogger logger) {
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task RenewCertificateAsync(string groupId, CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            var group = await _groups.FindAsync(groupId, ct);
            if (group == null) {
                throw new ResourceNotFoundException("Group not found");
            }

            TrustGroupRegistrationModel parent = null;
            if (!string.IsNullOrEmpty(group.Group.ParentId)) {
                parent = await _groups.FindAsync(group.Group.ParentId, ct);
                if (parent == null) {
                    throw new ResourceNotFoundException("Parent not found");
                }
            }
            var certificate = await RenewGroupCertificateAsync(group,
                parent, ct);
            _logger.Information("Group {groupId} certificate renewed.", groupId);
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationResultModel> CreateGroupAsync(
            TrustGroupRegistrationRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ParentId)) {
                throw new ArgumentNullException(nameof(request.ParentId));
            }

            var parent = await _groups.FindAsync(request.ParentId, ct);
            if (parent == null) {
                throw new ResourceNotFoundException("Parent group not found");
            }

            System.Diagnostics.Debug.Assert(parent.Id.EqualsIgnoreCase(
                request.ParentId));

            var result = await _groups.AddAsync(request.ToRegistration(parent.Group), ct);
            try {
                // Issue new certificate from parent
                var certificate = await RenewGroupCertificateAsync(result,
                    parent, ct);
                _logger.Information("Group {name} {groupId} created.",
                    request.Name, result.Id);
                return new TrustGroupRegistrationResultModel {
                    Id = result.Id
                };
            }
            catch {
                // Attempt to remove group
                await Try.Async(() => _groups.DeleteAsync(result.Id, r => true));
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationResultModel> CreateRootAsync(
            TrustGroupRootCreateRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _groups.AddAsync(request.ToRegistration(), ct);
            try {
                // Issues new root certificate
                var certificate = await RenewGroupCertificateAsync(result, null, ct);
                _logger.Information("Root {name} {groupId} created.",
                    request.Name, result.Id);
                return new TrustGroupRegistrationResultModel {
                    Id = result.Id
                };
            }
            catch {
                // Attempt to remove group
                await Try.Async(() => _groups.DeleteAsync(result.Id, r => true));
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationModel> GetGroupAsync(
            string groupId, CancellationToken ct) {
            var group = await _groups.FindAsync(groupId, ct);
            if (group == null) {
                throw new ResourceNotFoundException("No such group");
            }
            return group;
        }

        /// <inheritdoc/>
        public async Task UpdateGroupAsync(string groupId,
            TrustGroupRegistrationUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            await _groups.UpdateAsync(groupId, group => {
                group.Group.Patch(request);
                return true;
            }, ct);
            _logger.Information("Group {groupId} updated.", groupId);
        }

        /// <inheritdoc/>
        public Task<TrustGroupRegistrationListModel> QueryGroupsAsync(
            TrustGroupRegistrationQueryModel filter, int? pageSize,
            CancellationToken ct) {
            return _groups.QueryAsync(filter, null, pageSize, ct);
        }

        /// <inheritdoc/>
        public Task<TrustGroupRegistrationListModel> ListGroupsAsync(
            string nextPageLink, int? pageSize, CancellationToken ct) {
            return _groups.QueryAsync(null, nextPageLink, pageSize, ct);
        }

        /// <inheritdoc/>
        public async Task DeleteGroupAsync(string groupId, CancellationToken ct) {
            await _groups.DeleteAsync(groupId, r => true, ct);
        }

        /// <summary>
        /// Renew group certificate
        /// </summary>
        /// <param name="group"></param>
        /// <param name="parent"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<Certificate> RenewGroupCertificateAsync(
            TrustGroupRegistrationModel group, TrustGroupRegistrationModel parent,
            CancellationToken ct) {
            var now = DateTime.UtcNow.AddDays(-1);
            var notBefore = new DateTime(now.Year, now.Month, now.Day,
                0, 0, 0, DateTimeKind.Utc);
            if (parent == null) {
                return await RenewGroupRootAsync(group, notBefore, ct);
            }
            return await RenewGroupFromParentAsync(group, parent, notBefore, ct);
        }

        /// <summary>
        /// Renew group through parent group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="parent"></param>
        /// <param name="notBefore"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<Certificate> RenewGroupFromParentAsync(
            TrustGroupRegistrationModel group, TrustGroupRegistrationModel parent,
            DateTime notBefore, CancellationToken ct) {
            return await _issuer.NewIssuerCertificateAsync(
                parent.Id, group.Id,
                new X500DistinguishedName(group.Group.SubjectName),
                notBefore,
                new CreateKeyParams {
                    KeySize = group.Group.KeySize,
                    Type = KeyType.RSA
                },
                new IssuerPolicies {
                    IssuedLifetime = group.Group.IssuedLifetime,
                    SignatureType = group.Group.IssuedSignatureAlgorithm
                .ToSignatureType()
                },
                null, ct);
        }

        /// <summary>
        /// Renew group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="notBefore"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<Certificate> RenewGroupRootAsync(TrustGroupRegistrationModel group,
            DateTime notBefore, CancellationToken ct) {
            // create new CA cert
            return await _issuer.NewRootCertificateAsync(
                group.Id,
                new X500DistinguishedName(group.Group.SubjectName),
                notBefore, group.Group.Lifetime,
                new CreateKeyParams {
                    KeySize = group.Group.KeySize,
                    Type = KeyType.RSA
                },
                new IssuerPolicies {
                    IssuedLifetime = group.Group.IssuedLifetime,
                    SignatureType = group.Group.IssuedSignatureAlgorithm
                        .ToSignatureType()
                },
                null, ct);
        }

        private readonly ILogger _logger;
        private readonly IGroupRepository _groups;
        private readonly ICertificateIssuer _issuer;
    }
}
