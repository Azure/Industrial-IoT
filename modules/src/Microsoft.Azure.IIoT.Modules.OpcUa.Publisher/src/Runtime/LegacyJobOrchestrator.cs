using Microsoft.Azure.IIoT.Agent.Framework;
using Microsoft.Azure.IIoT.Agent.Framework.Models;
using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    public class LegacyJobOrchestrator : IJobOrchestrator {
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly LegacyCliModel _legacyCliModel;
        private readonly IJobSerializer _jobSerializer;
        private readonly ILogger _logger;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private string _lastKnownFileHash;

        private JobProcessingInstructionModel _jobProcessingInstructionModel;
        private bool _updated = false;

        public LegacyJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter, ILegacyCliModelProvider legacyCliModelProvider, IJobSerializer jobSerializer, ILogger logger) {
            _publishedNodesJobConverter = publishedNodesJobConverter;
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel;
            _jobSerializer = jobSerializer;
            _logger = logger;
            _fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(_legacyCliModel.PublishedNodesFile), Path.GetFileName(_legacyCliModel.PublishedNodesFile));
            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
            LoadJobFromFile();
        }

        private static string GetChecksum(string file) {
            using (FileStream stream = File.OpenRead(file)) {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        private void LoadJobFromFile() {
            var currentFileHash = GetChecksum(_legacyCliModel.PublishedNodesFile);

            if (currentFileHash != _lastKnownFileHash) {
                _lastKnownFileHash = currentFileHash;

                using (var reader = new StringReader(_legacyCliModel.PublishedNodesFile)) {
                    var jobs = _publishedNodesJobConverter.Read(reader, _legacyCliModel);
                    var flattened = Flatten(jobs);

                    var serializedJob = _jobSerializer.SerializeJobConfiguration(flattened, out var jobConfigurationType);

                    _jobProcessingInstructionModel = new JobProcessingInstructionModel() {
                        Job = new JobInfoModel() {
                            Demands = new List<DemandModel>(),
                            Id = "LegacyJob",
                            JobConfiguration = serializedJob,
                            JobConfigurationType = jobConfigurationType,
                            LifetimeData = new JobLifetimeDataModel(),
                            Name = "LegacyJob",
                            RedundancyConfig = new RedundancyConfigModel() { DesiredActiveAgents = 1, DesiredPassiveAgents = 0 }
                        },
                        ProcessMode = ProcessMode.Active
                    };

                    _updated = true;
                }
            }
        }

        private void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
            LoadJobFromFile();
        }

        public Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId, JobRequestModel request, CancellationToken ct = default) {
            _updated = false;
            return Task.FromResult(_jobProcessingInstructionModel);
        }

        public Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat, CancellationToken ct = default) {
            HeartbeatResultModel heartbeatResultModel;
            
            if (_updated && heartbeat.Job != null) {
                _updated = false;

                heartbeatResultModel = new HeartbeatResultModel() {
                    HeartbeatInstruction = HeartbeatInstruction.CancelProcessing,
                    LastActiveHeartbeat = DateTime.UtcNow,
                    UpdatedJob = _jobProcessingInstructionModel
                };
            }
            else {
                heartbeatResultModel = new HeartbeatResultModel() {
                    HeartbeatInstruction = HeartbeatInstruction.Keep,
                    LastActiveHeartbeat = DateTime.UtcNow,
                    UpdatedJob = null
                };
            }

            return Task.FromResult(heartbeatResultModel);
        }

        private WriterGroupJobModel Flatten(IEnumerable<WriterGroupJobModel> writerGroupJobModels) {
            throw new NotImplementedException();
        }
    }
}
