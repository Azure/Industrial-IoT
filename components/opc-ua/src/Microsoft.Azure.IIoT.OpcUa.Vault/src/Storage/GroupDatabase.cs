// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// Trust group database
    /// </summary>
    public sealed class GroupDatabase : IGroupRepository {

        /// <summary>
        /// Create group database
        /// </summary>
        /// <param name="db"></param>
        public GroupDatabase(IItemContainerFactory db) {
            if (db == null) {
                throw new ArgumentNullException(nameof(db));
            }
            var container = db.OpenAsync("groups").Result;
            _groups = container.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationModel> AddAsync(
            TrustGroupRegistrationModel group, CancellationToken ct) {
            if (group == null) {
                throw new ArgumentNullException(nameof(group));
            }
            while (true) {
                group.Id = "grp" + Guid.NewGuid();
                try {
                    var result = await _groups.AddAsync(group.ToDocumentModel(), ct);
                    return result.Value.ToServiceModel();
                }
                catch (ConflictingResourceException) {
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationModel> FindAsync(
            string groupId, CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            var document = await _groups.FindAsync<GroupDocument>(groupId, ct);
            if (document == null) {
                throw new ResourceNotFoundException("No such group");
            }
            return document.Value.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationModel> UpdateAsync(string groupId,
            Func<TrustGroupRegistrationModel, bool> predicate,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            while (true) {
                var document = await _groups.FindAsync<GroupDocument>(
                    groupId, ct);
                if (document == null) {
                    throw new ResourceNotFoundException("Group does not exist");
                }
                var group = document.Value.Clone().ToServiceModel();
                if (!predicate(group)) {
                    return group;
                }
                try {
                    var result = await _groups.ReplaceAsync(document,
                        group.ToDocumentModel(), ct);
                    return result.Value.ToServiceModel();
                }
                catch (ResourceOutOfDateException) {
                    // Try again
                }
            }
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationModel> DeleteAsync(string groupId,
            Func<TrustGroupRegistrationModel, bool> predicate,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId),
                    "The application id must be provided");
            }
            while (true) {
                var document = await _groups.FindAsync<GroupDocument>(
                    groupId, ct);
                if (document == null) {
                    return null;
                }
                var group = document.Value.ToServiceModel();
                if (!predicate(group)) {
                    return group;
                }
                try {
                    // Try delete
                    await _groups.DeleteAsync(document, ct);
                    return group;
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationListModel> QueryAsync(
            TrustGroupRegistrationQueryModel filter, string nextPageLink, int? pageSize,
            CancellationToken ct) {
            var client = _groups.OpenSqlClient();
            var query = nextPageLink != null ?
                client.Continue<GroupDocument>(nextPageLink, pageSize) :
                client.Query<GroupDocument>(CreateQuery(
                    filter, out var queryParameters), queryParameters, pageSize);

            // Read results
            var results = await query.ReadAsync(ct);
            return new TrustGroupRegistrationListModel {
                Registrations = results.Select(r => r.Value.ToServiceModel()).ToList(),
                NextPageLink = query.ContinuationToken
            };
        }

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        private static string CreateQuery(TrustGroupRegistrationQueryModel filter,
            out Dictionary<string, object> queryParameters) {
            queryParameters = new Dictionary<string, object>();
            var queryString = "SELECT * FROM Policies g WHERE ";

            if (filter?.IssuedKeySize != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.IssuedKeySize)} = @iks AND ";
                queryParameters.Add("@iks", filter.IssuedKeySize.Value);
            }
            if (filter?.IssuedLifetime != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.IssuedLifetime)} = @ilt AND ";
                queryParameters.Add("@ilt", filter.IssuedLifetime.Value);
            }
            if (filter?.IssuedSignatureAlgorithm != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.IssuedSignatureAlgorithm)} = @isa AND ";
                queryParameters.Add("@isa", filter.IssuedSignatureAlgorithm.Value);
            }
            if (filter?.Type != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.Type)} = @Type AND ";
                queryParameters.Add("@Type", filter.Type.Value);
            }
            if (filter?.Name != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.Name)} = @Name AND ";
                queryParameters.Add("@Name", filter.Name);
            }
            if (filter?.ParentId != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.ParentId)} = @ParentId AND ";
                queryParameters.Add("@ParentId", filter.ParentId);
            }
            if (filter?.SubjectName != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.SubjectName)} = @sn AND ";
                queryParameters.Add("@sn", filter.ParentId);
            }
            if (filter?.Lifetime != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.Lifetime)} = @Lifetime AND ";
                queryParameters.Add("@Lifetime", filter.ParentId);
            }
            if (filter?.KeySize != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.KeySize)} = @KeySize AND ";
                queryParameters.Add("@KeySize", filter.ParentId);
            }
            if (filter?.SignatureAlgorithm != null) {
                queryString +=
                    $"g.{nameof(GroupDocument.SignatureAlgorithm)} = @sa AND ";
                queryParameters.Add("@sa", filter.SignatureAlgorithm);
            }
            queryString +=
        $"g.{nameof(GroupDocument.ClassType)} = '{GroupDocument.ClassTypeName}'";
            return queryString;
        }

        private readonly IDocuments _groups;
    }
}
