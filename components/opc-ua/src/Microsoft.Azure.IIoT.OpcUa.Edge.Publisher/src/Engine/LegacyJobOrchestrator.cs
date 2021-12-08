// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job orchestrator the represents the legacy publishednodes.json with legacy command line arguments as job.
    /// </summary>
    public class LegacyJobOrchestrator : IJobOrchestrator, IPublisherConfigServices {
        /// <summary>
        /// Creates a new class of the LegacyJobOrchestrator.
        /// </summary>
        /// <param name="publishedNodesJobConverter">The converter to read the job from the specified file.</param>
        /// <param name="legacyCliModelProvider">The provider that provides the legacy command line arguments.</param>
        /// <param name="agentConfigProvider">The provider that provides the agent configuration.</param>
        /// <param name="jobSerializer">The serializer to (de)serialize job information.</param>
        /// <param name="logger">Logger to write log messages.</param>
        /// <param name="identity">Module's identity provider.</param>

        public LegacyJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter,
            ILegacyCliModelProvider legacyCliModelProvider, IAgentConfigProvider agentConfigProvider,
            IJobSerializer jobSerializer, ILogger logger, IIdentity identity) {
            _publishedNodesJobConverter = publishedNodesJobConverter
                ?? throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel
                    ?? throw new ArgumentNullException(nameof(legacyCliModelProvider));
            _agentConfig = agentConfigProvider
                    ?? throw new ArgumentNullException(nameof(agentConfigProvider));

            _jobSerializer = jobSerializer ?? throw new ArgumentNullException(nameof(jobSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));

            var directory = Path.GetDirectoryName(_legacyCliModel.PublishedNodesFile);

            if (string.IsNullOrWhiteSpace(directory)) {
                directory = Environment.CurrentDirectory;
            }

            _availableJobs = new ConcurrentQueue<JobProcessingInstructionModel>();
            _assignedJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();

            _lock = new SemaphoreSlim(1, 1);

            RefreshJobFromFile();

            var file = Path.GetFileName(_legacyCliModel.PublishedNodesFile);
            _fileSystemWatcher = new FileSystemWatcher(directory, file);
            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.Created += _fileSystemWatcher_Created;
            _fileSystemWatcher.Renamed += _fileSystemWatcher_Renamed;
            _fileSystemWatcher.Deleted += _fileSystemWatcher_Deleted;
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
        public async Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId, JobRequestModel request, CancellationToken ct = default) {
            await _lock.WaitAsync(ct);
            try {
                ct.ThrowIfCancellationRequested();
                if (_assignedJobs.TryGetValue(workerId, out var job)) {
                    return job;
                }
                if (!_availableJobs.IsEmpty && _availableJobs.TryDequeue(out job)) {
                    _assignedJobs.AddOrUpdate(workerId, job);
                }

                return job;
            }
            catch (OperationCanceledException) {
                _logger.Information("Operation GetAvailableJobAsync was canceled");
                throw;
            }
            catch (Exception e) {
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
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat, CancellationToken ct = default) {
            await _lock.WaitAsync();
            try {
                ct.ThrowIfCancellationRequested();
                HeartbeatResultModel heartbeatResultModel;
                JobProcessingInstructionModel job = null;
                if (heartbeat.Job != null) {
                    if (_assignedJobs.TryGetValue(heartbeat.Worker.WorkerId, out job)) {
                        if (job.Job.GetHashSafe() == heartbeat.Job.JobHash) {
                            // JobProcess should keep working
                            heartbeatResultModel = new HeartbeatResultModel {
                                HeartbeatInstruction = HeartbeatInstruction.Keep,
                                LastActiveHeartbeat = DateTime.UtcNow,
                                UpdatedJob = null,
                            };
                        }
                        else {
                            if (job.Job.Id == heartbeat.Job.JobId) {
                                // JobProcess have to finished current and process new job (if job != null) otherwise complete
                                //  TODO: since just the content of the datasets is changed, just trigger a job update
                                heartbeatResultModel = new HeartbeatResultModel {
                                    HeartbeatInstruction = HeartbeatInstruction.CancelProcessing,
                                    LastActiveHeartbeat = DateTime.UtcNow,
                                    UpdatedJob = job,
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
                    }
                    else {
                        heartbeatResultModel = new HeartbeatResultModel {
                            HeartbeatInstruction = HeartbeatInstruction.CancelProcessing,
                            LastActiveHeartbeat = DateTime.UtcNow,
                            UpdatedJob = null,
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

                return heartbeatResultModel;
            }
            catch (OperationCanceledException) {
                _logger.Information("Operation SendHeartbeatAsync was canceled");
                throw;
            }
            catch (Exception e) {
                _logger.Error(e, "Exception on heartbeat for worker {worker} with job {jobId}",
                    heartbeat?.Worker?.WorkerId,
                    heartbeat?.Job?.JobId);
                throw;
            }
            finally {
                _lock.Release();
            }
        }

        private void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} changed. Triggering file refresh ...", _legacyCliModel.PublishedNodesFile);
            RefreshJobFromFile();
        }

        private void _fileSystemWatcher_Created(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} created. Triggering file refresh ...", _legacyCliModel.PublishedNodesFile);
            RefreshJobFromFile();
        }

        private void _fileSystemWatcher_Renamed(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} renamed. Triggering file refresh ...", _legacyCliModel.PublishedNodesFile);
            RefreshJobFromFile();
        }

        private void _fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} deleted. Clearing configuration ...", _legacyCliModel.PublishedNodesFile);
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

        private static string GetChecksum(string content) {
            if (String.IsNullOrEmpty(content)) {
                return null;
            }
            var sha = new SHA256Managed();
            var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(checksum).Replace("-", string.Empty);
        }

        private void RefreshJobFromFile() {
            var retryCount = 3;
            var lastWriteTime = File.GetLastWriteTime(_legacyCliModel.PublishedNodesFile);
            while (true) {
                _lock.Wait();
                try {

                    var availableJobs = new ConcurrentQueue<JobProcessingInstructionModel>();
                    var assignedJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();

                    using (var fileStream = new FileStream(_legacyCliModel.PublishedNodesFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        var content = fileStream.ReadAsString(Encoding.UTF8);
                        var lastValidFileHash = _lastKnownFileHash;
                        var currentFileHash = GetChecksum(content);
                        if (currentFileHash != _lastKnownFileHash) {

                            _logger.Information("File {publishedNodesFile} has changed, last known hash {LastHash}, new hash {NewHash}, reloading...",
                                _legacyCliModel.PublishedNodesFile,
                                _lastKnownFileHash,
                                currentFileHash);
                            _lastKnownFileHash = currentFileHash;
                            if (!string.IsNullOrEmpty(content)) {
                                IEnumerable<WriterGroupJobModel> jobs = null;
                                try {
                                    if (!File.Exists(_legacyCliModel.PublishedNodesSchemaFile)) {
                                        _logger.Information("Validation schema file {PublishedNodesSchemaFile} does not exist or is disabled, ignoring validation of {publishedNodesFile} file...",
                                        _legacyCliModel.PublishedNodesSchemaFile, _legacyCliModel.PublishedNodesFile);
                                        jobs = _publishedNodesJobConverter.Read(content, null, _legacyCliModel);
                                    }
                                    else {
                                        using (var fileSchemaReader = new StreamReader(_legacyCliModel.PublishedNodesSchemaFile)) {
                                            jobs = _publishedNodesJobConverter.Read(content, fileSchemaReader, _legacyCliModel);
                                        }
                                    }
                                }
                                catch (IOException) {
                                    throw; //pass it thru, to handle retries
                                }
                                catch (SerializerException ex) {
                                    _logger.Warning(ex, "Failed to deserialize {publishedNodesFile}, aborting reload...", _legacyCliModel.PublishedNodesFile);
                                    _lastKnownFileHash = lastValidFileHash;
                                    return;
                                }

                                foreach (var job in jobs) {
                                    var newJob = ToJobProcessingInstructionModel(job);
                                    var found = false;
                                    foreach (var assignedJob in _assignedJobs) {
                                        if (newJob?.Job?.Id != null && assignedJob.Value?.Job?.Id != null &&
                                            newJob.Job.Id == assignedJob.Value.Job.Id) {
                                            assignedJobs[assignedJob.Key] = newJob;
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found) {
                                        availableJobs.Enqueue(newJob);
                                    }
                                }
                            }
                            if (_agentConfig.Config.MaxWorkers < availableJobs.Count + assignedJobs.Count) {
                                _agentConfig.Config.MaxWorkers = availableJobs.Count + assignedJobs.Count;
                            }

                            _availableJobs = availableJobs;
                            _assignedJobs = assignedJobs;

                            _agentConfig.TriggerConfigUpdate(this, new EventArgs());

                        }
                        else {
                            //avoid double events from FileSystemWatcher
                            if (lastWriteTime - _lastRead > TimeSpan.FromMilliseconds(10)) {
                                _logger.Information("File {publishedNodesFile} has changed and content-hash is equal to last one, nothing to do", _legacyCliModel.PublishedNodesFile);
                            }
                        }
                        _lastRead = lastWriteTime;
                        break;
                    }
                }
                catch (IOException ex) {
                    retryCount--;
                    if (retryCount > 0) {
                        _logger.Warning(ex, "Error while loading job from file. Retrying...");
                        Task.Delay(500).GetAwaiter().GetResult();
                    }
                    else {
                        _logger.Error(ex, "Error while loading job from file. Retry expired, giving up.");
                        break;
                    }
                }
                catch (SerializerException sx) {
                    _logger.Error(sx, "SerializerException while loading job from file.");
                    break;
                }
                catch (Exception e) {
                    _logger.Error(e, "Error while reloading {PublishedNodesFile}", _legacyCliModel.PublishedNodesFile);
                    _availableJobs.Clear();
                    _assignedJobs.Clear();
                    _lastKnownFileHash = string.Empty;
                }
                finally {
                    _lock.Release();
                }
            }
        }

        private JobProcessingInstructionModel ToJobProcessingInstructionModel (WriterGroupJobModel job){
            var jobId = $"Standalone_{job.WriterGroup.WriterGroupId}_{job.WriterGroup.DataSetWriters.FirstOrDefault().DataSetWriterId}";

            job.WriterGroup.DataSetWriters.ForEach(d => {
                d.DataSet.ExtensionFields ??= new Dictionary<string, string>();
                d.DataSet.ExtensionFields["PublisherId"] = jobId;
                d.DataSet.ExtensionFields["DataSetWriterId"] = d.DataSetWriterId;
            });
            var endpoints = string.Join(", ", job.WriterGroup.DataSetWriters.Select(w => w.DataSet.DataSetSource.Connection.Endpoint.Url));
            _logger.Information($"Job {jobId} loaded. DataSetWriters endpoints: {endpoints}");
            var serializedJob = _jobSerializer.SerializeJobConfiguration(job, out var jobConfigurationType);

            return new JobProcessingInstructionModel {
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
                };
        }

        /// <inheritdoc/>
        public async Task<List<string>> PublishNodesAsync(PublishedNodesEntryModel request) {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("PublishNodes method triggered");
                var publishJobs = _publishedNodesJobConverter.ToWriterGroupJobs(new List<PublishedNodesEntryModel> { request }, _legacyCliModel);
                if (!publishJobs.Any()) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NoContent, "Invalid or empty request");
                }

                foreach (var newJob in publishJobs) {
                    var found = false;
                    foreach (var item in _assignedJobs) {
                        var deserializedJob = _jobSerializer.DeserializeJobConfiguration(
                            item.Value.Job.JobConfiguration,
                            item.Value.Job.JobConfigurationType) as WriterGroupJobModel;
                        if (newJob.WriterGroup.DataSetWriters.First().DataSet.DataSetSource.Connection ==
                            deserializedJob.WriterGroup.DataSetWriters.First().DataSet.DataSetSource.Connection) {

                            _assignedJobs[item.Key] = ToJobProcessingInstructionModel(newJob);
                            found = true;
                            break;
                        }
                    }
                    if (found) {
                        break;
                    }
                    foreach (var item in _availableJobs) {
                        var deserializedJob = _jobSerializer.DeserializeJobConfiguration(
                            item.Job.JobConfiguration,
                            item.Job.JobConfigurationType) as WriterGroupJobModel;
                        if (newJob.WriterGroup.DataSetWriters.First().DataSet.DataSetSource.Connection ==
                            deserializedJob.WriterGroup.DataSetWriters.First().DataSet.DataSetSource.Connection) {

                            // Update Job content
                            found = true;
                            break;
                        }
                    }
                    if (found) {
                        break;
                    }

                    _availableJobs.Enqueue(ToJobProcessingInstructionModel(newJob));
                }
            }
            finally {
                _lock.Release();
            }

            return new List<string> { "Succeded" };
        }

        /// <inheritdoc/>
        public async Task<List<string>> UnpublishNodesAsync(PublishedNodesEntryModel request) {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("UnpublishNodes method triggered");
                var unpublishJobs = _publishedNodesJobConverter.ToWriterGroupJobs(new List<PublishedNodesEntryModel> { request }, _legacyCliModel);
                return new List<string> { "NotImplemented" };
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishAllNodesAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("UnpublishAllNodes method triggered");
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task GetConfiguredEndpointsAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("GetConfiguredEndpointsAsync method triggered");
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task GetConfiguredNodesOnEndpointAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("GetConfiguredNodesOnEndpointAsync method triggered");
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly IJobSerializer _jobSerializer;
        private readonly LegacyCliModel _legacyCliModel;
        private readonly IAgentConfigProvider _agentConfig;
        private readonly IIdentity _identity;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly SemaphoreSlim _lock;
        private ConcurrentDictionary<string, JobProcessingInstructionModel> _assignedJobs;
        private ConcurrentQueue<JobProcessingInstructionModel> _availableJobs;
        private string _lastKnownFileHash;
        private DateTime _lastRead = DateTime.MinValue;
    }
}