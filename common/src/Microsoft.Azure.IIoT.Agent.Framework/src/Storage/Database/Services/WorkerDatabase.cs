﻿// ------------------------------------------------------------
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
        public WorkerDatabase(IDatabaseServer databaseServer, IWorkerDatabaseConfig databaseRegistryConfig, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var database = databaseServer.OpenAsync(databaseRegistryConfig.DatabaseName).Result;
            var container = database.OpenContainerAsync(databaseRegistryConfig.ContainerName).Result;
            _documents = container.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task AddOrUpdate(WorkerHeartbeatModel workerHeartbeat,
            CancellationToken ct) {
            if (workerHeartbeat == null) {
                throw new ArgumentNullException(nameof(workerHeartbeat));
            }
            var retries = 0;
            var exceptions = new List<Exception>();
            while (retries < MaxRetries) {
                retries++;
                try {
                    ct.ThrowIfCancellationRequested();

                    var workerDocument = new WorkerDocument {
                        AgentId = workerHeartbeat.AgentId,
                        Id = workerHeartbeat.WorkerId,
                        WorkerStatus = workerHeartbeat.Status,
                        LastSeen = DateTime.UtcNow
                    };

                    var existing = await _documents.FindAsync<WorkerDocument>(workerHeartbeat.WorkerId, ct);
                    if (existing != null) {
                        try {
                            workerDocument.ETag = existing.Etag;
                            workerDocument.Id = existing.Id;
                            await _documents.ReplaceAsync(existing, workerDocument, ct);
                            return;
                        }
                        catch (ResourceOutOfDateException ex) {
                            _logger.Warning(ex, "Failed to update document for worker {workerId} - retrying", workerHeartbeat.WorkerId);
                            exceptions.Add(ex);
                            continue; // try again refreshing the etag
                        }
                        catch (ResourceNotFoundException ex) {
                            _logger.Warning(ex, "Failed to update document for worker {workerId} - retrying", workerHeartbeat.WorkerId);
                            exceptions.Add(ex);
                            continue;
                        }
                    }
                    try {
                        await _documents.AddAsync(workerDocument, ct);
                        return;
                    }
                    catch (ConflictingResourceException ex) {
                        // Try to update
                        _logger.Warning(ex, "Failed to add document for worker {workerId} - retrying", workerHeartbeat.WorkerId);
                        exceptions.Add(ex);
                        continue;
                    }
                }
                catch (OperationCanceledException) {
                    _logger.Warning("Failed to add document for worker {workerId} because of cancelation", workerHeartbeat.WorkerId);
                    throw;
                }
            }
            _logger.Warning("Failed to add or update document for worker {workerId} because of too many retries", workerHeartbeat.WorkerId);
            throw new AggregateException(exceptions);
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoListModel> ListWorkersAsync(string continuationToken,
            int? maxResults, CancellationToken ct) {
            var client = _documents.OpenSqlClient();
            var queryName = CreateQuery(out var queryParameters);
            var results = continuationToken != null ?
                client.Continue<WorkerDocument>(queryName, continuationToken, queryParameters, maxResults) :
                client.Query<WorkerDocument>(queryName, queryParameters, maxResults);
            if (!results.HasMore()) {
                return new WorkerInfoListModel();
            }
            var documents = await results.ReadAsync(ct);
            return new WorkerInfoListModel {
                ContinuationToken = results.ContinuationToken,
                Workers = documents.Select(r => r.Value.ToFrameworkModel()).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoModel> GetWorkerAsync(string workerId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            var document = await _documents.FindAsync<WorkerDocument>(workerId, ct);
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
            await _documents.DeleteAsync(workerId, ct);
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

        private readonly ILogger _logger;
        private readonly IDocuments _documents;
        private const int MaxRetries = 10;
    }
}
