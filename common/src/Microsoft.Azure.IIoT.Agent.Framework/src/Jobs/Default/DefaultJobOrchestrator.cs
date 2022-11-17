// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job repository connector
    /// </summary>
    public class DefaultJobOrchestrator : IJobOrchestrator {

        /// <summary>
        /// Create manager
        /// </summary>
        /// <param name="jobRepository"></param>
        /// <param name="workerRepository"></param>
        /// <param name="demandMatcher"></param>
        /// <param name="jobOrchestratorConfig"></param>
        /// <param name="jobEventHandler"></param>
        /// <param name="jobService"></param>
        /// <param name="logger"></param>
        public DefaultJobOrchestrator(IJobRepository jobRepository,
            IWorkerRepository workerRepository, IDemandMatcher demandMatcher,
            IJobOrchestratorConfig jobOrchestratorConfig,
            IJobEventHandler jobEventHandler,
            IJobService jobService,
            ILogger logger) {
            _logger = logger;
            _jobRepository = jobRepository;
            _demandMatcher = demandMatcher;
            _workerRepository = workerRepository;
            _jobOrchestratorConfig = jobOrchestratorConfig;
            _jobEventHandler = jobEventHandler;
            _jobService = jobService;
        }

        /// <inheritdoc/>
        public async Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId,
            JobRequestModel request, CancellationToken ct) {
            var query = new JobInfoQueryModel {
                Status = JobStatus.Active // Only query active jobs
            };
            string continuationToken = null;
            do {
                var jobList = await _jobRepository.QueryAsync(query, continuationToken, null, ct);
                if (jobList?.Jobs == null) {
                    break;
                }
                Debug.Assert(!jobList.Jobs.Any(j => j.LifetimeData.Status != JobStatus.Active));

                // Filter demands
                var demandFilteredJobs = jobList.Jobs
                    .Where(j => _demandMatcher.MatchCapabilitiesAndDemands(j.Demands,
                        request?.Capabilities));

                foreach (var job in demandFilteredJobs) {
                    // Test the listed job first before hitting the database
                    var jobProcessInstruction = CalculateInstructions(job, workerId);
                    if (jobProcessInstruction != null) {
                        try {
                            await _jobRepository.UpdateAsync(job.Id, (existingJob, ct) => {
                                ct.ThrowIfCancellationRequested();
                                // Try again on the current value in the database
                                jobProcessInstruction = CalculateInstructions(existingJob, workerId);
                                if (jobProcessInstruction != null) {
                                    _jobEventHandler.OnJobAssignmentAsync(_jobService, jobProcessInstruction.Job, workerId).Wait();
                                }
                                return Task.FromResult(jobProcessInstruction != null);
                            }, ct);
                            if (jobProcessInstruction != null) {
                                _logger.Information("Assigned new job {jobId} to worker {workerId}", jobProcessInstruction.Job.Id, workerId);
                                return jobProcessInstruction;
                            }
                            _logger.Debug("Job stolen after query, continue.");
                        }
                        catch (ResourceNotFoundException) {
                            _logger.Debug("Job deleted after query, continue.");
                        }
                    }
                }
                continuationToken = jobList.ContinuationToken;
                Debug.Assert(continuationToken != string.Empty);
            }
            while (!string.IsNullOrEmpty(continuationToken));

            _logger.Debug("No job for worker {workerId}", workerId);
            return null;
        }

        /// <inheritdoc/>
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat,
            JobDiagnosticInfoModel info, CancellationToken ct) {
            if (_workerRepository != null) {
                await _workerRepository.AddOrUpdate(heartbeat.Worker);
            }

            var result = new HeartbeatResultModel {
                HeartbeatInstruction = HeartbeatInstruction.Keep,
                LastActiveHeartbeat = null,
                UpdatedJob = null
            };

            if (heartbeat.Job == null) {
                // Worker heartbeat
                return result;
            }

            var job = await _jobRepository.UpdateAsync(heartbeat.Job.JobId, (existingJob , ct) => {
                ct.ThrowIfCancellationRequested();
                if (existingJob.GetHashSafe() != heartbeat.Job.JobHash) {

                    // job was updated - instruct worker to reset
                    result.UpdatedJob = new JobProcessingInstructionModel {
                        Job = existingJob,
                        ProcessMode = heartbeat.Job.ProcessMode
                    };
                }

                if (existingJob.LifetimeData == null) {
                    existingJob.LifetimeData = new JobLifetimeDataModel();
                }

                if (existingJob.LifetimeData.Status == JobStatus.Canceled ||
                    existingJob.LifetimeData.Status == JobStatus.Deleted) {
                    result.HeartbeatInstruction = HeartbeatInstruction.CancelProcessing;
                    result.UpdatedJob = null;
                    result.LastActiveHeartbeat = null;
                }

                if (result.HeartbeatInstruction == HeartbeatInstruction.Keep) {
                    existingJob.LifetimeData.Status = heartbeat.Job.Status;
                }

                var processingStatus = new ProcessingStatusModel {
                    LastKnownHeartbeat = DateTime.UtcNow,
                    LastKnownState = heartbeat.Job.State,
                    ProcessMode = // Unset processing mode to do correct calculation of active agents
                        heartbeat.Job.Status == JobStatus.Active ?
                            heartbeat.Job.ProcessMode : (ProcessMode?)null
                };

                if (existingJob.LifetimeData.ProcessingStatus == null) {
                    existingJob.LifetimeData.ProcessingStatus = new Dictionary<string, ProcessingStatusModel>();
                }
                existingJob.LifetimeData.ProcessingStatus[heartbeat.Worker.WorkerId] = processingStatus;

                var numberOfActiveAgents = existingJob.LifetimeData.ProcessingStatus
                    .Count(j =>
                        j.Value.ProcessMode == ProcessMode.Active &&
                        j.Value.LastKnownHeartbeat > DateTime.UtcNow.Subtract(_jobOrchestratorConfig.JobStaleTime));

                if (processingStatus.ProcessMode == ProcessMode.Passive &&
                    numberOfActiveAgents < existingJob.RedundancyConfig.DesiredActiveAgents) {

                    var lastActiveHeartbeat = existingJob.LifetimeData.ProcessingStatus
                        .Where(s => s.Value.ProcessMode == ProcessMode.Active)
                        .OrderByDescending(s => s.Value.LastKnownHeartbeat)
                        .Select(s => s.Value.LastKnownHeartbeat)
                        .FirstOrDefault();

                    // Switch this passive agent to active
                    result.HeartbeatInstruction = HeartbeatInstruction.SwitchToActive;
                    result.LastActiveHeartbeat = lastActiveHeartbeat;

                    existingJob.LifetimeData.ProcessingStatus[heartbeat.Worker.WorkerId].ProcessMode =
                        ProcessMode.Active;
                }
                return Task.FromResult(true);
            }, ct);

            _logger.Debug("Worker {workerId} updated heartbeat for job {jobId}.",
                heartbeat.Worker?.AgentId, heartbeat.Job?.JobId);
            return result;
        }

        /// <summary>
        /// Calculate new processing instructions if possible
        /// </summary>
        /// <param name="job"></param>
        /// <param name="workerId"></param>
        /// <returns></returns>
        private JobProcessingInstructionModel CalculateInstructions(JobInfoModel job,
            string workerId) {
            var numberOfActiveAgents = job.LifetimeData.ProcessingStatus
                .Count(j => j.Value.ProcessMode == ProcessMode.Active &&
                    j.Value.LastKnownHeartbeat > DateTime.UtcNow.Subtract(_jobOrchestratorConfig.JobStaleTime) &&
                    j.Key != workerId);
            var numberOfPassiveAgents =
                job.LifetimeData.ProcessingStatus
                .Count(j => j.Value.ProcessMode == ProcessMode.Passive &&
                    j.Value.LastKnownHeartbeat > DateTime.UtcNow.Subtract(_jobOrchestratorConfig.JobStaleTime) &&
                    j.Key != workerId);

            if (numberOfActiveAgents < job.RedundancyConfig.DesiredActiveAgents) {
                job.LifetimeData.ProcessingStatus[workerId] = new ProcessingStatusModel {
                    ProcessMode = ProcessMode.Active,
                    LastKnownHeartbeat = DateTime.UtcNow
                };
                return new JobProcessingInstructionModel {
                    Job = job,
                    ProcessMode = ProcessMode.Active
                };
            }

            if (numberOfPassiveAgents < job.RedundancyConfig.DesiredPassiveAgents) {
                job.LifetimeData.ProcessingStatus[workerId] = new ProcessingStatusModel {
                    ProcessMode = ProcessMode.Passive,
                    LastKnownHeartbeat = DateTime.UtcNow
                };
                return new JobProcessingInstructionModel {
                    Job = job,
                    ProcessMode = ProcessMode.Passive
                };
            }
            return null;
        }

        private readonly IDemandMatcher _demandMatcher;
        private readonly IJobOrchestratorConfig _jobOrchestratorConfig;
        private readonly IWorkerRepository _workerRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IJobEventHandler _jobEventHandler;
        private readonly IJobService _jobService;
        private readonly ILogger _logger;
    }
}