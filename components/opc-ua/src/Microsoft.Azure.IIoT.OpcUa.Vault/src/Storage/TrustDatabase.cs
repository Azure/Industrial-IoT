// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Storage;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// Trust relationship database
    /// </summary>
    public sealed class TrustDatabase : ITrustRepository {

        /// <summary>
        /// Create relationship database
        /// </summary>
        /// <param name="db"></param>
        public TrustDatabase(IItemContainerFactory db) {
            if (db == null) {
                throw new ArgumentNullException(nameof(db));
            }
            var container = db.OpenAsync("trust").Result;
            _relationships = container.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<TrustRelationshipModel> AddAsync(TrustRelationshipModel relationship,
            CancellationToken ct) {
            if (relationship == null) {
                throw new ArgumentNullException(nameof(relationship));
            }
            var result = await _relationships.AddAsync(relationship.ToDocumentModel(),
                ct);

            return result.Value.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string subjectId, TrustDirectionType? direction,
            string objectId, CancellationToken ct) {
            if (string.IsNullOrEmpty(subjectId)) {
                throw new ArgumentNullException(nameof(subjectId));
            }
            var client = _relationships.OpenSqlClient();
            try {
                await client.DropAsync(CreateQuery(subjectId, false,
                    direction, objectId, out var queryParameters), queryParameters, null,
                    ct);
            }
            catch {
                var query = client.Query<string>(CreateQuery(subjectId, true,
                    direction, objectId, out var queryParameters), queryParameters, null);
                await query.ForEachAsync(
                    document => _relationships.DeleteAsync(document, ct), ct);
            }
        }

        /// <inheritdoc/>
        public async Task<TrustRelationshipListModel> ListAsync(
            string entityId, TrustDirectionType? direction, string nextPageLink,
            int? pageSize, CancellationToken ct) {

            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            var client = _relationships.OpenSqlClient();
            var query = nextPageLink != null ?
                client.Continue<TrustDocument>(nextPageLink, pageSize) :
                client.Query<TrustDocument>(CreateQuery(entityId, false, direction,
                    null, out var queryParameters), queryParameters, pageSize);

            // Read results
            var results = await query.ReadAsync(ct);
            return new TrustRelationshipListModel {
                Relationships = results.Select(r => r.Value.ToServiceModel()).ToList(),
                NextPageLink = query.ContinuationToken
            };
        }

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="idOnly"></param>
        /// <param name="direction"></param>
        /// <param name="objectId"></param>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        private static string CreateQuery(string entityId, bool idOnly, TrustDirectionType? direction,
            string objectId, out Dictionary<string, object> queryParameters) {

            queryParameters = new Dictionary<string, object>();
            var queryString = $"SELECT {(idOnly ? "g.Id" : "*")} FROM Trust g WHERE ";

            var trusted = direction == null ||
                 TrustDirectionType.Trusted == (direction.Value & TrustDirectionType.Trusted);
            var trusts = direction == null ||
                TrustDirectionType.Trusting == (direction.Value & TrustDirectionType.Trusting);

            if (trusted || trusts) {
                queryString += "(";
            }
            if (trusted) {
                queryString += $"(g.{nameof(TrustDocument.TrustedId)} = @trustedS";
                queryParameters.Add("@trustedS", entityId);
                if (objectId != null) {
                    queryString += $" AND g.{nameof(TrustDocument.TrustingId)} = @trustingO";
                    queryParameters.Add("@trustingO", objectId);
                }
                queryString += ")";
            }
            if (trusted && trusts) {
                queryString += " OR ";
            }
            if (trusts) {
                queryString += $"(g.{nameof(TrustDocument.TrustingId)} = @trustingS";
                queryParameters.Add("@trustingS", entityId);
                if (objectId != null) {
                    queryString += $" AND g.{nameof(TrustDocument.TrustedId)} = @trustedO";
                    queryParameters.Add("@trustedO", objectId);
                }
                queryString += ")";
            }
            if (trusted || trusts) {
                queryString += ") AND ";
            }
            queryString += $"g.{nameof(TrustDocument.ClassType)} = '{TrustDocument.ClassTypeName}'";
            return queryString;
        }

        private readonly IDocuments _relationships;
    }
}
