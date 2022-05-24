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
    /// Database job repository
    /// </summary>
    public class JobDatabase : IJobRepository {

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="databaseServer"></param>
        /// <param name="databaseJobRepositoryConfig"></param>
        public JobDatabase(IDatabaseServer databaseServer, IJobDatabaseConfig databaseJobRepositoryConfig) {
            var dbs = databaseServer.OpenAsync(databaseJobRepositoryConfig.DatabaseName).Result;
            var cont = dbs.OpenContainerAsync(databaseJobRepositoryConfig.ContainerName).Result;
            _documents = cont.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddAsync(JobInfoModel job, CancellationToken ct) {
            if (job == null) {
                throw new ArgumentNullException(nameof(job));
            }
            while (true) {
                var document = await _documents.FindAsync<JobDocument>(job.Id, ct);
                if (document != null) {
                    throw new ConflictingResourceException($"Job {job.Id} already exists.");
                }
                job.LifetimeData.Created = job.LifetimeData.Updated = DateTime.UtcNow;
                try {
                    var result = await _documents.AddAsync(job.ToDocumentModel(), ct);
                    return result.Value.ToFrameworkModel();
                }
                catch (ConflictingResourceException) {
                    // Try again
                    continue;
                }
                catch {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddOrUpdateAsync(string jobId,
            Func<JobInfoModel, Task<JobInfoModel>> predicate, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            while (true) {
                var document = await _documents.FindAsync<JobDocument>(jobId, ct);
                var updateOrAdd = document?.Value.ToFrameworkModel();
                var job = await predicate(updateOrAdd);
                if (job == null) {
                    return updateOrAdd;
                }
                job.LifetimeData.Updated = DateTime.UtcNow;
                var updated = job.ToDocumentModel(document?.Value?.ETag);
                if (document == null) {
                    try {
                        // Add document
                        var result = await _documents.AddAsync(updated, ct);
                        return result.Value.ToFrameworkModel();
                    }
                    catch (ConflictingResourceException) {
                        // Conflict - try update now
                        continue;
                    }
                }
                // Try replacing
                try {
                    var result = await _documents.ReplaceAsync(document, updated, ct);
                    return result.Value.ToFrameworkModel();
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> UpdateAsync(string jobId,
            Func<JobInfoModel, Task<bool>> predicate, CancellationToken ct) {

            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            while (true) {
                var document = await _documents.FindAsync<JobDocument>(jobId, ct);
                if (document == null) {
                    throw new ResourceNotFoundException("Job not found");
                }
                var job = document.Value.ToFrameworkModel();
                if (!await predicate(job)) {
                    return job;
                }
                job.LifetimeData.Updated = DateTime.UtcNow;
                var updated = job.ToDocumentModel(document.Value.ETag);
                try {
                    var result = await _documents.ReplaceAsync(document, updated, ct);
                    return result.Value.ToFrameworkModel();
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> GetAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var document = await _documents.FindAsync<JobDocument>(jobId, ct);
            if (document == null) {
                throw new ResourceNotFoundException("Job not found");
            }
            return document.Value.ToFrameworkModel();
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> QueryAsync(JobInfoQueryModel query,
            string continuationToken, int? maxResults, CancellationToken ct) {
            var client = _documents.OpenSqlClient();
            var queryName = CreateQuery(query, out var queryParameters);
            var results = continuationToken != null ?
                client.Continue<JobDocument>(queryName, continuationToken, queryParameters, maxResults) :
                client.Query<JobDocument>(queryName, queryParameters, maxResults);
            if (!results.HasMore()) {
                return new JobInfoListModel();
            }
            var documents = await results.ReadAsync(ct);
            return new JobInfoListModel {
                ContinuationToken = results.ContinuationToken,
                Jobs = documents.Select(r => r.Value.ToFrameworkModel()).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> DeleteAsync(string jobId,
            Func<JobInfoModel, Task<bool>> predicate, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            while (true) {
                var document = await _documents.FindAsync<JobDocument>(
                    jobId);
                if (document == null) {
                    return null;
                }
                var job = document.Value.ToFrameworkModel();
                if (!await predicate(job)) {
                    return job;
                }
                try {
                    await _documents.DeleteAsync(document, ct);
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
                return job;
            }
        }

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        private static string CreateQuery(JobInfoQueryModel query,
            out Dictionary<string, object> queryParameters) {
            queryParameters = new Dictionary<string, object>();
            var queryString = $"SELECT * FROM r WHERE ";
            if (query?.Status != null) {
                queryString +=
$"r.{nameof(JobDocument.Status)} = @state AND ";
                queryParameters.Add("@state", query.Status.Value);
            }
            if (query?.Name != null) {
                queryString +=
$"r.{nameof(JobDocument.Name)} = @name AND ";
                queryParameters.Add("@name", query.Name);
            }
            if (query?.JobConfigurationType != null) {
                queryString +=
$"r.{nameof(JobDocument.Type)} = @type AND ";
                queryParameters.Add("@type", query.JobConfigurationType);
            }
            queryString +=
$"r.{nameof(JobDocument.ClassType)} = '{JobDocument.ClassTypeName}'";
            return queryString;
        }

        private readonly IDocuments _documents;
    }
}
