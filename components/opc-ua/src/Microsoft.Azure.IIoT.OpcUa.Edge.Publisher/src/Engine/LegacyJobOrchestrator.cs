// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Serilog;

    /// <summary>
    /// Job orchestrator the represents the legacy publishednodes.json with legacy command line arguments as job.
    /// </summary>
    public class LegacyJobOrchestrator : IJobOrchestrator {
        /// <summary>
        /// Creates a new class of the LegacyJobOrchestrator.
        /// </summary>
        /// <param name="publishedNodesJobConverter">The converter to read the job from the specified file.</param>
        /// <param name="legacyCliModelProvider">The provider that provides the legacy command line arguments.</param>
        /// <param name="jobSerializer">The serializer to (de)serialize job information.</param>
        /// <param name="logger">Logger to write log messages.</param>
        /// <param name="identity">Module's identity provider.</param>

        public LegacyJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter, 
            ILegacyCliModelProvider legacyCliModelProvider, IJobSerializer jobSerializer, 
            ILogger logger, IIdentity identity) {
            _publishedNodesJobConverter = publishedNodesJobConverter
                ?? throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel 
                    ?? throw new ArgumentNullException(nameof(legacyCliModelProvider));
            _jobSerializer = jobSerializer ?? throw new ArgumentNullException(nameof(jobSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));

            var directory = Path.GetDirectoryName(_legacyCliModel.PublishedNodesFile);

            if (string.IsNullOrWhiteSpace(directory)) {
                directory = Environment.CurrentDirectory;
            }

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
            _updated = false;
            return Task.FromResult(_jobProcessingInstructionModel);
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
                _updated = false;

                heartbeatResultModel = new HeartbeatResultModel {HeartbeatInstruction = HeartbeatInstruction.CancelProcessing, LastActiveHeartbeat = DateTime.UtcNow, UpdatedJob = _jobProcessingInstructionModel};
            }
            else {
                heartbeatResultModel = new HeartbeatResultModel {HeartbeatInstruction = HeartbeatInstruction.Keep, LastActiveHeartbeat = DateTime.UtcNow, UpdatedJob = null};
            }

            return Task.FromResult(heartbeatResultModel);
        }

        private WriterGroupJobModel Flatten(IEnumerable<WriterGroupJobModel> writerGroupJobModels) {
            if (writerGroupJobModels.Count() == 1) {
                return writerGroupJobModels.Single();
            }

            // we use the first item in as template and add the DataSet writers of the subsequent jobs
            var writerGroupTemplate = writerGroupJobModels.First().WriterGroup.Clone();
            writerGroupTemplate.DataSetWriters = writerGroupJobModels.SelectMany(s => s.WriterGroup.DataSetWriters).ToList();

            var mergedModel = new WriterGroupJobModel {ConnectionString = null, Engine = writerGroupJobModels.First().Engine, MessagingMode = writerGroupJobModels.First().MessagingMode, WriterGroup = writerGroupTemplate};

            return mergedModel;
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

                    if (currentFileHash != _lastKnownFileHash) {
                        _logger.Information("File {publishedNodesFile} has changed, reloading...", _legacyCliModel.PublishedNodesFile);
                        _lastKnownFileHash = currentFileHash;

                        using (var reader = new StreamReader(_legacyCliModel.PublishedNodesFile)) {
                            var jobs = _publishedNodesJobConverter.Read(reader, _legacyCliModel);
                            var flattened = Flatten(jobs);

                            var serializedJob = _jobSerializer.SerializeJobConfiguration(flattened, out var jobConfigurationType);

                            _jobProcessingInstructionModel = new JobProcessingInstructionModel {
                                Job = new JobInfoModel {
                                    Demands = new List<DemandModel>(),
                                    Id = "LegacyJob" + "_" + _identity.DeviceId +"_" +_identity.ModuleId,
                                    JobConfiguration = serializedJob,
                                    JobConfigurationType = jobConfigurationType,
                                    LifetimeData = new JobLifetimeDataModel(),
                                    Name = "LegacyJob" + "_" + _identity.DeviceId + "_" + _identity.ModuleId,
                                    RedundancyConfig = new RedundancyConfigModel {DesiredActiveAgents = 1, DesiredPassiveAgents = 0}
                                },
                                ProcessMode = ProcessMode.Active
                            };

                            _updated = true;
                        }
                    }

                    break;
                }
                catch (IOException ex) {
                    retryCount--;

                    if (retryCount > 0) {
                        _logger.Error("Error while loading job from file, retrying...");
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
        private readonly IIdentity _identity;
        private readonly ILogger _logger;

        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private JobProcessingInstructionModel _jobProcessingInstructionModel;
        private string _lastKnownFileHash;
        private bool _updated;
    }
}