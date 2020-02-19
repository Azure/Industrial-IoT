// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;

    /// <summary>
    /// Individual agent worker
    /// </summary>
    public class Worker : IWorker, IDisposable {
        /// <summary>
        /// Create worker
        /// </summary>
        /// <param name="agentConfigProvider"></param>
        /// <param name="jobConfigurationFactory"></param>
        /// <param name="lifetimeScope"></param>
        /// <param name="logger"></param>
        /// <param name="jobHeartbeatCollection"></param>
        /// <param name="jobProcessingInstruction"></param>
        public Worker(IAgentConfigProvider agentConfigProvider, IJobSerializer jobConfigurationFactory,
            ILifetimeScope lifetimeScope, ILogger logger,
            IJobHeartbeatCollection jobHeartbeatCollection, JobProcessingInstructionModel jobProcessingInstruction) {
            _logger = logger;
            _currentJobProcessInstruction = jobProcessingInstruction;
            _jobHeartbeatCollection = jobHeartbeatCollection;
            _lock = new SemaphoreSlim(1, 1);

            _heartbeatInterval = agentConfigProvider.GetHeartbeatInterval();

            // Do autofac injection to resolve processing engine
            var jobConfig = jobConfigurationFactory.DeserializeJobConfiguration(Job.JobConfiguration, Job.JobConfigurationType);

            var jobProcessorFactory = lifetimeScope.ResolveNamed<IProcessingEngineContainerFactory>(
                Job.JobConfigurationType, new NamedParameter(nameof(jobConfig), jobConfig));

            _jobScope = lifetimeScope.BeginLifetimeScope(
                jobProcessorFactory.GetJobContainerScope(Job.Id));
            _currentProcessingEngine = _jobScope.Resolve<IProcessingEngine>();
        }

        /// <inheritdoc />
        public event JobFinishedEventHandler OnJobCompleted;

        /// <inheritdoc />
        public event JobCanceledEventHandler OnJobCanceled;

        /// <inheritdoc />
        public event JobStartedEventHandler OnJobStarted;

        /// <inheritdoc />
        public JobProcessingInstructionModel JobContinuation { get; private set; }

        /// <inheritdoc />
        public JobStatus Status => Job.LifetimeData.Status;

        /// <inheritdoc />
        public JobInfoModel Job => _currentJobProcessInstruction.Job;

        /// <inheritdoc />
        public void Dispose() {
            _cts.Cancel();

            while (Status == JobStatus.Active) {
                Task.Delay(200).Wait();
            }

            _cts?.Dispose();
            _lock.Dispose();
        }

        /// <inheritdoc />
        public Task ProcessHeartbeatResult(HeartbeatResultEntryModel result) {
            switch (result.HeartbeatInstruction) {
                case HeartbeatInstruction.Keep:
                    break;
                case HeartbeatInstruction.SwitchToActive:
                    break;
                case HeartbeatInstruction.CancelProcessing:
                    JobContinuation = result.UpdatedJob;
                    _cts.Cancel();
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Process jobs
        /// </summary>
        /// <returns></returns>
        public async Task ProcessAsync(CancellationToken ct) {
            // Execute processor
            while (true) {
                _logger.Information("Starting to process new job...");

                // Continuously send job status heartbeats
                _heartbeatTimer = new Timer(_ => HeartbeatTimer_ElapsedAsync().Wait());

                ct.ThrowIfCancellationRequested();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                try {
                    OnJobStarted?.Invoke(this, new JobInfoEventArgs(Job, this));

                    // Start sending heartbeats
                    _heartbeatTimer.Change(TimeSpan.FromSeconds(1), _heartbeatInterval);

                    await _currentProcessingEngine.RunAsync(_currentJobProcessInstruction.ProcessMode.Value,
                        _cts.Token);

                    _cts.Token.ThrowIfCancellationRequested();
                    _logger.Information("Job {job} completed.", Job.Id);
                }
                catch (OperationCanceledException) {
                    _logger.Information("Job {job} cancelled.", Job.Id);
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error processing job {job}.", Job.Id);
                    Job.LifetimeData.Status = JobStatus.Error;
                }
                finally {
                    // Stop sending heartbeats
                    _heartbeatTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    await CleanupAsync();
                }

                //Check if the job is to be continued with new configuration settings
                if (JobContinuation == null) {
                    break;
                }

                _currentJobProcessInstruction = JobContinuation;
                JobContinuation = null;
                if (_currentJobProcessInstruction?.Job?.JobConfiguration == null ||
                    _currentJobProcessInstruction?.ProcessMode == null) {
                    _logger.Information("Job continuation invalid, continue listening...");
                    break;
                }

                _logger.Information("Processing job continuation...");
            }
        }

        private async Task HeartbeatTimer_ElapsedAsync() {
            await SendHeartBeatAsync();
            Try.Op(() => _heartbeatTimer.Change(_heartbeatInterval, Timeout.InfiniteTimeSpan));
        }

        private async Task SendHeartBeatAsync() {
            try {
                var heartbeat = await GetJobHeartbeatAsync();
                await _jobHeartbeatCollection.AddOrUpdate(Job.Id, heartbeat);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Could not store worker heartbeat.");
            }
        }

        /// <summary>
        /// Get job heartbeat
        /// </summary>
        /// <returns></returns>
        private async Task<JobHeartbeatModel> GetJobHeartbeatAsync() {
            try {
                await _lock.WaitAsync();

                var jobHeartbeat = new JobHeartbeatModel {
                    JobId = Job.Id,
                    JobHash = Job.GetHashSafe(),
                    Status = Job.LifetimeData.Status,
                    ProcessMode = _currentJobProcessInstruction.ProcessMode.Value,
                    State = await _currentProcessingEngine.GetCurrentJobState()
                };

                return jobHeartbeat;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Cleanup job processing
        /// </summary>
        /// <returns></returns>
        private async Task CleanupAsync() {
            if (JobContinuation != null) {
                // Continuation - do not update job state but continue
                return;
            }

            try {
                if (_cts.IsCancellationRequested) {
                    _logger.Debug("Update cancellation status for {job}.", Job.Id);
                    // Job was cancelled
                    Job.LifetimeData.Status = JobStatus.Canceled;
                    await SendHeartBeatAsync();
                    OnJobCanceled?.Invoke(this, new JobInfoEventArgs(Job, this));
                }
                else {
                    _logger.Debug("Update completion status for {job}.", Job.Id);
                    if (Job.LifetimeData.Status != JobStatus.Error) {
                        Job.LifetimeData.Status = JobStatus.Completed;
                    }

                    await SendHeartBeatAsync();
                    OnJobCompleted?.Invoke(this, new JobInfoEventArgs(Job, this));
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error sending heartbeat and status for {job} - continue.",
                    Job.Id);
            }
        }

        private readonly IProcessingEngine _currentProcessingEngine;
        private readonly TimeSpan _heartbeatInterval;
        private readonly IJobHeartbeatCollection _jobHeartbeatCollection;
        private readonly ILifetimeScope _jobScope;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private CancellationTokenSource _cts;
        private JobProcessingInstructionModel _currentJobProcessInstruction;
        private Timer _heartbeatTimer;
    }
}