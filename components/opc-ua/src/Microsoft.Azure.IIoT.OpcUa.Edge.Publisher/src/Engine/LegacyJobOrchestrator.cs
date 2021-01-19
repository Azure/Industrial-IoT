// ------------------------------------------------------------
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
    using System.Linq;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Exceptions;

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

            _lock = new SemaphoreSlim(1, 1);

            RefreshJobFromFile();

            var file = Path.GetFileName(_legacyCliModel.PublishedNodesFile);
            _fileSystemWatcher = new FileSystemWatcher(directory, file);
            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.Created += _fileSystemWatcher_Changed;
            _fileSystemWatcher.Renamed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.Deleted += _fileSystemWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
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
            _lock.Wait(ct);
            try {
                if (_assignedJobs.TryGetValue(workerId, out var job)) {
                    return Task.FromResult(job);
                }
                if (_availableJobs.Count > 0 && _availableJobs.TryDequeue(out job)) {
                    _assignedJobs.AddOrUpdate(workerId, job);
                }

                return Task.FromResult(job);
            }
            catch(OperationCanceledException) {
                _logger.Information("Operation GetAvailableJobAsync was canceled");
                throw;
            }
            catch(Exception e) {
                _logger.Error(e, "Error while looking for available jobs, for {Worker}", workerId);
                throw;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Receives the heartbeat from the LegacyJobOrchestrator, JobProcess; used to control lifetime of job (cancel, restart, keep).
        /// </summary>
        /// <param name="heartbeat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat, CancellationToken ct = default) {
            _lock.Wait(ct);
            try {
                HeartbeatResultModel heartbeatResultModel;
                JobProcessingInstructionModel job = null;
                if (heartbeat.Job != null) {
                    if (_assignedJobs.TryGetValue(heartbeat.Worker.WorkerId, out job)
                        && job.Job.Id == heartbeat.Job.JobId) {
                        // JobProcess should keep working
                        heartbeatResultModel = new HeartbeatResultModel {
                            HeartbeatInstruction = HeartbeatInstruction.Keep,
                            LastActiveHeartbeat = DateTime.UtcNow,
                            UpdatedJob = null,
                        };
                    }
                    else {
                        // JobProcess have to finished current and process new job (if job != null) otherwise complete
                        heartbeatResultModel = new HeartbeatResultModel {
                            HeartbeatInstruction = HeartbeatInstruction.CancelProcessing,
                            LastActiveHeartbeat = DateTime.UtcNow,
                            UpdatedJob = job,
                        };
                    }
                }
                else {
                    // usecase when called from Timer of Worker instead of JobProcess
                    heartbeatResultModel = new HeartbeatResultModel {
                        HeartbeatInstruction = HeartbeatInstruction.Keep,
                        LastActiveHeartbeat = DateTime.UtcNow,
                        UpdatedJob = null,
                    };
                }
                _logger.Debug("SendHeartbeatAsync updated worker {worker} with {heartbeatInstruction} instruction for job {jobId}.",
                    heartbeat.Worker.WorkerId,
                    heartbeatResultModel?.HeartbeatInstruction,
                    job?.Job?.Id);

                return Task.FromResult(heartbeatResultModel);
            }
            catch (OperationCanceledException) {
                _logger.Information("Operation SendHeartbeatAsync was canceled");
                throw;
            }
            finally {
                _lock.Release();
            }
        }

        private void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
            if (e.ChangeType == WatcherChangeTypes.Deleted) {
                _logger.Information("Published nodes file deleted, cancelling all publishing jobs");
                _lock.Wait();
                try {
                    _availableJobs.Clear();
                    _assignedJobs.Clear();
                    _lastKnownFileHash = string.Empty;
                }
                finally {
                    _lock.Release();
                }
            }
            else {
                RefreshJobFromFile();
            }
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
                    _lock.Wait();
                    var currentFileHash = GetChecksum(_legacyCliModel.PublishedNodesFile);
                    var availableJobs = new Queue<JobProcessingInstructionModel>();
                    if (currentFileHash != _lastKnownFileHash) {
                        _logger.Information("File {publishedNodesFile} has changed, last known hash {LastHash}, new hash {NewHash}, reloading...",
                            _legacyCliModel.PublishedNodesFile,
                            _lastKnownFileHash,
                            currentFileHash);
                        _lastKnownFileHash = currentFileHash;
                        using (var reader = new StreamReader(_legacyCliModel.PublishedNodesFile)) {
                            IEnumerable<WriterGroupJobModel> jobs = null;
                            try {
                                jobs = _publishedNodesJobConverter.Read(reader, _legacyCliModel);
                            }
                            catch (IOException) {
                                throw; //pass it thru, to handle retries
                            }
                            catch (SerializerException ex) {
                                _logger.Information(ex, "Failed to deserialize {publishedNodesFile}, aborting reload...", _legacyCliModel.PublishedNodesFile);
                                _lastKnownFileHash = string.Empty;
                                return;
                            }

                            foreach (var job in jobs) {
                                var jobId = string.IsNullOrEmpty(job.WriterGroup.WriterGroupId)
                                        ? $"Standalone_{_identity.DeviceId}_{Guid.NewGuid()}"
                                        : job.WriterGroup.WriterGroupId;

                                job.WriterGroup.DataSetWriters.ForEach(d => {
                                    d.DataSet.ExtensionFields ??= new Dictionary<string, string>();
                                    d.DataSet.ExtensionFields["PublisherId"] = jobId;
                                    d.DataSet.ExtensionFields["DataSetWriterId"] = d.DataSetWriterId;
                                });
                                var endpoints = string.Join(", ", job.WriterGroup.DataSetWriters.Select(w => w.DataSet.DataSetSource.Connection.Endpoint.Url));
                                _logger.Information($"Job {jobId} loaded. DataSetWriters endpoints: {endpoints}");
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
                        if (_agentConfig.MaxWorkers < availableJobs.Count && availableJobs.Count > 1) {
                            _agentConfig.MaxWorkers = availableJobs.Count;

                            ThreadPool.GetMinThreads(out var workerThreads, out var asyncThreads);
                            if (_agentConfig.MaxWorkers > workerThreads ||
                                _agentConfig.MaxWorkers > asyncThreads) {
                                var result = ThreadPool.SetMinThreads(_agentConfig.MaxWorkers.Value, _agentConfig.MaxWorkers.Value);
                                _logger.Information("Thread pool changed to: worker {worker}, async {async} threads {succeeded}",
                                    _agentConfig.MaxWorkers.Value, _agentConfig.MaxWorkers.Value, result ? "succeeded" : "failed");
                            }
                        }
                        _availableJobs = availableJobs;
                        _assignedJobs.Clear();
                    } else {
                        _logger.Information("File {publishedNodesFile} has changed and content-hash is equal to last one, nothing to do", _legacyCliModel.PublishedNodesFile);
                    }
                    break;
                }
                catch (IOException ex) {
                    retryCount--;
                    if (retryCount > 0) {
                        Task.Delay(5000).GetAwaiter().GetResult();
                    }
                    else {
                        _logger.Error(ex, "Error while loading job from file. Retry expired, giving up.");
                        break;
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Error while reloading {PublishedNodesFile}", _legacyCliModel.PublishedNodesFile);
                    _availableJobs.Clear();
                    _assignedJobs.Clear();
                }
                finally {
                    _lock.Release();
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
        private readonly SemaphoreSlim _lock;
    }
}