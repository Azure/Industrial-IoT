// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Serilog;

    /// <summary>
    /// Job orchestrator the represents the legacy publishednodes.json with legacy command line arguments as job.
    /// </summary>
    public class LegacyJobRepository : IJobRepository {
        /// <summary>
        /// Creates a new class of the LegacyJobOrchestrator.
        /// </summary>
        /// <param name="publishedNodesJobConverter">The converter to read the job from the specified file.</param>
        /// <param name="legacyCliModelProvider">The provider that provides the legacy command line arguments.</param>
        /// <param name="jobSerializer">The serializer to (de)serialize job information.</param>
        /// <param name="logger">Logger to write log messages.</param>
        public LegacyJobRepository(PublishedNodesJobConverter publishedNodesJobConverter,
            ILegacyCliModelProvider legacyCliModelProvider, IJobSerializer jobSerializer, ILogger logger) {
            _publishedNodesJobConverter = publishedNodesJobConverter;
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel;
            _jobSerializer = jobSerializer;
            _logger = logger;

            var directory = Path.GetDirectoryName(_legacyCliModel.PublishedNodesFile);

            if (string.IsNullOrWhiteSpace(directory)) {
                directory = Environment.CurrentDirectory;
            }

            var file = Path.GetFileName(_legacyCliModel.PublishedNodesFile);

            _fileSystemWatcher = new FileSystemWatcher(directory, file);
            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
            RefreshJobFromFile();
        }

        /// <inheritdoc/>
        public Task<JobInfoListModel> QueryAsync(JobInfoQueryModel query = null, string continuationToken = null,
            int? maxResults = null, CancellationToken ct = default) {
            var jobsQuery = _jobs.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.JobConfigurationType)) {
                jobsQuery = jobsQuery.Where(j => j.JobConfigurationType == query.JobConfigurationType);
            }

            if (!string.IsNullOrWhiteSpace(query.Name)) {
                jobsQuery = jobsQuery.Where(j => j.Name == query.Name);
            }

            if (query.Status.HasValue) {
                jobsQuery = jobsQuery.Where(j => j.LifetimeData.Status == query.Status.Value);
            }

            var result = new JobInfoListModel() {Jobs = jobsQuery.ToList(), ContinuationToken = continuationToken};

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<JobInfoModel> GetAsync(string jobId, CancellationToken ct = default) {
            if (_jobs.TryGetValue(jobId, out var jobInfoModel)) {
                return Task.FromResult(jobInfoModel);
            }
            else {
                return Task.FromResult<JobInfoModel>(null);
            }
        }

        /// <inheritdoc/>
        public Task<JobInfoModel> AddAsync(JobInfoModel job, CancellationToken ct = default) {
            if (!_jobs.TryAdd(job.Id, job)) {
                throw new Exception("Could not add job");
            }

            return Task.FromResult(job);
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddOrUpdateAsync(string jobId, Func<JobInfoModel, Task<JobInfoModel>> predicate,
            CancellationToken ct = default) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }

            _jobs.TryGetValue(jobId, out var updateOrAdd);

            var job = await predicate(updateOrAdd);

            if (job == null) {
                return updateOrAdd;
            }

            job.LifetimeData.Updated = DateTime.UtcNow;
            _jobs[jobId] = job;
            return job;
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> UpdateAsync(string jobId, Func<JobInfoModel, Task<bool>> predicate,
            CancellationToken ct = default) {
            if (!_jobs.ContainsKey(jobId)) {
                throw new ResourceNotFoundException("Job not found");
            }

            var job = _jobs[jobId];

            if (!await predicate(job)) {
                return job;
            }

            job.LifetimeData.Updated = DateTime.UtcNow;
            _jobs[jobId] = job;
            return job;
        }

        /// <inheritdoc/>
        public Task<JobInfoModel> DeleteAsync(string jobId, Func<JobInfoModel, Task<bool>> predicate,
            CancellationToken ct = default) {
            if (_jobs.ContainsKey(jobId)) {
                throw new ResourceNotFoundException("Job not found");
            }

            if (_jobs.TryRemove(jobId, out var deleted)) {
                return Task.FromResult(deleted);
            }
            else {
                throw new Exception("Could not remove job.");
            }
        }

        private void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
            RefreshJobFromFile();
        }

        private static string GetChecksum(string file) {
            using (var stream = File.OpenRead(file)) {
                var sha = new SHA256Managed();
                var checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }

        private void RefreshJobFromFile() {
            var retryCount = 3;

            while (true) {
                try {
                    var currentFileHash = GetChecksum(_legacyCliModel.PublishedNodesFile);

                    if (currentFileHash != _lastKnownFileHash) {
                        _logger.Information("File {publishedNodesFile} has changed, reloading...",
                            _legacyCliModel.PublishedNodesFile);
                        _lastKnownFileHash = currentFileHash;

                        using (var reader = new StreamReader(_legacyCliModel.PublishedNodesFile)) {
                            var jobs = _publishedNodesJobConverter.Read(reader, _legacyCliModel);
                            _jobs.Clear();
                            var now = DateTime.UtcNow;

                            foreach (var job in jobs) {
                                var serializedJob =
                                    _jobSerializer.SerializeJobConfiguration(job, out var jobConfigurationType);
                                var jobHash = job.GetHashSafe().ToString();

                                var jobInfoModel = new JobInfoModel {
                                    Demands = new List<DemandModel>(),
                                    Id = jobHash,
                                    JobConfiguration = serializedJob,
                                    JobConfigurationType = jobConfigurationType,
                                    LifetimeData = new JobLifetimeDataModel() {Updated = now},
                                    Name = $"LegacyJob_{jobHash}",
                                    RedundancyConfig = new RedundancyConfigModel {
                                        DesiredActiveAgents = 1, DesiredPassiveAgents = 0
                                    }
                                };

                                _jobs[jobHash] = jobInfoModel;
                            }
                        }
                    }

                    break;
                }
                catch (IOException ex) {
                    retryCount--;

                    if (retryCount > 0) {
                        _logger.Error("Error while loading job from file, retrying...");
                    }
                    else {
                        _logger.Error(ex, "Error while loading job from file. Retry expired, giving up.");
                        break;
                    }
                }
            }
        }

        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly IJobSerializer _jobSerializer;
        private readonly LegacyCliModel _legacyCliModel;
        private readonly ILogger _logger;

        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;

        private readonly ConcurrentDictionary<string, JobInfoModel> _jobs =
            new ConcurrentDictionary<string, JobInfoModel>();

        private string _lastKnownFileHash;
    }
}