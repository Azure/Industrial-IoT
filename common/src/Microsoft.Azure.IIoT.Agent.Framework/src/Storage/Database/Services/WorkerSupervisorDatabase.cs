// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Database based worker registry
    /// </summary>
    public class WorkerSupervisorDatabase : IWorkerSupervisorRegistry, IAgentRepository {

        /// <summary>
        /// Create worker registry
        /// </summary>
        /// <param name="databaseServer"></param>
        /// <param name="databaseRegistryConfig"></param>
        public WorkerSupervisorDatabase(IDatabaseServer databaseServer, IWorkerDatabaseConfig databaseRegistryConfig) {
            var database = databaseServer.OpenAsync(databaseRegistryConfig.DatabaseName).Result;
            var container = database.OpenContainerAsync(databaseRegistryConfig.ContainerName).Result;
            _documents = container.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task AddOrUpdate(SupervisorHeartbeatModel supervisorHeartbeat,
            CancellationToken ct) {
            if (supervisorHeartbeat == null) {
                throw new ArgumentNullException(nameof(supervisorHeartbeat));
            }
            while (true) {
                var workerDocument = new WorkerSupervisorDocument {
                    Id = supervisorHeartbeat.SupervisorId,
                    WorkerStatus = supervisorHeartbeat.Status,
                    LastSeen = DateTime.UtcNow
                };
                var existing = await _documents.FindAsync<WorkerSupervisorDocument>(
                    supervisorHeartbeat.SupervisorId);
                if (existing != null) {
                    try {
                        workerDocument.ETag = existing.Etag;
                        workerDocument.Id = existing.Id;
                        await _documents.ReplaceAsync(existing, workerDocument);
                        return;
                    }
                    catch (ResourceNotFoundException) {
                        continue;
                    }
                }
                try {
                    await _documents.AddAsync(workerDocument);
                    return;
                }
                catch (ConflictingResourceException) {
                    // Try to update
                    continue;
                }
            }
        }
        /// <inheritdoc/>
        public async Task<WorkerSupervisorInfoListModel> ListWorkerSupervisorsAsync(string continuationToken,
            int? maxResults, CancellationToken ct) {

            var client = _documents.OpenSqlClient();
            var results = continuationToken != null ?
                client.Continue<WorkerSupervisorDocument>(continuationToken, maxResults) :
                client.Query<WorkerSupervisorDocument>(CreateQuery(out var queryParameters),
                    queryParameters, maxResults);
            if (!results.HasMore()) {
                return new WorkerSupervisorInfoListModel();
            }
            var documents = await results.ReadAsync(ct);
            return new WorkerSupervisorInfoListModel {
                ContinuationToken = results.ContinuationToken,
                Workers = documents.Select(r => r.Value.ToFrameworkModel()).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<WorkerSupervisorInfoModel> GetWorkerSupervisorAsync(string workerSupervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerSupervisorId)) {
                throw new ArgumentNullException(nameof(workerSupervisorId));
            }
            var document = await _documents.FindAsync<WorkerSupervisorDocument>(workerSupervisorId, ct);
            if (document == null) {
                throw new ResourceNotFoundException("Worker not found");
            }
            return document.Value.ToFrameworkModel();
        }

        /// <inheritdoc/>
        public async Task DeleteWorkerSupervisorAsync(string workerSupervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerSupervisorId)) {
                throw new ArgumentNullException(nameof(workerSupervisorId));
            }
            await _documents.DeleteAsync(workerSupervisorId, ct);
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
                $"r.{nameof(WorkerSupervisorDocument.ClassType)} = '{WorkerSupervisorDocument.ClassTypeName}'";
            return queryString;
        }

        private readonly IDocuments _documents;
    }
}