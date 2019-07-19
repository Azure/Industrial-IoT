// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Cosmos db certificate request database
    /// </summary>
    public sealed class RequestDatabase : IRequestRepository {

        /// <summary>
        /// Create certificate request
        /// </summary>
        /// <param name="db"></param>
        public RequestDatabase(IItemContainerFactory db) {
            if (db == null) {
                throw new ArgumentNullException(nameof(db));
            }

            var container = db.OpenAsync("requests").Result;
            _requests = container.AsDocuments();
            _index = new ContainerIndex(db, container.Name);
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestModel> AddAsync(
            CertificateRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var recordId = await _index.AllocateAsync();
            while (true) {
                request.Index = recordId;
                request.Record.State = CertificateRequestState.New;
                request.Record.RequestId = "req" + Guid.NewGuid();
                try {
                    var result = await _requests.AddAsync(request.ToDocument(), ct);
                    return result.Value.ToServiceModel();
                }
                catch (ConflictingResourceException) {
                    continue;
                }
                catch {
                    await Try.Async(() => _index.FreeAsync(recordId));
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestModel> UpdateAsync(string requestId,
            Func<CertificateRequestModel, bool> predicate,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            while (true) {
                var document = await _requests.FindAsync<RequestDocument>(
                    requestId);
                if (document == null) {
                    throw new ResourceNotFoundException("Request not found");
                }
                var request = document.Value.ToServiceModel();
                if (!predicate(request)) {
                    return request;
                }
                var updated = request.ToDocument(document.Value.ETag);
                try {
                    var result = await _requests.ReplaceAsync(document, updated, ct);
                    return result.Value.ToServiceModel();
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestModel> DeleteAsync(string requestId,
            Func<CertificateRequestModel, bool> predicate, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            while (true) {
                var document = await _requests.FindAsync<RequestDocument>(
                    requestId);
                if (document == null) {
                    return null;
                }
                var request = document.Value.ToServiceModel();
                if (!predicate(request)) {
                    return request;
                }
                try {
                    await _requests.DeleteAsync(document, ct);
                    await Try.Async(() => _index.FreeAsync(document.Value.Index));
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
                return request;
            }
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestModel> FindAsync(string requestId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var document = await _requests.FindAsync<RequestDocument>(
                requestId, ct);
            if (document == null) {
                throw new ResourceNotFoundException("Request not found");
            }
            return document.Value.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestListModel> QueryAsync(
            CertificateRequestQueryRequestModel query, string nextPageLink,
            int? maxResults, CancellationToken ct) {
            var client = _requests.OpenSqlClient();
            var results = nextPageLink != null ?
                client.Continue<RequestDocument>(nextPageLink, maxResults) :
                client.Query<RequestDocument>(
                    CreateQuery(query, out var queryParameters),
                    queryParameters, maxResults);
            if (!results.HasMore()) {
                return new CertificateRequestListModel();
            }
            var documents = await results.ReadAsync(ct);
            return new CertificateRequestListModel {
                NextPageLink = results.ContinuationToken,
                Requests = documents.Select(r => r.Value.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Create query string from parameters
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        private static string CreateQuery(CertificateRequestQueryRequestModel query,
            out Dictionary<string, object> queryParameters) {
            queryParameters = new Dictionary<string, object>();
            var queryString = "SELECT * FROM CertificateRequests r WHERE ";
            if (query?.State != null) {
                queryString +=
                    $"r.{nameof(RequestDocument.State)} = @state AND ";
                queryParameters.Add("@state", query.State.Value);
            }
            if (query?.EntityId != null) {
                queryString +=
$"r.{nameof(RequestDocument.Entity)}.{nameof(EntityInfoModel.Id)} = @entityId AND ";
                queryParameters.Add("@entityId", query.EntityId);
            }
            queryString +=
$"r.{nameof(RequestDocument.ClassType)} = '{RequestDocument.ClassTypeName}'";
            return queryString;
        }

        private readonly IDocuments _requests;
        private readonly IContainerIndex _index;
    }
}
