// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Default job manager
    /// </summary>
    public class DefaultJobService : IJobService, IJobScheduler {

        /// <summary>
        /// Create manager
        /// </summary>
        /// <param name="jobRepository"></param>
        /// <param name="jobRepositoryEventHandlers"></param>
        public DefaultJobService(IJobRepository jobRepository,
            IEnumerable<IJobEventHandler> jobRepositoryEventHandlers) {
            _jobRepository = jobRepository;
            _jobRepositoryEventHandlers = jobRepositoryEventHandlers;
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> NewJobAsync(JobInfoModel model, CancellationToken ct) {

            SetDefaultValues(model);
            foreach (var jreh in _jobRepositoryEventHandlers) {
                await jreh.OnJobCreatingAsync(this, model);
            }
            try {
                model = await _jobRepository.AddAsync(model, ct);
            }
            catch {
                foreach (var jreh in _jobRepositoryEventHandlers) {
                    await jreh.OnJobDeletedAsync(this, model);
                }
                throw;
            }
            foreach (var jreh in _jobRepositoryEventHandlers) {
                await jreh.OnJobCreatedAsync(this, model);
            }
            return model;
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> NewOrUpdateJobAsync(string jobId,
            Func<JobInfoModel, CancellationToken, Task<bool>> predicate, CancellationToken ct) {

            var created = false;
            var job = await _jobRepository.AddOrUpdateAsync(jobId, async (model, ct) => {
                ct.ThrowIfCancellationRequested();
                if (model == null) {
                    created = true;
                    // Create new job
                    model = new JobInfoModel {
                        Id = jobId
                    };
                    SetDefaultValues(model);
                    if (!await predicate(model, ct)) {
                        return null;
                    }

                    foreach (var jreh in _jobRepositoryEventHandlers) {
                        await jreh.OnJobCreatingAsync(this, model);
                    }
                    return model;
                }

                created = false;
                var updated = model.Clone();
                if (!await predicate(updated, ct)) {
                    return null;
                }
                if (!model.RedundancyConfig.Equals(updated.RedundancyConfig)) {
                    updated.RedundancyConfig = updated.RedundancyConfig;
                    updated.LifetimeData.ProcessingStatus.Clear();
                }
                return updated;
            });

            if (created && job != null) {
                foreach (var jreh in _jobRepositoryEventHandlers) {
                    await jreh.OnJobCreatedAsync(this, job);
                }
            }
            return job;
        }

        /// <summary>
        /// Set default values
        /// </summary>
        /// <param name="model"></param>
        private static void SetDefaultValues(JobInfoModel model) {
            if (model.Id == null) {
                model.Id = Guid.NewGuid().ToString();
            }
            model.LifetimeData = new JobLifetimeDataModel {
                Created = DateTime.UtcNow,
                Status = JobStatus.Active,
                Updated = DateTime.UtcNow,
                ProcessingStatus = new Dictionary<string, ProcessingStatusModel>()
            };
            if (model.RedundancyConfig == null) {
                model.RedundancyConfig = new RedundancyConfigModel {
                    DesiredActiveAgents = 1,
                    DesiredPassiveAgents = 0
                };
            }
        }

        /// <inheritdoc/>
        public async Task DeleteJobAsync(string jobId, CancellationToken ct) {
            var job = await _jobRepository.DeleteAsync(jobId, async (model, ct) => {
                foreach (var jreh in _jobRepositoryEventHandlers) {
                    ct.ThrowIfCancellationRequested();
                    await jreh.OnJobDeletingAsync(this, model);
                }
                return true;
            }, ct);
            if (job != null) {
                // Success
                foreach (var jreh in _jobRepositoryEventHandlers) {
                    await jreh.OnJobDeletedAsync(this, job);
                }
            }
        }

        /// <inheritdoc/>
        public Task<JobInfoListModel> ListJobsAsync(string continuationToken, int? maxResults,
            CancellationToken ct) {
            return _jobRepository.QueryAsync(null, continuationToken, maxResults, ct);
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> QueryJobsAsync(JobInfoQueryModel query,
            int? maxResults, CancellationToken ct) {
            return await _jobRepository.QueryAsync(query, null, maxResults, ct);
        }

        /// <inheritdoc/>
        public async Task CancelJobAsync(string jobId, CancellationToken ct) {
            await _jobRepository.UpdateAsync(jobId, (job, ct) => {
                ct.ThrowIfCancellationRequested();
                if (job.LifetimeData.Status == JobStatus.Deleted ||
                    job.LifetimeData.Status == JobStatus.Canceled) {
                    return Task.FromResult(false); // Nothing to do
                }
                job.LifetimeData.Status = JobStatus.Canceled;
                return Task.FromResult(true);
            }, ct);
        }

        /// <inheritdoc/>
        public async Task RestartJobAsync(string jobId, CancellationToken ct) {
            await _jobRepository.UpdateAsync(jobId, (job, ct) => {
                ct.ThrowIfCancellationRequested();
                if (job.LifetimeData.Status == JobStatus.Deleted ||
                    job.LifetimeData.Status == JobStatus.Active) {
                    return Task.FromResult(false);  // Nothing to do
                }
                job.LifetimeData.Status = JobStatus.Active;
                return Task.FromResult(true);
            }, ct);
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> GetJobAsync(string jobId, CancellationToken ct) {
            return await _jobRepository.GetAsync(jobId, ct);
        }

        private readonly IJobRepository _jobRepository;
        private readonly IEnumerable<IJobEventHandler> _jobRepositoryEventHandlers;
    }
}