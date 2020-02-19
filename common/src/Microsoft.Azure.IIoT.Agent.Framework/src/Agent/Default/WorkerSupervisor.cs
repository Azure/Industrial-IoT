// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;

    /// <summary>
    /// Default worker supervisor = agent.
    /// </summary>
    public class WorkerSupervisor : IWorkerSupervisor, IDisposable {
        /// <summary>
        /// Create supervisor
        /// </summary>
        /// <param name="lifetimeScope"></param>
        /// <param name="agentConfigProvider"></param>
        /// <param name="jobOrchestrator"></param>
        /// <param name="jobHeartbeatCollection"></param>
        /// <param name="logger"></param>
        public WorkerSupervisor(ILifetimeScope lifetimeScope,
            IAgentConfigProvider agentConfigProvider, IJobOrchestrator jobOrchestrator, IJobHeartbeatCollection jobHeartbeatCollection, ILogger logger) {
            _jobOrchestrator = jobOrchestrator;
            _lifetimeScope = lifetimeScope;
            _agentConfigProvider = agentConfigProvider;
            _jobHeartbeatCollection = jobHeartbeatCollection;
            _logger = logger;

            _heartbeatInterval = _agentConfigProvider.GetHeartbeatInterval();
            _jobCheckerInterval = _agentConfigProvider.GetJobCheckInterval();

            _agentConfigProvider.OnConfigUpdated += (s, e) => {
                _heartbeatInterval = _agentConfigProvider.GetHeartbeatInterval();
                _jobCheckerInterval = _agentConfigProvider.GetJobCheckInterval();
            };

            _heartbeatTimer = new Timer(_ => HeartbeatTimer_ElapsedAsync().Wait());
        }

        /// <inheritdoc />
        public string AgentId => _agentConfigProvider.Config.AgentId ?? "Agent";

        /// <summary>
        /// Heartbeat timer
        /// </summary>
        private async Task HeartbeatTimer_ElapsedAsync() {
            await SendHeartbeatAsync();
            Try.Op(() => _heartbeatTimer.Change(_heartbeatInterval, Timeout.InfiniteTimeSpan));
        }

        private Task<HeartbeatModel> GetCurrentHeartbeat() {
            var supervisorHeartbeat = new SupervisorHeartbeatModel {SupervisorId = AgentId, Status = SupervisorStatus.Running};

            var workerHeartbeats = _jobHeartbeatCollection.Heartbeats.Values.ToArray();

            return Task.FromResult(new HeartbeatModel {SupervisorHeartbeat = supervisorHeartbeat, JobHeartbeats = workerHeartbeats});
        }

        private async Task SendHeartbeatAsync() {
            try {
                _logger.Debug("Sending heartbeat...");

                // Note - will take lock for status
                var heartbeat = await GetCurrentHeartbeat();

                var result = await _jobOrchestrator.SendHeartbeatAsync(heartbeat);

                foreach (var entry in result) {
                    if (_runningWorkers.TryGetValue(entry.JobId, out var worker)) {
                        await worker.ProcessHeartbeatResult(entry);
                    }
                    else {
                        await _jobHeartbeatCollection.Remove(entry.JobId);
                    }
                }
            }
            catch (OperationCanceledException) {
            }
            catch (Exception ex) {
                _logger.Error(ex, "Could not send worker heartbeat.");
            }
        }

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken ct) {
            _logger.Debug("WorkerSupervisor starting...");
            _heartbeatTimer.Change(0, int.MaxValue);

            while (!ct.IsCancellationRequested) {
                try {
                    ct.ThrowIfCancellationRequested();

                    _logger.Debug("Try querying available job...");

                    var availableWorkerCount = (_agentConfigProvider.Config.MaxWorkers ?? 1) - _runningWorkers.Count;

                    if (availableWorkerCount < 1) {
                        await Task.Delay(_jobCheckerInterval, ct);
                        continue;
                    }

                    var jobProcessInstructions = await Try.Async(() =>
                        _jobOrchestrator.GetAvailableJobsAsync(AgentId, new JobRequestModel {Capabilities = _agentConfigProvider.Config.Capabilities, MaxJobCount = availableWorkerCount}, ct));

                    ct.ThrowIfCancellationRequested();

                    if (!jobProcessInstructions.Any()) {
                        _logger.Information("No job received, wait {delay} ...",
                            _jobCheckerInterval);
                        await Task.Delay(_jobCheckerInterval, ct);
                        continue;
                    }

                    await StartJobProcessing(jobProcessInstructions, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Information("WorkerSupervisor cancelled...");
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Exception during worker supervisor execution.  Continue...");
                }
            }

            _logger.Information("Waiting for all worker to stop...");

            while (_runningWorkers.Any()) {
                await Task.Delay(1000);
            }

            _logger.Information("WorkerSupervisor stopped.");
        }

        private Task StartJobProcessing(IEnumerable<JobProcessingInstructionModel> jobProcessingInstructionModels, CancellationToken ct) {
            foreach (var jobProcessingInstructionModel in jobProcessingInstructionModels) {
                var workerScope = _lifetimeScope.BeginLifetimeScope(c => {
                    c.RegisterInstance(jobProcessingInstructionModel);
                });
                var worker = workerScope.Resolve<IWorker>();

                _runningWorkers[worker.Job.Id] = worker;

                worker.ProcessAsync(ct).ContinueWith(c => {
                    _runningWorkers.TryRemove(worker.Job.Id, out var removed);
                    _jobHeartbeatCollection.Remove(worker.Job.Id).Wait();
                    workerScope.Dispose();
                });
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() {
            Try.Async(StopAsync).Wait();
        }

        /// <inheritdoc />
        public Task StartAsync() {
            Task.Run(() => RunAsync(_cts.Token));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync() {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private const int kDefaultWorkers = 5; // TODO - single listener, dynamic workers.

        private readonly IAgentConfigProvider _agentConfigProvider;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Timer _heartbeatTimer;
        private readonly IJobHeartbeatCollection _jobHeartbeatCollection;
        private readonly IJobOrchestrator _jobOrchestrator;
        private readonly ILifetimeScope _lifetimeScope;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IWorker> _runningWorkers = new ConcurrentDictionary<string, IWorker>();
        private TimeSpan _heartbeatInterval;
        private TimeSpan _jobCheckerInterval;
    }
}