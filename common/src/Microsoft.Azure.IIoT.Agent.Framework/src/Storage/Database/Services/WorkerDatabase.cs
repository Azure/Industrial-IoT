// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Database based worker registry
    /// </summary>
    public class WorkerDatabase : IWorkerRegistry, IWorkerRepository {

        /// <summary>
        /// Create worker registry
        /// </summary>
        /// <param name="databaseServer"></param>
        /// <param name="databaseRegistryConfig"></param>
        /// <param name="logger"></param>
        public WorkerDatabase(IDatabaseServer databaseServer,
            IWorkerDatabaseConfig databaseRegistryConfig, ILogger logger) {
            _logger = logger;
            _databaseServer = databaseServer;
            _databaseRegistryConfig = databaseRegistryConfig;
        }

        /// <inheritdoc/>
        public async Task AddOrUpdate(WorkerHeartbeatModel workerHeartbeat,
            CancellationToken ct) {
            if (workerHeartbeat == null) {
                throw new ArgumentNullException(nameof(workerHeartbeat));
            }
            while (true) {
                var workerDocument = new WorkerDocument {
                    AgentId = workerHeartbeat.AgentId,
                    Id = workerHeartbeat.WorkerId,
                    WorkerStatus = workerHeartbeat.Status,
                    LastSeen = DateTime.UtcNow
                };
                var documents = await GetDocumentsAsync();
                var existing = await documents.FindAsync<WorkerDocument>(
                    workerHeartbeat.WorkerId);
                if (existing != null) {
                    try {
                        workerDocument.ETag = existing.Etag;
                        workerDocument.Id = existing.Id;
                        await documents.ReplaceAsync(existing, workerDocument);
                        return;
                    }
                    catch (ResourceOutOfDateException) {
                        continue; // try again refreshing the etag
                    }
                    catch (ResourceNotFoundException) {
                        continue;
                    }
                }
                try {
                    await documents.AddAsync(workerDocument);
                    return;
                }
                catch (ConflictingResourceException) {
                    // Try to update
                    continue;
                }
            }
        }
        /// <inheritdoc/>
        public async Task<WorkerInfoListModel> ListWorkersAsync(string continuationToken,
            int? maxResults, CancellationToken ct) {

            var documents = await GetDocumentsAsync();
            var client = documents.OpenSqlClient();
            var queryName = CreateQuery(out var queryParameters);
            var results = continuationToken != null ?
                client.Continue<WorkerDocument>(queryName, continuationToken, queryParameters, maxResults) :
                client.Query<WorkerDocument>(queryName, queryParameters, maxResults);
            if (!results.HasMore()) {
                return new WorkerInfoListModel();
            }
            var docs = await results.ReadAsync(ct);
            return new WorkerInfoListModel {
                ContinuationToken = results.ContinuationToken,
                Workers = docs.Select(r => r.Value.ToFrameworkModel()).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoModel> GetWorkerAsync(string workerId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            var documents = await GetDocumentsAsync();
            var document = await documents.FindAsync<WorkerDocument>(workerId, ct);
            if (document == null) {
                throw new ResourceNotFoundException("Worker not found");
            }
            return document.Value.ToFrameworkModel();
        }

        /// <inheritdoc/>
        public async Task DeleteWorkerAsync(string workerId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            var documents = await GetDocumentsAsync();
            await documents.DeleteAsync(workerId, ct);
        }

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        private static string CreateQuery(out Dictionary<string, object> queryParameters) {
            queryParameters = new Dictionary<string, object>();
            var queryString = $"SELECT * FROM r WHERE ";
            queryString +=
                $"r.{nameof(WorkerDocument.ClassType)} = '{WorkerDocument.ClassTypeName}'";
            return queryString;
        }

        private async Task<IDocuments> GetDocumentsAsync() {
            if (_documents == null) {
                try {
                    var database = await _databaseServer.OpenAsync(_databaseRegistryConfig.DatabaseName);
                    var container = await database.OpenContainerAsync(_databaseRegistryConfig.ContainerName);
                    _documents = container.AsDocuments();
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to open document collection.");
                    throw;
                }
            }
            return _documents;
        }

        private IDocuments _documents;
        private readonly ILogger _logger;
        private readonly IDatabaseServer _databaseServer;
        private readonly IWorkerDatabaseConfig _databaseRegistryConfig;
    }
}
