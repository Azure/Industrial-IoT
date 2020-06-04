﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Module;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    /// <summary>
    /// Job orchestrator the represents the legacy publishednodes.json with legacy command line arguments as job.
    /// </summary>
    public class LegacyJobOrchestrator : IJobOrchestrator {
        /// <summary>
        /// Creates a new class of the LegacyJobOrchestrator.
        /// </summary>
        /// <param name="publishedNodesJobConverter">The converter to read the job from the specified file.</param>
        /// <param name="legacyCliModelProvider">The provider that provides the legacy command line arguments.</param>
        /// <param name="agentConfigPriovider">The provider that provides the agent configuration.</param>
        /// <param name="jobSerializer">The serializer to (de)serialize job information.</param>
        /// <param name="logger">Logger to write log messages.</param>
        /// <param name="identity">Module's identity provider.</param>

        public LegacyJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter,
            ILegacyCliModelProvider legacyCliModelProvider, IAgentConfigProvider agentConfigPriovider,
            IJobSerializer jobSerializer, ILogger logger, IIdentity identity) {
            _publishedNodesJobConverter = publishedNodesJobConverter
                ?? throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel
                    ?? throw new ArgumentNullException(nameof(legacyCliModelProvider));
            _agentConfig = agentConfigPriovider.Config
                    ?? throw new ArgumentNullException(nameof(agentConfigPriovider));

            _jobSerializer = jobSerializer ?? throw new ArgumentNullException(nameof(jobSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));

            var directory = Path.GetDirectoryName(_legacyCliModel.PublishedNodesFile);

            if (string.IsNullOrWhiteSpace(directory)) {
                directory = Environment.CurrentDirectory;
            }

            _availableJobs = new Queue<JobProcessingInstructionModel>();
            _assignedJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();

            var file = Path.GetFileName(_legacyCliModel.PublishedNodesFile);
            _fileSystemWatcher = new FileSystemWatcher(directory, file);
            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
            RefreshJobFromFile();
        }

        /// <summary>
        /// Gets the next available job - this will always return the job representation of the legacy publishednodes.json
        /// along with legacy command line arguments.
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId, JobRequestModel request, CancellationToken ct = default) {
            if (_assignedJobs.TryGetValue(workerId, out var job)) {
                return Task.FromResult(job);
            }
            if (_availableJobs.Count > 0 && (job = _availableJobs.Dequeue()) != null) {
                _assignedJobs.AddOrUpdate(workerId, job);
                if (_availableJobs.Count == 0) {
                    _updated = false;
                }
            }
            else {
                _updated = false;
            }

            return Task.FromResult(job);
        }

        /// <summary>
        /// Receives the heartbeat from the agent. Lifetime information is not persisted in this implementation. This method is
        /// only used if the
        /// publishednodes.json file has changed. Is that the case, the worker is informed to cancel (and restart) processing.
        /// </summary>
        /// <param name="heartbeat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat, CancellationToken ct = default) {
            HeartbeatResultModel heartbeatResultModel;

            if (_updated && heartbeat.Job != null) {
                if (_availableJobs.Count == 0) {
                    _updated = false;
                }

                heartbeatResultModel = new HeartbeatResultModel {
                    HeartbeatInstruction = HeartbeatInstruction.CancelProcessing,
                    LastActiveHeartbeat = DateTime.UtcNow,
                    UpdatedJob = _assignedJobs.TryGetValue(heartbeat.Worker.WorkerId, out var job) ? job : null
                };
            }
            else {
                heartbeatResultModel = new HeartbeatResultModel {
                    HeartbeatInstruction = HeartbeatInstruction.Keep,
                    LastActiveHeartbeat = DateTime.UtcNow,
                    UpdatedJob = null
                };
            }

            return Task.FromResult(heartbeatResultModel);
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
                    var availableJobs = new Queue<JobProcessingInstructionModel>();
                    if (currentFileHash != _lastKnownFileHash) {
                        _logger.Information("File {publishedNodesFile} has changed, reloading...", _legacyCliModel.PublishedNodesFile);
                        _lastKnownFileHash = currentFileHash;
                        using (var reader = new StreamReader(_legacyCliModel.PublishedNodesFile)) {
                            var jobs = _publishedNodesJobConverter.Read(reader, _legacyCliModel);
                            foreach (var job in jobs) {
                                var jobId = $"Standalone_{_identity.DeviceId}_{_identity.ModuleId}";
                                job.WriterGroup.DataSetWriters.ForEach(d => {
                                    d.DataSet.ExtensionFields ??= new Dictionary<string, string>();
                                    d.DataSet.ExtensionFields["PublisherId"] = jobId;
                                    d.DataSet.ExtensionFields["DataSetWriterId"] = d.DataSetWriterId;
                                });
                                var serializedJob = _jobSerializer.SerializeJobConfiguration(job, out var jobConfigurationType);
                                availableJobs.Enqueue(
                                    new JobProcessingInstructionModel {
                                        Job = new JobInfoModel {
                                            Demands = new List<DemandModel>(),
                                            Id = jobId,
                                            JobConfiguration = serializedJob,
                                            JobConfigurationType = jobConfigurationType,
                                            LifetimeData = new JobLifetimeDataModel(),
                                            Name = jobId,
                                            RedundancyConfig = new RedundancyConfigModel { DesiredActiveAgents = 1, DesiredPassiveAgents = 0 }
                                        },
                                        ProcessMode = ProcessMode.Active
                                    });
                            }
                        }
                        _agentConfig.MaxWorkers = availableJobs.Count;
                        ThreadPool.GetMinThreads(out var workerThreads, out var asyncThreads);
                        if (_agentConfig.MaxWorkers > workerThreads ||
                            _agentConfig.MaxWorkers > asyncThreads) {
                            var result = ThreadPool.SetMinThreads(_agentConfig.MaxWorkers.Value, _agentConfig.MaxWorkers.Value);
                            _logger.Information("Thread pool changed to: worker {worker}, async {async} threads {succeeded}",
                                _agentConfig.MaxWorkers.Value, _agentConfig.MaxWorkers.Value, result ? "succeeded" : "failed");
                        }
                        _availableJobs = availableJobs;
                        _assignedJobs.Clear();
                        _updated = true;
                    }
                    break;
                }
                catch (IOException ex) {
                    retryCount--;
                    if (retryCount > 0) {
                        _logger.Debug("Error while loading job from file, retrying...");
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
        private readonly AgentConfigModel _agentConfig;
        private readonly IIdentity _identity;
        private readonly ILogger _logger;

        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private Queue<JobProcessingInstructionModel> _availableJobs;
        private readonly ConcurrentDictionary<string, JobProcessingInstructionModel> _assignedJobs;
        private string _lastKnownFileHash;
        private bool _updated;
    }
}