// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Prometheus;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

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
            _exitHeartbeat = true;

            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                if (_cts != null) {
                    _logger.Warning("Worker already running");
                    return;
                }

                StartSendingHeartbeat();
                _agentConfigProvider.OnConfigUpdated += ConfigUpdate_Handler;
                _cts = new CancellationTokenSource();

                _logger.Information("Starting worker {WorkerId}: {@Capabilities}",
                    WorkerId, _agentConfigProvider.Config.Capabilities);
                _worker = Task.Factory.StartNew(() => RunAsync(_cts.Token), 
                    TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            if (_cts == null) {
                return;
            }

            _logger.Debug("Stopping worker...");
            _agentConfigProvider.OnConfigUpdated -= ConfigUpdate_Handler;
            await StopHeartbeatAsync().ConfigureAwait(false);

            // Inform services, that this worker has stopped working,
            // so orchestrator can reassign the job
            await StopJobProcess().ConfigureAwait(false);

            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                // Stop worker
                _cts.Cancel();
                await _worker.ConfigureAwait(false);
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
            System.Diagnostics.Debug.Assert(_exitHeartbeat);
            _jobProcess?.Dispose();

            _cts?.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// Get Diagnostic Info
        /// </summary>
        private JobDiagnosticInfoModel GetDiagnosticInfo() {
            if (_jobProcess != null) {
                return _jobProcess.GetProcessDiagnosticInfo();
            }
            return null;
        }

        /// <summary>
        /// Handler for ConfigUpdated event
        /// </summary>
        private void ConfigUpdate_Handler(object sender, EventArgs eventArgs) {
            _heartbeatInterval = _agentConfigProvider.GetHeartbeatInterval();
            _jobCheckerInterval = _agentConfigProvider.GetJobCheckInterval();

            if (_cts != null && !_cts.IsCancellationRequested) {
                if (_jobProcess != null) {
                    _jobProcess.ResetHeartbeat();
                }
                else {
                    // Reset heartbeat
                    if (!_exitHeartbeat) {
                        _resetTimer.Cancel();
                        _resetTimer = new CancellationTokenSource();
                    }

                    // Or reset job query
                    if (_reset != null) {
                        _reset.TrySetResult(true);
                    }
                }
            }
        }

        private void StartSendingHeartbeat() {
            if (_exitHeartbeat) {
                _exitHeartbeat = false;
                _resetTimer = new CancellationTokenSource();
                _heartbeatTimerTask = Task.Factory.StartNew(() => SendHeartbeatsAsync(), 
                    TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// Heartbeat timer loop
        /// </summary>
        private async Task SendHeartbeatsAsync() {
            var timerInterval = TimeSpan.MinValue;
            PeriodicTimer timer = null;
            try {
                while (!_exitHeartbeat) {
                    if (timerInterval != _heartbeatInterval) {
                        timerInterval = _heartbeatInterval;
                        timer?.Dispose();
                        timer = new PeriodicTimer(timerInterval);
                    }

                    var resetTimerToken = _resetTimer;
                    try {
                        _logger.Debug("Worker {workerId} sending heartbeat...", WorkerId);

                        // Note - will take lock for status
                        var workerHeartbeat = await GetWorkerHeartbeatAsync(resetTimerToken.Token).ConfigureAwait(false);

                        await _jobManagerConnector.SendHeartbeatAsync(
                            new HeartbeatModel { Worker = workerHeartbeat },
                            GetDiagnosticInfo(),
                            resetTimerToken.Token
                        ).ConfigureAwait(false);

                        // Wait for timer or exit
                        if (!await timer.WaitForNextTickAsync(resetTimerToken.Token).ConfigureAwait(false)) {
                            System.Diagnostics.Debug.Assert(false, "Unexpected disposal of timer.");
                            return;
                        }
                    }
                    catch (OperationCanceledException) {
                        if (resetTimerToken.IsCancellationRequested) {
                            resetTimerToken.Dispose();
                        }
                        // Continue
                    }
                    catch (Exception ex) when (ex is ITransientException) {
                        _logger.Debug("Heartbeat endpoint busy...");
                    }
                    catch (Exception ex) {
                        _logger.Debug(ex, "Worker {workerId} could not send heartbeat.", WorkerId);
                        kModuleExceptions.WithLabels(AgentId, ex.Source, ex.GetType().FullName,
                            ex.Message, ex.StackTrace, "Could not send worker hearbeat").Inc();
                    }
                }
            }
            finally {
                timer?.Dispose();
            }
        }

        private async Task StopHeartbeatAsync() {
            _exitHeartbeat = true;
            if (_resetTimer != null) {
                _resetTimer.Cancel(); // is disposed in timer task
            }
            if (_heartbeatTimerTask != null) {
                await _heartbeatTimerTask.ConfigureAwait(false);
                _heartbeatTimerTask = null;
            }
        }

        /// <summary>
        /// Process the actual job
        /// </summary>
        private async Task RunAsync(CancellationToken ct) {
            _logger.Debug("Worker {workerId} starting...", WorkerId);
            while (!ct.IsCancellationRequested) {
                try {
                    ct.ThrowIfCancellationRequested();

                    _logger.Debug("Worker {workerId} querying available job...", WorkerId);
                    var jobProcessInstruction = await Try.Async(() =>
                        _jobManagerConnector.GetAvailableJobAsync(WorkerId, new JobRequestModel {
                            Capabilities = _agentConfigProvider.Config.Capabilities
                        }, ct)).ConfigureAwait(false);

                    if (jobProcessInstruction?.Job?.JobConfiguration == null ||
                        jobProcessInstruction?.ProcessMode == null) {
                        _logger.Debug("Worker: {Id}, no job received, wait {delay} ...",
                            WorkerId, _jobCheckerInterval);
                        _reset = new TaskCompletionSource<bool>();
                        var delay = Task.Delay(_jobCheckerInterval, ct);
                        await Task.WhenAny(delay, _reset.Task).ConfigureAwait(false);
                        _reset = null;
                        continue;
                    }

                    _logger.Information("Worker {Id} start processing job {job}...",
                        WorkerId, jobProcessInstruction.Job.Id);

                    // Process until cancelled
                    await ProcessAsync(jobProcessInstruction, ct).ConfigureAwait(false);

                    _logger.Information("Worker {Id} finished processing job {job}.",
                        WorkerId, jobProcessInstruction.Job.Id);
                }
                catch (OperationCanceledException) {
                    if (ct.IsCancellationRequested) {
                        _logger.Information("Worker {workerId} cancelled...", WorkerId);
                        return;
                    }
                }
                catch (Exception ex) {
                    // TODO: we should notify the exception
                    _logger.Error(ex, "Worker {workerId}, exception during worker processing, wait {delay}...",
                        WorkerId, _jobCheckerInterval);
                    kModuleExceptions.WithLabels(AgentId, ex.Source, ex.GetType().FullName,
                        ex.Message, ex.StackTrace, "Exception during worker processing").Inc();
                    await Task.Delay(_jobCheckerInterval, ct).ConfigureAwait(false);
                }
            }
            _logger.Information("Worker {workerId} stopped...", WorkerId);
        }

        /// <summary>
        /// Process jobs
        /// </summary>
        private async Task ProcessAsync(JobProcessingInstructionModel jobProcessInstruction,
            CancellationToken ct) {
            var currentProcessInstruction = jobProcessInstruction ??
                throw new ArgumentNullException(nameof(jobProcessInstruction));

            try {
                // Stop worker heartbeat to start the job heartbeat process
                await StopHeartbeatAsync().ConfigureAwait(false);

                _logger.Information("Worker {WorkerId} processing job {JobId}, mode: {ProcessMode}",
                    WorkerId, currentProcessInstruction.Job.Id, currentProcessInstruction.ProcessMode);

                // Execute processor
                while (true) {
                    ct.ThrowIfCancellationRequested();
                    if (_jobProcess == null) {
                        _jobProcess = new JobProcess(this, currentProcessInstruction, _lifetimeScope, _logger);
                    }
                    else {
                        _jobProcess.ProcessNewInstruction(currentProcessInstruction);
                    }
                    await _jobProcess.WaitAsync().ConfigureAwait(false); // Does not throw
                    ct.ThrowIfCancellationRequested();

                    // Check if the job is to be continued with new configuration settings
                    if (_jobProcess?.JobContinuation?.Job?.JobConfiguration == null ||
                        _jobProcess?.JobContinuation?.ProcessMode == null) {
                        await StopJobProcess().ConfigureAwait(false);
                        break;
                    }

                    currentProcessInstruction = _jobProcess.JobContinuation;
                    _logger.Information("Worker {WorkerId} processing job {JobId} continuation in mode {ProcessMode}",
                        WorkerId, currentProcessInstruction.Job.Id, currentProcessInstruction.ProcessMode);
                }
            }
            catch (OperationCanceledException) {
                _logger.Information("Worker {WorkerId} cancellation received ...", WorkerId);
                await StopJobProcess().ConfigureAwait(false);
            }
            finally {
                _logger.Information("Worker: {WorkerId}, Job: {JobId} processing completed ... ",
                    WorkerId, currentProcessInstruction.Job.Id);
                if (!ct.IsCancellationRequested) {
                    StartSendingHeartbeat(); // restart worker heartbeat
                }
            }
        }

        /// <summary>
        /// Stops and disposes the job process
        /// </summary>
        private async Task StopJobProcess() {
            if (_jobProcess != null && _jobProcess.Status != WorkerStatus.Stopped) {
                _jobProcess.Status = WorkerStatus.Stopped;
                try {
                    _logger.Debug("Worker {workerId} sending job process stopped heartbeat...", WorkerId);

                    // Note - will take lock for status
                    var workerHeartbeat = await GetWorkerHeartbeatAsync(_cts.Token).ConfigureAwait(false);

                    await _jobManagerConnector.SendHeartbeatAsync(
                        new HeartbeatModel { Worker = workerHeartbeat },
                        GetDiagnosticInfo(),
                        _cts.Token
                    ).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Done
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Worker {workerId} could not send heartbeat.", WorkerId);
                    kModuleExceptions.WithLabels(AgentId, ex.Source, ex.GetType().FullName,
                        ex.Message, ex.StackTrace, "Could not send worker hearbeat").Inc();
                }
                _jobProcess.Dispose();
                _jobProcess = null;
            }
        }

        /// <summary>
        /// Worker job processing process
        /// </summary>
        private class JobProcess : IDisposable {

            /// <inheritdoc/>
            public JobProcessingInstructionModel JobContinuation { get; private set; }

            /// <inheritdoc/>
            public WorkerStatus Status { get; internal set; } = WorkerStatus.Stopped;

            /// <inheritdoc/>
            public JobInfoModel Job => _currentJobProcessInstruction.Job;

            /// <summary>
            /// Create processor
            /// </summary>
            public JobProcess(Worker outer, JobProcessingInstructionModel jobProcessInstruction,
                ILifetimeScope workerScope, ILogger logger) {
                _outer = outer;
                _exitHeartbeat = true;
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
                _processor = Task.Factory.StartNew(() => ProcessAsync(_cancellationTokenSource.Token),
                    TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
            }

            /// <summary>
            /// Reconfigure the existing process
            /// </summary>
            public void ProcessNewInstruction(JobProcessingInstructionModel jobProcessInstruction) {
                _cancellationTokenSource.Cancel();
                _processor.Wait();
                _cancellationTokenSource.Dispose();

                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    _outer._cts.Token);
                _currentJobProcessInstruction = jobProcessInstruction;
                var jobConfig = _outer._jobConfigurationFactory.DeserializeJobConfiguration(
                    Job.JobConfiguration, Job.JobConfigurationType);

                _currentProcessingEngine.ReconfigureTrigger(jobConfig);
                JobContinuation = null;
                _processor = Task.Factory.StartNew(() => ProcessAsync(_cancellationTokenSource.Token),
                    TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
            }

            /// <summary>
            /// Trigger a heartbeat request immediately
            /// </summary>
            public void ResetHeartbeat() {
                if (!_exitHeartbeat) {
                    _resetTimer.Cancel();
                    _resetTimer = new CancellationTokenSource();
                }
            }

            /// <summary>
            /// Get Diagnostic Info
            /// </summary>
            /// <returns></returns>
            public JobDiagnosticInfoModel GetProcessDiagnosticInfo() {
                return _currentProcessingEngine.GetDiagnosticInfo();
            }

            /// <summary>
            /// Wait till completion or heartbeat cancelling
            /// </summary>
            public Task WaitAsync() {
                return _processor;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _jobScope.Dispose();
                _cancellationTokenSource.Dispose();
            }

            private void StartSendingHeartBeat() {
                System.Diagnostics.Debug.Assert(_exitHeartbeat);
                System.Diagnostics.Debug.Assert(_heartbeatTimerTask == null);
                _exitHeartbeat = false;
                _resetTimer = new CancellationTokenSource();
                _heartbeatTimerTask = Task.Factory.StartNew(() => SendHeartbeatsAsync(),
                    TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.LongRunning);
            }

            private async Task StopSendingHeartbeatAsync() {
                System.Diagnostics.Debug.Assert(!_exitHeartbeat);
                System.Diagnostics.Debug.Assert(_heartbeatTimerTask != null);
                _exitHeartbeat = true;
                _resetTimer.Cancel(); // is disposed in timer task
                await _heartbeatTimerTask.ConfigureAwait(false);
                _heartbeatTimerTask = null;
            }

            /// <summary>
            /// Processor
            /// </summary>
            private async Task ProcessAsync(CancellationToken ct) {
                try {
                    Status = WorkerStatus.ProcessingJob;
                    _outer.OnJobStarted?.Invoke(this, new JobInfoEventArgs(Job));
                    _logger.Information("Job {job} started.", Job.Id);

                    // Start sending heartbeats
                    StartSendingHeartBeat();

                    await _currentProcessingEngine.RunAsync(_currentJobProcessInstruction.ProcessMode.Value,
                        ct).ConfigureAwait(false);

                    ct.ThrowIfCancellationRequested();
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
                    await StopSendingHeartbeatAsync().ConfigureAwait(false);
                    await CleanupAsync().ConfigureAwait(false);
                    _logger.Information("Job {job} completed.", Job.Id);
                }
            }


            /// <summary>
            /// Cleanup job processing
            /// </summary>
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
                        await SendHeartbeatAsync(CancellationToken.None).ConfigureAwait(false);
                        _outer.OnJobCanceled?.Invoke(this, new JobInfoEventArgs(Job));
                    }
                    else {
                        _logger.Debug("Update completion status for {job}.", Job.Id);
                        if (Job.LifetimeData.Status != JobStatus.Error) {
                            Job.LifetimeData.Status = JobStatus.Completed;
                        }
                        await SendHeartbeatAsync(CancellationToken.None).ConfigureAwait(false);
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
            private async Task SendHeartbeatAsync(CancellationToken ct = default) {
                _logger.Debug("Sending job processor heartbeat...");
                var workerHeartbeat = await _outer.GetWorkerHeartbeatAsync(ct).ConfigureAwait(false);
                var result = await _outer._jobManagerConnector
                    .SendHeartbeatAsync(
                        new HeartbeatModel {
                            Worker = workerHeartbeat,
                            Job = new JobHeartbeatModel {
                                JobId = Job.Id,
                                JobHash = Job.GetHashSafe(),
                                Status = Job.LifetimeData.Status,
                                ProcessMode = _currentJobProcessInstruction.ProcessMode.Value,
                                State = await _currentProcessingEngine.GetCurrentJobState().ConfigureAwait(false)
                            }
                        }, GetProcessDiagnosticInfo(), ct)
                    .ConfigureAwait(false);

                // Process instructions
                switch (result.HeartbeatInstruction) {
                    case HeartbeatInstruction.SwitchToActive:
                        await _currentProcessingEngine
                            .SwitchProcessMode(ProcessMode.Active, result.LastActiveHeartbeat)
                            .ConfigureAwait(false);
                        break;
                    case HeartbeatInstruction.CancelProcessing:
                        if (result.UpdatedJob != null && JobContinuation == null) {
                            JobContinuation = result.UpdatedJob;
                        }
                        if (!_cancellationTokenSource.IsCancellationRequested) {
                            _logger.Debug("Received job cancellation, cancelling processing...");
                            _cancellationTokenSource.Cancel();
                        }
                        break;
                    case HeartbeatInstruction.Update:
                    case HeartbeatInstruction.Keep:
                        if (result.UpdatedJob != null && JobContinuation == null) {
                            JobContinuation = result.UpdatedJob;
                            // Cancel
                            if (!_cancellationTokenSource.IsCancellationRequested) {
                                _logger.Debug("Received job update request - continue ...");
                                _cancellationTokenSource.Cancel();
                            }
                        }
                        break;
                }
            }

            /// <summary>
            /// Heartbeat timer loop
            /// </summary>
            private async Task SendHeartbeatsAsync() {
                var timerInterval = TimeSpan.MinValue;
                PeriodicTimer timer = null;
                try {
                    while (!_exitHeartbeat) {
                        if (timerInterval != _outer._heartbeatInterval) {
                            timerInterval = _outer._heartbeatInterval;
                            timer?.Dispose();
                            timer = new PeriodicTimer(timerInterval);
                        }

                        var resetTimerToken = _resetTimer;
                        try {
                            // Send heartbeat
                            await SendHeartbeatAsync(resetTimerToken.Token).ConfigureAwait(false);

                            // Wait for timer or exit
                            if (!await timer.WaitForNextTickAsync(resetTimerToken.Token).ConfigureAwait(false)) {
                                System.Diagnostics.Debug.Assert(false, "Unexpected disposal of timer."); 
                                return;
                            }
                        }
                        catch (OperationCanceledException) {
                            if (resetTimerToken.IsCancellationRequested) {
                                resetTimerToken.Dispose();
                            }
                        }
                        catch (ResourceNotFoundException) {
                            if (!_cancellationTokenSource.IsCancellationRequested) {
                                _logger.Debug("Heartbeat returned job not found - cancelling ...");
                                _cancellationTokenSource.Cancel();
                            }
                        }
                        catch (Exception ex) when (ex is ITransientException) {
                            _logger.Debug("Heartbeat endpoint busy...");
                        }
                        catch (Exception ex) {
                            _logger.Error(ex, "Could not send worker heartbeat.");
                        }
                    }
                }
                finally {
                    timer?.Dispose();
                }
            }

            private bool _exitHeartbeat;
            private CancellationTokenSource _resetTimer;
            private Task _heartbeatTimerTask;
            private readonly IProcessingEngine _currentProcessingEngine;
            private readonly ILifetimeScope _jobScope;
            private readonly Worker _outer;
            private readonly ILogger _logger;
            private Task _processor;
            private CancellationTokenSource _cancellationTokenSource;
            private JobProcessingInstructionModel _currentJobProcessInstruction;
        }

        /// <summary>
        /// Get worker heartbeat
        /// </summary>
        private async Task<WorkerHeartbeatModel> GetWorkerHeartbeatAsync(CancellationToken ct) {
            var workerHeartbeat = new WorkerHeartbeatModel {
                WorkerId = WorkerId,
                AgentId = AgentId,
                Status = GetStatus()
            };
            if (_agentRepository != null) {
                await _agentRepository.AddOrUpdate(workerHeartbeat, ct).ConfigureAwait(false);
            }
            return workerHeartbeat;
        }

        /// <summary>
        /// Get status under lock
        /// </summary>
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

        private bool _exitHeartbeat;
        private CancellationTokenSource _resetTimer;
        private Task _heartbeatTimerTask;

        private readonly int _workerInstance;
        private readonly IAgentConfigProvider _agentConfigProvider;
        private readonly IWorkerRepository _agentRepository;
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
        private TaskCompletionSource<bool> _reset;
        private static readonly Counter kModuleExceptions = Metrics.CreateCounter("iiot_edge_publisher_exceptions", "module exceptions",
            new CounterConfiguration {
                LabelNames = new[] { "agent", "source", "type", "message", "stacktrace", "custom_message" }
            });
    }
}
