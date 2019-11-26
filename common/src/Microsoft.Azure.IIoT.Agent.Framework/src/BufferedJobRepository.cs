// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File system based job repository
    /// </summary>
    public abstract class BufferedJobRepository : IJobRepository, IDisposable {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<JobInfoModel> ReadJobs();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract Task WriteJobs();

        /// <summary>
        /// Create repo
        /// </summary>
        public BufferedJobRepository(int updateBuffer) {
            _jobs = new List<JobInfoModel>(ReadJobs());
            _updateBuffer = updateBuffer;
        }

        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> QueryAsync(JobInfoQueryModel query,
            string continuationToken, int? maxResults, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                var jobs = _jobs.Select(j => j.Clone()).ToList();
                
                if (query != null) {
                    if (query.Status.HasValue) {
                        jobs = jobs.Where(j => j.LifetimeData.Status == query.Status.Value).ToList();
                    }
                    if (!string.IsNullOrWhiteSpace(query.JobConfigurationType)) {
                        jobs = jobs.Where(j => j.JobConfigurationType == query.JobConfigurationType).ToList();
                    }
                }

                return new JobInfoListModel {
                    Jobs = jobs
                };
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> GetAsync(string jobId, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                return _jobs.SingleOrDefault(j => j.Id == jobId)?.Clone();
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddAsync(JobInfoModel job, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                if (_jobs.Any(j => j.Id == job.Id)) {
                    throw new ConflictingResourceException($"{job.Id} already exists.");
                }
                job.LifetimeData.Created = job.LifetimeData.Updated = DateTime.UtcNow;
                _jobs.Add(job);
                await BufferedUpdate();
                return job;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> AddOrUpdateAsync(string jobId,
            Func<JobInfoModel, Task<JobInfoModel>> predicate, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                var jobToUpdate = _jobs.SingleOrDefault(j => j.Id == jobId);
                var updatedOrNew = await predicate(jobToUpdate.Clone());
                if (updatedOrNew == null) {
                    return jobToUpdate;
                }
                updatedOrNew.LifetimeData.Updated = DateTime.UtcNow;
                if (jobToUpdate != null) {
                    // Remove old job to update
                    _jobs.Remove(jobToUpdate);
                }
                else {
                    updatedOrNew.LifetimeData.Created = updatedOrNew.LifetimeData.Updated;
                }
                _jobs.Add(updatedOrNew);
                await BufferedUpdate();
                return updatedOrNew;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> UpdateAsync(string jobId,
            Func<JobInfoModel, Task<bool>> predicate, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                var jobToUpdate = _jobs.SingleOrDefault(j => j.Id == jobId);
                var updated = jobToUpdate.Clone();
                if (await predicate(updated)) {
                    _jobs.Remove(jobToUpdate);
                    updated.LifetimeData.Updated = DateTime.UtcNow;
                    _jobs.Add(updated);
                    await BufferedUpdate();
                }
                return updated;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> DeleteAsync(string jobId,
            Func<JobInfoModel, Task<bool>> predicate, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                var jobToRemove = _jobs.SingleOrDefault(j => j.Id == jobId);
                if (jobToRemove == null) {
                    throw new ResourceNotFoundException("Job not found");
                }
                if (await predicate(jobToRemove)) {
                    _jobs.Remove(jobToRemove);
                    await BufferedUpdate();
                }
                return jobToRemove.Clone();
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Buffering
        /// </summary>
        /// <returns></returns>
        private Task BufferedUpdate() {
            if (_updateCounter < _updateBuffer) {
                _updateCounter++;
                return Task.CompletedTask;
            }

            _updateCounter = 0;
            WriteJobs();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }
            if (disposing) {
                WriteJobs().Wait();
                _lock.Dispose();
            }
            _disposed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected List<JobInfoModel> _jobs;

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly int _updateBuffer;
        private bool _disposed;
        private int _updateCounter;
    }
}