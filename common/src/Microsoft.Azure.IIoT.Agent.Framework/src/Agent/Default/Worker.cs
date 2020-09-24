// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Autofac;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Exceptions;
    using Prometheus;

    /// <summary>
    /// Individual agent worker
    /// </summary>
    public class Worker : IWorker, IDisposable {

        /// <inheritdoc/>
        public string AgentId => _agentConfigProvider.Config.AgentId ?? "Agent";

        /// <inheritdoc/>
        public string WorkerId => AgentId + "_" + _workerInstance;

        /// <inheritdoc/>
        public WorkerStatus Status => GetStatus();

        /// <inheritdoc/>
        public event JobFinishedEventHandler OnJobCompleted;
        /// <inheritdoc/>
        public event JobCanceledEventHandler OnJobCanceled;
        /// <inheritdoc/>
        public event JobStartedEventHandler OnJobStarted;

        /// <summary>
        /// Create worker
        /// </summary>
        /// <param name="jobManagerConnector"></param>
        /// <param name="agentConfigProvider"></param>
        /// <param name="jobConfigurationFactory"></param>
        /// <param name="workerInstance"></param>
        /// <param name="lifetimeScope"></param>
        /// <param name="logger"></param>
        /// <param name="agentRepository"></param>
        public Worker(IJobOrchestrator jobManagerConnector,
            IAgentConfigProvider agentConfigProvider, IJobSerializer jobConfigurationFactory,
            int workerInstance, ILifetimeScope lifetimeScope, ILogger logger,
            IWorkerRepository agentRepository = null) {

            _agentRepository = agentRepository;
            _jobConfigurationFactory = jobConfigurationFactory ??
                throw new ArgumentNullException(nameof(jobConfigurationFactory));
            _lifetimeScope = lifetimeScope ??
                throw new ArgumentNullException(nameof(lifetimeScope));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _agentConfigProvider = agentConfigProvider ??
                throw new ArgumentNullException(nameof(agentConfigProvider));
            _jobManagerConnector = jobManagerConnector ??
                throw new ArgumentNullException(nameof(jobManagerConnector));
            _workerInstance = workerInstance;

            _heartbeatInterval = _agentConfigProvider.GetHeartbeatInterval();
            _jobCheckerInterval = _agentConfigProvider.GetJobCheckInterval();
            _agentConfigProvider.OnConfigUpdated += (s, e) => {
                _heartbeatInterval = _agentConfigProvider.GetHeartbeatInterval();
                _jobCheckerInterval = _agentConfigProvider.GetJobCheckInterval();
            };

            _lock = new SemaphoreSlim(1, 1);
            _heartbeatTimer = new Timer(HeartbeatTimer_ElapsedAsync);
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_cts != null) {
                    _logger.Warning("Worker already running");
                    return;
                }

                _cts = new CancellationTokenSource();
                _heartbeatTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);

                _logger.Information("Worker {WorkerId}: {@Capabilities}",
                    WorkerId, _agentConfigProvider.Config.Capabilities);
                _worker = Task.Run(() => RunAsync(_cts.Token));
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                if (_cts == null) {
                    return;
                }

                _logger.Information("Stopping worker...");
                _heartbeatTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                // Stop worker
                _cts.Cancel();
                await _worker;
                _worker = null;

                System.Diagnostics.Debug.Assert(_jobProcess == null);
                _logger.Information("Worker stopped.");
            }
            catch (OperationCanceledException) { }
            catch (Exception e) {
                _logger.Error(e, "Stopping worker failed.");
            }
            finally {
                _cts?.Dispose();
                _cts = null;
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Async(StopAsync).Wait();

            System.Diagnostics.Debug.Assert(_jobProcess == null);
            _jobProcess?.Dispose();

            _cts?.Dispose();
            _heartbeatTimer.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// Heartbeat timer
        /// </summary>
        /// <param name="sender"></param>
        private async void HeartbeatTimer_ElapsedAsync(object sender) {
            try {
                _logger.Debug("Sending heartbeat...");

                // Note - will take lock for status
                var workerHeartbeat = await GetWorkerHeartbeatAsync(_cts.Token);

                await _jobManagerConnector.SendHeartbeatAsync(
                    new HeartbeatModel { Worker = workerHeartbeat }, _cts.Token);
            }
            catch (OperationCanceledException) {
                return; // Done
            }
            catch (Exception ex) {
                _logger.Information(ex, "Could not send worker heartbeat.");
                kModuleExceptions.WithLabels(AgentId, ex.Source, ex.GetType().FullName, ex.Message, ex.StackTrace, "Could not send worker hearbeat").Inc();
            }
            Try.Op(() => _heartbeatTimer.Change(_heartbeatInterval, Timeout.InfiniteTimeSpan));
        }

        /// <summary>
        /// Process job
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct) {
            _logger.Debug("Worker starting...");
            while (!ct.IsCancellationRequested) {
                try {
                    ct.ThrowIfCancellationRequested();

                    _logger.Debug("Try querying available job...");
                    var jobProcessInstruction = await Try.Async(() =>
                        _jobManagerConnector.GetAvailableJobAsync(WorkerId, new JobRequestModel {
                            Capabilities = _agentConfigProvider.Config.Capabilities
                        }, ct));

                    ct.ThrowIfCancellationRequested();
                    if (jobProcessInstruction?.Job?.JobConfiguration == null ||
                        jobProcessInstruction?.ProcessMode == null) {
                        _logger.Debug("Worker: {Id}, no job received, wait {delay} ...",
                            WorkerId, _jobCheckerInterval);
                        await Task.Delay(_jobCheckerInterval, ct);
                        continue;
                    }
                    // Process until cancelled
                    await ProcessAsync(jobProcessInstruction, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Information("Worker cancelled...");
                }
                catch (Exception ex) {
                    // TODO: we should notify the exception
                    _logger.Error(ex, "Worker: {Id}, exception during worker processing, wait {delay}...",
                        WorkerId, _jobCheckerInterval);
                    kModuleExceptions.WithLabels(AgentId, ex.Source, ex.GetType().FullName, ex.Message, ex.StackTrace, "Exception during worker processing").Inc();
                    await Task.Delay(_jobCheckerInterval, ct);
                }
            }
            _logger.Information("Worker stopping...");
        }

        /// <summary>
        /// Process jobs
        /// </summary>
        /// <returns></returns>
        private async Task ProcessAsync(JobProcessingInstructionModel jobProcessInstruction,
            CancellationToken ct) {
            try {
                // Stop worker heartbeat to start the job heartbeat process
                _heartbeatTimer.Change(-1, -1); // Stop worker heartbeat

                _logger.Information("Worker: {WorkerId} processing job: {JobId}, mode: {ProcessMode}",
                    WorkerId, jobProcessInstruction.Job.Id, jobProcessInstruction.ProcessMode);

                // Execute processor
                while (true) {
                    _jobProcess = null;
                    ct.ThrowIfCancellationRequested();
                    using (_jobProcess = new JobProcess(this, jobProcessInstruction,
                        _lifetimeScope, _logger)) {
                        await _jobProcess.WaitAsync(ct).ConfigureAwait(false); // Does not throw
                    }

                    // Check if the job is to be continued with new configuration settings
                    if (_jobProcess.JobContinuation == null) {
                        _jobProcess = null;
                        break;
                    }

                    jobProcessInstruction = _jobProcess.JobContinuation;
                    if (jobProcessInstruction?.Job?.JobConfiguration == null ||
                        jobProcessInstruction?.ProcessMode == null) {
                        _logger.Information("Job continuation invalid, continue listening...");
                        _jobProcess = null;
                        break;
                    }
                    _logger.Information("Processing job continuation...");
                }
            }
            catch (OperationCanceledException) {
                _logger.Information("Processing cancellation received ...");
                _jobProcess = null;
            }
            finally {
                _logger.Information("Worker: {WorkerId}, Job: {JobId} processing completed ... ",
                    WorkerId, jobProcessInstruction.Job.Id);
                if (!ct.IsCancellationRequested) {
                    _heartbeatTimer.Change(0, -1); // restart worker heartbeat
                }
            }
        }

        /// <summary>
        /// Worker job processing process
        /// </summary>
        private class JobProcess : IDisposable {

            /// <inheritdoc/>
            public JobProcessingInstructionModel JobContinuation { get; private set; }

            /// <inheritdoc/>
            public WorkerStatus Status { get; private set; } = WorkerStatus.Stopped;

            /// <inheritdoc/>
            public JobInfoModel Job => _currentJobProcessInstruction.Job;

            /// <summary>
            /// Create processor
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="jobProcessInstruction"></param>
            /// <param name="workerScope"></param>
            /// <param name="logger"></param>
            public JobProcess(Worker outer, JobProcessingInstructionModel jobProcessInstruction,
                ILifetimeScope workerScope, ILogger logger) {
                _outer = outer;
                _logger = logger.ForContext<JobProcess>();
                _currentJobProcessInstruction = jobProcessInstruction;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    _outer._cts.Token);

                // Do autofac injection to resolve processing engine
                var jobConfig = _outer._jobConfigurationFactory.DeserializeJobConfiguration(
                    Job.JobConfiguration, Job.JobConfigurationType);
                var jobProcessorFactory = workerScope.ResolveNamed<IProcessingEngineContainerFactory>(
                    Job.JobConfigurationType, new NamedParameter(nameof(jobConfig), jobConfig));

                _jobScope = workerScope.BeginLifetimeScope(
                    jobProcessorFactory.GetJobContainerScope(outer.WorkerId, Job.Id));
                _currentProcessingEngine = _jobScope.Resolve<IProcessingEngine>();

                // Continuously send job status heartbeats
                _heartbeatTimer = new Timer(_ => OnHeartbeatTimerAsync().Wait());
                _processor = Task.Run(() => ProcessAsync());
            }

            /// <summary>
            /// Wait till completion or heartbeat cancelling
            /// </summary>
            /// <returns></returns>
            public Task WaitAsync(CancellationToken ct) {
                return _processor.ContinueWith(_ => Task.CompletedTask, ct);
            }

            /// <inheritdoc/>
            public void Dispose() {
                _heartbeatTimer.Dispose();
                _jobScope.Dispose();
                _cancellationTokenSource.Dispose();
            }

            /// <summary>
            /// Processor
            /// </summary>
            /// <returns></returns>
            private async Task ProcessAsync() {
                try {
                    Status = WorkerStatus.ProcessingJob;
                    _outer.OnJobStarted?.Invoke(this, new JobInfoEventArgs(Job));

                    // Start sending heartbeats
                    _heartbeatTimer.Change(TimeSpan.FromSeconds(1), _outer._heartbeatInterval);

                    await _currentProcessingEngine.RunAsync(_currentJobProcessInstruction.ProcessMode.Value,
                        _cancellationTokenSource.Token);

                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    _logger.Information("Job {job} completed.", Job.Id);
                }
                catch (OperationCanceledException) {
                    _logger.Information("Job {job} cancelled.", Job.Id);
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error processing job {job}.", Job.Id);
                    kModuleExceptions.WithLabels("", ex.Source, ex.GetType().FullName, ex.Message,
                        ex.StackTrace, "Error processing job " + Job.Id).Inc();
                    Job.LifetimeData.Status = JobStatus.Error;
                }
                finally {
                    // Stop sending heartbeats
                    _heartbeatTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    await CleanupAsync();
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
                    if (_cancellationTokenSource.IsCancellationRequested) {
                        _logger.Debug("Update cancellation status for {job}.", Job.Id);
                        // Job was cancelled
                        Job.LifetimeData.Status = JobStatus.Canceled;
                        await SendHeartbeatAsync(CancellationToken.None);
                        _outer.OnJobCanceled?.Invoke(this, new JobInfoEventArgs(Job));
                    }
                    else {
                        _logger.Debug("Update completion status for {job}.", Job.Id);
                        if (Job.LifetimeData.Status != JobStatus.Error) {
                            Job.LifetimeData.Status = JobStatus.Completed;
                        }
                        await SendHeartbeatAsync(CancellationToken.None);
                        _outer.OnJobCompleted?.Invoke(this, new JobInfoEventArgs(Job));
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error sending heartbeat and status for {job} - continue.",
                        Job.Id);
                }
                finally {
                    Status = WorkerStatus.WaitingForJob;
                }
            }

            /// <summary>
            /// Send heartbeat
            /// </summary>
            /// <returns></returns>
            private async Task SendHeartbeatAsync(CancellationToken ct = default) {
                _logger.Debug("Sending job processor heartbeat...");
                var workerHeartbeat = await _outer.GetWorkerHeartbeatAsync(ct);
                var result = await _outer._jobManagerConnector.SendHeartbeatAsync(
                    new HeartbeatModel {
                        Worker = workerHeartbeat,
                        Job = new JobHeartbeatModel {
                            JobId = Job.Id,
                            JobHash = Job.GetHashSafe(),
                            Status = Job.LifetimeData.Status,
                            ProcessMode = _currentJobProcessInstruction.ProcessMode.Value,
                            State = await _currentProcessingEngine.GetCurrentJobState()
                        }
                    }, ct);

                // Check for updated job
                if (result.UpdatedJob != null && JobContinuation == null) {
                    JobContinuation = result.UpdatedJob;
                    // Cancel
                    if (!_cancellationTokenSource.IsCancellationRequested) {
                        _logger.Debug("Received job update request - continue ...");
                        _cancellationTokenSource.Cancel();
                        return;
                    }
                }

                // Process instructions
                switch (result.HeartbeatInstruction) {
                    case HeartbeatInstruction.SwitchToActive:
                        await _currentProcessingEngine.SwitchProcessMode(ProcessMode.Active,
                            result.LastActiveHeartbeat);
                        break;
                    case HeartbeatInstruction.CancelProcessing:
                        if (!_cancellationTokenSource.IsCancellationRequested) {
                            _logger.Debug("Received job cancellation, cancelling processing...");
                            _cancellationTokenSource.Cancel();
                        }
                        break;
                    case HeartbeatInstruction.Keep:
                        // nothing to do.
                        break;
                }
            }

            /// <summary>
            /// Heartbeat timer expired
            /// </summary>
            private async Task OnHeartbeatTimerAsync() {
                try {
                    await SendHeartbeatAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException) { }
                catch (ResourceNotFoundException) {
                    if (!_cancellationTokenSource.IsCancellationRequested) {
                        _logger.Debug("Heartbeat returned job not found - cancelling ...");
                        _cancellationTokenSource.Cancel();
                        return;
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Could not send worker heartbeat.");
                }
            }

            private readonly Timer _heartbeatTimer;
            private readonly Task _processor;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly JobProcessingInstructionModel _currentJobProcessInstruction;
            private readonly IProcessingEngine _currentProcessingEngine;
            private readonly ILifetimeScope _jobScope;
            private readonly Worker _outer;
            private readonly ILogger _logger;
        }

        /// <summary>
        /// Get worker heartbeat
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<WorkerHeartbeatModel> GetWorkerHeartbeatAsync(CancellationToken ct) {
            var workerHeartbeat = new WorkerHeartbeatModel {
                WorkerId = WorkerId,
                AgentId = AgentId,
                Status = GetStatus()
            };
            if (_agentRepository != null) {
                await _agentRepository.AddOrUpdate(workerHeartbeat, ct);
            }
            return workerHeartbeat;
        }

        /// <summary>
        /// Get status under lock
        /// </summary>
        /// <returns></returns>
        private WorkerStatus GetStatus() {
            _lock.Wait();
            try {
                return _jobProcess?.Status ??
                    (_cts == null ? WorkerStatus.Stopped :
                        (_cts.IsCancellationRequested ? WorkerStatus.Stopping :
                            WorkerStatus.WaitingForJob));
            }
            finally {
                _lock.Release();
            }
        }

        private readonly int _workerInstance;
        private readonly IAgentConfigProvider _agentConfigProvider;
        private readonly IWorkerRepository _agentRepository;
        private readonly Timer _heartbeatTimer;
        private readonly SemaphoreSlim _lock;
        private readonly IJobSerializer _jobConfigurationFactory;
        private readonly IJobOrchestrator _jobManagerConnector;
        private readonly ILifetimeScope _lifetimeScope;
        private readonly ILogger _logger;

        private TimeSpan _jobCheckerInterval;
        private TimeSpan _heartbeatInterval;
        private JobProcess _jobProcess;
        private Task _worker;
        private CancellationTokenSource _cts;
        private static readonly Counter kModuleExceptions = Metrics.CreateCounter("iiot_edge_publisher_exceptions", "module exceptions",
            new CounterConfiguration {
                LabelNames = new[] { "agent", "source", "type", "message", "stacktrace", "custom_message" }
            });
    }
}