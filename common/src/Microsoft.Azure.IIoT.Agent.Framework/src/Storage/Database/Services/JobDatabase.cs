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
    /// Database job repository
    /// </summary>
    public class JobDatabase : IJobRepository {

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="databaseServer"></param>
        /// <param name="databaseRegistryConfig"></param>
        /// <param name="logger"></param>
        public JobDatabase(IDatabaseServer databaseServer,
            IJobDatabaseConfig databaseRegistryConfig, ILogger logger) {
            _logger = logger;
            _databaseServer = databaseServer;
            _databaseRegistryConfig = databaseRegistryConfig;
            _documents = GetDocumentsAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddAsync(JobInfoModel job, CancellationToken ct) {
            if (job == null) {
                throw new ArgumentNullException(nameof(job));
            }
            var retries = 0;
            var exceptions = new List<Exception>();
            while (retries < MaxRetries) {
                retries++;
                ct.ThrowIfCancellationRequested();
                var document = await _documents.FindAsync<JobDocument>(job.Id, ct);
                if (document != null) {
                    _logger.Warning("Failed to add document for job {jobId} - already exist", job.Id);
                    throw new ConflictingResourceException($"Job {job.Id} already exists.");
                }
                job.LifetimeData.Created = job.LifetimeData.Updated = DateTime.UtcNow;
                try {
                    var result = await _documents.AddAsync(job.ToDocumentModel(), ct);
                    return result.Value.ToFrameworkModel();
                }
                catch (ConflictingResourceException ex) {
                    // Try again
                    exceptions.Add(ex);
                    continue;
                }
                catch {
                    throw;
                }
            }
            var aggregateException = new AggregateException(exceptions);
            _logger.Warning(aggregateException,
                "Failed to add document for job {jobId} because of too many retries", job.Id);
            throw aggregateException;
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddOrUpdateAsync(string jobId,
            Func<JobInfoModel, CancellationToken, Task<JobInfoModel>> predicate, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var retries = 0;
            var exceptions = new List<Exception>();
            while (retries < MaxRetries) {
                retries++;
                ct.ThrowIfCancellationRequested();
                var document = await _documents.FindAsync<JobDocument>(jobId, ct);
                var updateOrAdd = document?.Value.ToFrameworkModel();
                var job = await predicate(updateOrAdd, ct);
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
                    catch (ConflictingResourceException ex) {
                        // Conflict - try update now
                        exceptions.Add(ex);
                        continue;
                    }
                }
                // Try replacing
                try {
                    var result = await _documents.ReplaceAsync(document, updated, ct);
                    return result.Value.ToFrameworkModel();
                }
                catch (ResourceOutOfDateException ex) {
                    exceptions.Add(ex);
                    continue;
                }
            }
            var aggregateException = new AggregateException(exceptions);
            _logger.Warning(aggregateException,
                "Failed to add or update document for job {jobId} because of too many retries", jobId);
            throw aggregateException;
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> UpdateAsync(string jobId,
            Func<JobInfoModel,CancellationToken, Task<bool>> predicate, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var retries = 0;
            var exceptions = new List<Exception>();
            while (retries < MaxRetries) {
                retries++;
                ct.ThrowIfCancellationRequested();
                var document = await _documents.FindAsync<JobDocument>(jobId, ct);
                if (document == null) {
                    throw new ResourceNotFoundException("Job not found");
                }
                var job = document.Value.ToFrameworkModel();
                if (!await predicate(job, ct)) {
                    return job;
                }
                job.LifetimeData.Updated = DateTime.UtcNow;
                var updated = job.ToDocumentModel(document.Value.ETag);
                try {
                    var result = await _documents.ReplaceAsync(document, updated, ct);
                    return result.Value.ToFrameworkModel();
                }
                catch (ResourceOutOfDateException ex) {
                    exceptions.Add(ex);
                    continue;
                }
            }
            var aggregateException = new AggregateException(exceptions);
            _logger.Warning(aggregateException,
                "Failed to update document for job {jobId} because of too many retries", jobId);
            throw aggregateException;
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> GetAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var document = await _documents.FindAsync<JobDocument>(jobId, ct);
            if (document == null) {
                _logger.Warning("Failed to find document for job {jobId}", jobId);
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
            Func<JobInfoModel, CancellationToken,Task<bool>> predicate, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var retries = 0;
            var exceptions = new List<Exception>();
            while (retries < MaxRetries) {
                retries++;
                ct.ThrowIfCancellationRequested();
                var document = await _documents.FindAsync<JobDocument>(jobId);
                if (document == null) {
                    return null;
                }
                var job = document.Value.ToFrameworkModel();
                if (!await predicate(job, ct)) {
                    return job;
                }
                try {
                    await _documents.DeleteAsync(document, ct);
                }
                catch (ResourceOutOfDateException ex) {
                    exceptions.Add(ex);
                    continue;
                }
                return job;
            }
            var aggregateException = new AggregateException(exceptions);
            _logger.Warning(aggregateException,
                "Failed to delete document for job {jobId} because of too many retries", jobId);
            throw aggregateException;
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

        private async Task<IDocuments> GetDocumentsAsync() {
            try {
                var database = await _databaseServer.OpenAsync(_databaseRegistryConfig.DatabaseName);
                var container = await database.OpenContainerAsync(_databaseRegistryConfig.ContainerName);
                return container.AsDocuments();
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to open document collection.");
                throw;
            }
        }

        private readonly IDocuments _documents;
        private readonly ILogger _logger;
        private readonly IDatabaseServer _databaseServer;
        private readonly IJobDatabaseConfig _databaseRegistryConfig;
        private const int MaxRetries = 10;
    }
}
