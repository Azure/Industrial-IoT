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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Default worker supervisor = agent.
    /// </summary>
    public class WorkerSupervisor : IWorkerSupervisor, IDisposable {

        /// <summary>
        /// Create supervisor
        /// </summary>
        /// <param name="lifetimeScope"></param>
        /// <param name="agentConfigProvider"></param>
        /// <param name="logger"></param>
        public WorkerSupervisor(ILifetimeScope lifetimeScope,
            IAgentConfigProvider agentConfigProvider, ILogger logger) {
            _lifetimeScope = lifetimeScope;
            _agentConfigProvider = agentConfigProvider;
            _logger = logger;
            _ensureWorkerRunningTimer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            _ensureWorkerRunningTimer.Elapsed += EnsureWorkerRunningTimer_ElapsedAsync;
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await EnsureWorkersAsync();
            _ensureWorkerRunningTimer.Start();
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            _logger.Information("Stopping worker supervisor");
            var stopTasks = new List<Task>();
            _ensureWorkerRunningTimer.Stop();

            foreach (var instance in _instances.ToList()) {
                stopTasks.Add(instance.Key.StopAsync());
            }

            await Task.WhenAll(stopTasks);
            _logger.Information("Worker supervisor successfully stopped");
        }

        /// <inheritdoc/>
        public Task<IWorker> CreateWorker() {
            var maxWorkers = _agentConfigProvider.Config?.MaxWorkers ?? kDefaultWorkers;
            if (_instances.Count >= maxWorkers) {
                throw new MaxWorkersReachedException(maxWorkers);
            }

            var childScope = _lifetimeScope.BeginLifetimeScope();
            var worker = childScope.Resolve<IWorker>(new NamedParameter("workerInstance", _instances.Count));
            _instances[worker] = childScope;
            return Task.FromResult(worker);
        }

        /// <inheritdoc/>
        public async Task StopWorker(string workerId) {
            if (!_instances.Keys.Any(w => w.WorkerId == workerId)) {
                throw new WorkerNotFoundException(workerId);
            }

            var worker = _instances.Where(w => w.Key.WorkerId == workerId).Single();

            await worker.Key.StopAsync();
            worker.Value.Dispose();
            _instances.Remove(worker.Key);
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Async(StopAsync).Wait();
            _ensureWorkerRunningTimer?.Dispose();
        }

        /// <summary>
        /// Monitoring timer elapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnsureWorkerRunningTimer_ElapsedAsync(object sender, ElapsedEventArgs e) {
            try {
                _ensureWorkerRunningTimer.Enabled = false;
                await EnsureWorkersAsync();
            }
            finally {
                _ensureWorkerRunningTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Monitor workers
        /// </summary>
        /// <returns></returns>
        private async Task EnsureWorkersAsync() {
            var workerStartTasks = new List<Task>();

            while (true) {
                var workers = _agentConfigProvider.Config?.MaxWorkers ?? kDefaultWorkers;
                while (_instances.Count < workers) {
                    _logger.Information("Creating new worker...");
                    var worker = await CreateWorker();
                }

                foreach (var stoppedWorker in _instances.Keys.Where(s => s.Status == WorkerStatus.Stopped)) {
                    _logger.Information("Starting worker '{workerId}'...", stoppedWorker.WorkerId);
                    workerStartTasks.Add(stoppedWorker.StartAsync());
                }
                await Task.WhenAll(workerStartTasks);
                // the configuration might have been changed by workers execution
                var newWorkers = _agentConfigProvider.Config?.MaxWorkers ?? kDefaultWorkers;
                if (workers >= newWorkers) {
                    break;
                }
            }
        }

        private const int kDefaultWorkers = 5; // TODO - single listener, dynamic workers.
        private readonly IAgentConfigProvider _agentConfigProvider;
        private readonly Timer _ensureWorkerRunningTimer;
        private readonly Dictionary<IWorker, ILifetimeScope> _instances = new Dictionary<IWorker, ILifetimeScope>();
        private readonly ILifetimeScope _lifetimeScope;
        private readonly ILogger _logger;
    }
}