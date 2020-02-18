// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Collections.Generic;
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
        public DefaultJobOrchestrator(IJobRepository jobRepository,
            IDemandMatcher demandMatcher,
            IJobOrchestratorConfig jobOrchestratorConfig,
            IAgentRepository workerRepository = null) {
            _jobRepository = jobRepository;
            _demandMatcher = demandMatcher;
            _agentRepository = workerRepository;
            _jobOrchestratorConfig = jobOrchestratorConfig;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<JobProcessingInstructionModel>> GetAvailableJobsAsync(string workerSupervisorId,
            JobRequestModel request, CancellationToken ct) {
            var jobsToReturn = new List<JobProcessingInstructionModel>(request.MaxJobCount);

            var query = new JobInfoQueryModel {
                Status = JobStatus.Active // Only query active jobs
            };
            string continuationToken = null;
            do {
                var jobList = await _jobRepository.QueryAsync(query, continuationToken, null, ct);
                System.Diagnostics.Debug
                    .Assert(!jobList.Jobs.Any(j => j.LifetimeData.Status != JobStatus.Active));

                // Filter demands
                var demandFilteredJobs = jobList.Jobs
                    .Where(j => _demandMatcher.MatchCapabilitiesAndDemands(j.Demands,
                        request?.Capabilities))
                    .ToArray();

                foreach (var job in demandFilteredJobs) {
                    // Test the listed job first before hitting the database
                    var jobProcessInstruction = CalculateInstructions(job, workerSupervisorId);
                    if (jobProcessInstruction != null) {
                        try {
                            await _jobRepository.UpdateAsync(job.Id, existingJob => {
                                // Try again on the current value in the database
                                jobProcessInstruction = CalculateInstructions(existingJob, workerSupervisorId);
                                return Task.FromResult(jobProcessInstruction != null);
                            }, ct);

                            jobsToReturn.Add(jobProcessInstruction);

                            if (jobsToReturn.Count == request.MaxJobCount) {
                                return jobsToReturn.ToArray();
                            }
                        }
                        catch (ResourceNotFoundException) {
                            continue; // Job deleted while updating, continue to next job
                        }
                    }
                }
                continuationToken = jobList.ContinuationToken;
            }
            while (continuationToken != null);
            
            return jobsToReturn.ToArray();
        }

        /// <inheritdoc/>
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat,
            CancellationToken ct) {
            if (_agentRepository != null) {
                await _agentRepository.AddOrUpdate(heartbeat.SupervisorHeartbeat);
            }

            var results = new List<HeartbeatResultEntryModel>();

            foreach (var entry in heartbeat.JobHeartbeats) {
                var result = new HeartbeatResultEntryModel {
                    HeartbeatInstruction = HeartbeatInstruction.Keep,
                    LastActiveHeartbeat = null,
                    UpdatedJob = null,
                    JobId = entry.JobId
                };

                var extJob = await _jobRepository.GetAsync((entry.JobId));

                var jobExists = extJob != null;

                if (!jobExists) {
                    result.HeartbeatInstruction = HeartbeatInstruction.CancelProcessing;
                }
                else {

                    var job = await _jobRepository.UpdateAsync(entry.JobId, existingJob => {
                        if (existingJob.GetHashSafe() != entry.JobHash) {

                            // job was updated - instruct worker to reset
                            result.UpdatedJob = new JobProcessingInstructionModel {
                                Job = existingJob, ProcessMode = entry.ProcessMode
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
                            existingJob.LifetimeData.Status = entry.Status;
                        }

                        var processingStatus = new ProcessingStatusModel {
                            LastKnownHeartbeat = DateTime.UtcNow,
                            LastKnownState = entry.State,
                            ProcessMode = // Unset processing mode to do correct calculation of active agents
                                entry.Status == JobStatus.Active ? entry.ProcessMode : (ProcessMode?)null
                        };

                        if (existingJob.LifetimeData.ProcessingStatus == null) {
                            existingJob.LifetimeData.ProcessingStatus = new Dictionary<string, ProcessingStatusModel>();
                        }

                        existingJob.LifetimeData.ProcessingStatus[heartbeat.SupervisorHeartbeat.SupervisorId] =
                            processingStatus;

                        var numberOfActiveAgents = existingJob.LifetimeData.ProcessingStatus
                            .Count(j =>
                                j.Value.ProcessMode == ProcessMode.Active &&
                                j.Value.LastKnownHeartbeat >
                                DateTime.UtcNow.Subtract(_jobOrchestratorConfig.JobStaleTime));

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

                            existingJob.LifetimeData.ProcessingStatus[heartbeat.SupervisorHeartbeat.SupervisorId]
                                    .ProcessMode =
                                ProcessMode.Active;
                        }

                        return Task.FromResult(true);
                    }, ct);
                }

                results.Add(result);
            }

            return new HeartbeatResultModel(results);
        }

        /// <summary>
        /// Calculate new processing instructions if possible
        /// </summary>
        /// <param name="job"></param>
        /// <param name="workerSupervisorId"></param>
        /// <returns></returns>
        private JobProcessingInstructionModel CalculateInstructions(JobInfoModel job,
            string workerSupervisorId) {
            var numberOfActiveAgents = job.LifetimeData.ProcessingStatus
                .Count(j => j.Value.ProcessMode == ProcessMode.Active &&
                    j.Value.LastKnownHeartbeat > DateTime.UtcNow.Subtract(_jobOrchestratorConfig.JobStaleTime) &&
                    j.Key != workerSupervisorId);
            var numberOfPassiveAgents =
                job.LifetimeData.ProcessingStatus
                .Count(j => j.Value.ProcessMode == ProcessMode.Passive &&
                    j.Value.LastKnownHeartbeat > DateTime.UtcNow.Subtract(_jobOrchestratorConfig.JobStaleTime) &&
                    j.Key != workerSupervisorId);

            if (numberOfActiveAgents < job.RedundancyConfig.DesiredActiveAgents) {
                job.LifetimeData.ProcessingStatus[workerSupervisorId] = new ProcessingStatusModel {
                    ProcessMode = ProcessMode.Active,
                    LastKnownHeartbeat = DateTime.UtcNow
                };
                return new JobProcessingInstructionModel {
                    Job = job,
                    ProcessMode = ProcessMode.Active
                };
            }

            if (numberOfPassiveAgents < job.RedundancyConfig.DesiredPassiveAgents) {
                job.LifetimeData.ProcessingStatus[workerSupervisorId] = new ProcessingStatusModel {
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
        private readonly IAgentRepository _agentRepository;
        private readonly IJobRepository _jobRepository;
    }
}