// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;

    /// <summary>
    /// Job orchestrator the represents the legacy publishednodes.json with legacy command line arguments as job.
    /// </summary>
    public class LegacyJobOrchestrator : IJobOrchestrator, IPublisherConfigServices, IDisposable {
        /// <summary>
        /// Creates a new class of the LegacyJobOrchestrator.
        /// </summary>
        /// <param name="publishedNodesJobConverter">The converter to read the job from the specified file.</param>
        /// <param name="legacyCliModelProvider">The provider that provides the legacy command line arguments.</param>
        /// <param name="agentConfigProvider">The provider that provides the agent configuration.</param>
        /// <param name="jobSerializer">The serializer to (de)serialize job information.</param>
        /// <param name="logger">Logger to write log messages.</param>
        /// <param name="publishedNodesProvider">Published nodes provider.</param>
        public LegacyJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter,
            ILegacyCliModelProvider legacyCliModelProvider, IAgentConfigProvider agentConfigProvider,
            IJobSerializer jobSerializer, ILogger logger, PublishedNodesProvider publishedNodesProvider) {
            _publishedNodesJobConverter = publishedNodesJobConverter
                ?? throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel
                    ?? throw new ArgumentNullException(nameof(legacyCliModelProvider));
            _agentConfig = agentConfigProvider
                    ?? throw new ArgumentNullException(nameof(agentConfigProvider));

            _jobSerializer = jobSerializer ?? throw new ArgumentNullException(nameof(jobSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publishedNodesProvider = publishedNodesProvider ?? throw new ArgumentNullException(nameof(publishedNodesProvider));

            _availableJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();
            _assignedJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();

            _lock = new SemaphoreSlim(1, 1);

            _publishedNodesEntrys = new List<PublishedNodesEntryModel>();

            RefreshJobFromFile();
            _publishedNodesProvider.FileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _publishedNodesProvider.FileSystemWatcher.Created += _fileSystemWatcher_Created;
            _publishedNodesProvider.FileSystemWatcher.Renamed += _fileSystemWatcher_Renamed;
            _publishedNodesProvider.FileSystemWatcher.Deleted += _fileSystemWatcher_Deleted;
            _publishedNodesProvider.FileSystemWatcher.EnableRaisingEvents = true;
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
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try {
                ct.ThrowIfCancellationRequested();
                if (_assignedJobs.TryGetValue(workerId, out var job)) {
                    return job;
                }
                if (!_availableJobs.IsEmpty && _availableJobs.TryRemove(_availableJobs.First().Key, out job)) {
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
            if (heartbeat == null || heartbeat.Worker == null) {
                throw new ArgumentNullException(nameof(heartbeat));
            }
            await _lock.WaitAsync(ct).ConfigureAwait(false);
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
                _logger.Debug("Worker update with {heartbeatInstruction} instruction for job {jobId}.",
                    heartbeatResultModel?.HeartbeatInstruction, job?.Job?.Id);

                return heartbeatResultModel;
            }
            catch (OperationCanceledException) {
                _logger.Information("Operation SendHeartbeatAsync was canceled.");
                throw;
            }
            catch (Exception e) {
                _logger.Error(e, "Exception while handling worker heartbeat.");
                throw;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_publishedNodesProvider.FileSystemWatcher != null) {
                _publishedNodesProvider.FileSystemWatcher.EnableRaisingEvents = false;
                _publishedNodesProvider.FileSystemWatcher.Changed -= _fileSystemWatcher_Changed;
                _publishedNodesProvider.FileSystemWatcher.Created -= _fileSystemWatcher_Created;
                _publishedNodesProvider.FileSystemWatcher.Renamed -= _fileSystemWatcher_Renamed;
                _publishedNodesProvider.FileSystemWatcher.Deleted -= _fileSystemWatcher_Deleted;
            }
            _lock?.Dispose();
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
            if (string.IsNullOrEmpty(content)) {
                return null;
            }
            var sha = new SHA256Managed();
            var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(checksum).Replace("-", string.Empty);
        }

        private IEnumerable<PublishedNodesEntryModel> DeserializePublishedNodes(string content) {
            if (!File.Exists(_legacyCliModel.PublishedNodesSchemaFile)) {
                _logger.Information("Validation schema file {PublishedNodesSchemaFile} does not exist or is disabled, ignoring validation of {publishedNodesFile} file...",
                _legacyCliModel.PublishedNodesSchemaFile, _legacyCliModel.PublishedNodesFile);
                return _publishedNodesJobConverter.Read(content, null);
            }
            else {
                using (var fileSchemaReader = new StreamReader(_legacyCliModel.PublishedNodesSchemaFile)) {
                    return _publishedNodesJobConverter.Read(content, fileSchemaReader);
                }
            }
        }

        private IEnumerable<PublishedNodesEntryModel> AddNodes(
            IEnumerable<PublishedNodesEntryModel> entries,
            IEnumerable<PublishedNodesEntryModel> entriesToAdd
        ) {
            var existingEntries = entries
                .Select(entry => {
                    // Pre-create ConnectionModel for this entry.
                    var connection = _publishedNodesJobConverter.ToConnectionModel(entry, _legacyCliModel);
                    // PublishedNodesJobConverter.ToConnectionModel excludes initialization of Id with DataSetWriterId.
                    // So we have to initialize that field manually since we want to include it in the comparison of ConnectionModel objects.
                    connection.Id = entry.DataSetWriterId;

                    // Pre-create HashSet of nodes for this entry.
                    var existingNodesSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                    existingNodesSet.UnionWith(entry.OpcNodes);

                    return Tuple.Create(entry, connection, existingNodesSet);
                })
                .ToList();

            foreach (var entryToAdd in entriesToAdd) {
                bool foundMatchingEndpoint = false;

                var entryToAddConnection = _publishedNodesJobConverter.ToConnectionModel(entryToAdd, _legacyCliModel);
                // PublishedNodesJobConverter.ToConnectionModel excludes initialization of Id with DataSetWriterId.
                // So we have to initialize that field manually since we want to include it in the comparison of ConnectionModel objects.
                entryToAddConnection.Id = entryToAdd.DataSetWriterId;

                foreach (var existingEntry in existingEntries) {
                    if (entryToAddConnection.IsSameAs(existingEntry.Item2)) {
                        foreach (var nodeToAdd in entryToAdd.OpcNodes) {
                            if (!existingEntry.Item3.Contains(nodeToAdd)) {
                                existingEntry.Item1.OpcNodes.Add(nodeToAdd);
                                existingEntry.Item3.Add(nodeToAdd);
                            } else {
                                _logger.Debug("Node \"{node}\" is already present for entry with \"{endpoint}\" endpoint.",
                                    nodeToAdd.Id, existingEntry.Item2.Endpoint.Url);
                            }
                        }

                        foundMatchingEndpoint = true;

                        // We should not continue search for a matching endpoint.
                        break;
                    }
                }

                if (!foundMatchingEndpoint) {
                    var entryToAddNodesSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                    entryToAddNodesSet.UnionWith(entryToAdd.OpcNodes);

                    existingEntries.Add(Tuple.Create(entryToAdd, entryToAddConnection, entryToAddNodesSet));
                }
            }

            return existingEntries.ConvertAll(complexItem => complexItem.Item1);
        }

        private void RefreshJobFromFile() {
            var retryCount = 3;
            var lastWriteTime = _publishedNodesProvider.GetLastWriteTime();
            while (true) {
                _lock.Wait();
                try {
                    var content = _publishedNodesProvider.ReadContent();
                    var lastValidFileHash = _lastKnownFileHash;
                    var currentFileHash = GetChecksum(content);
                    if (currentFileHash != _lastKnownFileHash) {
                        _logger.Information("File {publishedNodesFile} has changed, last known hash {LastHash}, new hash {NewHash}, reloading...",
                            _legacyCliModel.PublishedNodesFile,
                            _lastKnownFileHash,
                            currentFileHash);

                        _lastKnownFileHash = currentFileHash;
                        if (!string.IsNullOrEmpty(content)) {
                            IEnumerable<PublishedNodesEntryModel> entries = null;

                            try {
                                entries = DeserializePublishedNodes(content);
                            }
                            catch (IOException) {
                                throw; //pass it thru, to handle retries
                            }
                            catch (SerializerException ex) {
                                _logger.Warning(ex, "Failed to deserialize {publishedNodesFile}, aborting reload...", _legacyCliModel.PublishedNodesFile);
                                _lastKnownFileHash = lastValidFileHash;
                                break;
                            }

                            var availableJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();
                            var assignedJobs = new ConcurrentDictionary<string, JobProcessingInstructionModel>();

                            var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(entries, _legacyCliModel);
                            if (jobs.Any()) {
                                foreach (var job in jobs) {
                                    var newJob = ToJobProcessingInstructionModel(job);
                                    if (string.IsNullOrEmpty(newJob?.Job?.Id)) {
                                        continue;
                                    }
                                    var found = false;
                                    foreach (var assignedJob in _assignedJobs) {
                                        if (newJob.Job.Id == assignedJob.Value.Job.Id) {
                                            assignedJobs[assignedJob.Key] = newJob;
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found) {
                                        availableJobs.TryAdd(newJob.Job.Id, newJob);
                                    }
                                }
                            }
                            _availableJobs = availableJobs;
                            _assignedJobs = assignedJobs;

                            _publishedNodesEntrys.Clear();
                            _publishedNodesEntrys.AddRange(entries);
                        }
                        else {
                            _availableJobs.Clear();
                            _assignedJobs.Clear();
                        }

                        TriggerAgentConfigUpdate();
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
                    break;
                }
                finally {
                    _lock.Release();
                }
            }
        }

        private JobProcessingInstructionModel ToJobProcessingInstructionModel (WriterGroupJobModel job){
            if (job == null) {
                return null;
            }

            var jobId = job.GetJobId();

            job.WriterGroup.DataSetWriters.ForEach(d => {
                d.DataSet.ExtensionFields ??= new Dictionary<string, string>();
                d.DataSet.ExtensionFields["PublisherId"] = jobId;
                d.DataSet.ExtensionFields["DataSetWriterId"] = d.DataSetWriterId;
            });
            var dataSetWriters = string.Join(", ", job.WriterGroup.DataSetWriters.Select(w => w.DataSetWriterId));
            _logger.Information("Job {jobId} loaded with dataSetGroup {group} with dataSetWriters {dataSetWriters}",
                jobId, job.WriterGroup.WriterGroupId, dataSetWriters);
            var serializedJob = _jobSerializer.SerializeJobConfiguration(job, out var jobConfigurationType);

            return new JobProcessingInstructionModel {
                    Job = new JobInfoModel {
                        Demands = new List<DemandModel>(),
                        Id = jobId,
                        JobConfiguration = serializedJob,
                        JobConfigurationType = jobConfigurationType,
                        LifetimeData = new JobLifetimeDataModel(),
                        Name = jobId,
                        RedundancyConfig = new RedundancyConfigModel { DesiredActiveAgents = 1, DesiredPassiveAgents = 0 },
                    },
                    ProcessMode = ProcessMode.Active,
                };
        }

        /// <summary>
        /// Notify the worker supervisor about a configuration change
        /// </summary>
        private void TriggerAgentConfigUpdate() {
            if (_agentConfig.Config.MaxWorkers < _availableJobs.Count + _assignedJobs.Count) {
                _agentConfig.Config.MaxWorkers = _availableJobs.Count + _assignedJobs.Count;
            }
            _agentConfig.TriggerConfigUpdate(this, new EventArgs());
        }

        /// <inheritdoc/>
        public async Task<List<string>> PublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(PublishNodesAsync));
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try {
                // ToDo: Implement here merging of request and .
                // When writin to published_node.json watcher should be disabled as next parts will take care of in-memory updates.

                if (request.NodeId != null) {
                    // Let's throw an arument exception telling people to use OpcNodes instead.
                }

                // Note: do not include EncryptedAuthUsername and EncryptedAuthPassword in the comparison of PublishedNodesEntryModel objects.

                // Use new FuncCompare<ConnectionModel>((x, y) => x.IsSameAs(y)) for comparison.

                // Tentatively, this part will be updating in-memory job definitions.
                var existingGroup = new List<PublishedNodesEntryModel>();
                foreach (var entry in _publishedNodesEntrys) {
                    if (entry.HasSameGroup(request)) {
                        if (request.DataSetWriterId == entry.DataSetWriterId &&
                            request.DataSetPublishingInterval == entry.DataSetPublishingInterval) {
                            entry.OpcNodes.AddRange(request.OpcNodes);
                        }
                        existingGroup.Add(entry);
                    }
                }

                if (!existingGroup.Any()) {
                    existingGroup.Add(request);
                    _publishedNodesEntrys.Add(request);
                }

                var found = false;
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroup, _legacyCliModel);
                if (jobs.Any()) {
                    foreach (var job in jobs) {
                        var newJob = ToJobProcessingInstructionModel(job);
                        if (string.IsNullOrEmpty(newJob?.Job?.Id)) {
                            continue;
                        }
                        foreach (var assignedJob in _assignedJobs) {
                            if (newJob.Job.Id == assignedJob.Value.Job.Id) {
                                _assignedJobs[assignedJob.Key] = newJob;
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            _availableJobs.AddOrUpdate(newJob.Job.Id, newJob);
                            found = true;
                        }
                    }
                }
                if (!found) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Endpoint not found.");
                }

                // fire config update so that the worker supervisor pickes up the changes ASAP
                TriggerAgentConfigUpdate();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _lock.Release();
            }

            return new List<string> { "Succeeded" };
        }

        /// <inheritdoc/>
        public async Task<List<string>> UnpublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(UnpublishNodesAsync));
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try {
                var nodeFound = false;
                var existingGroup = new List<PublishedNodesEntryModel>();
                foreach (var entry in _publishedNodesEntrys.ToList()) {
                    if (entry.HasSameGroup(request)) {
                        if (request.DataSetWriterId == entry.DataSetWriterId &&
                            request.DataSetPublishingInterval == entry.DataSetPublishingInterval) {
                            foreach (var requestNode in request.OpcNodes) {
                                foreach (var entryNode in entry.OpcNodes) {
                                    if (requestNode.IsSame(entryNode)) {
                                        entry.OpcNodes.Remove(entryNode);
                                        nodeFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                        existingGroup.Add(entry);
                    }
                }

                if (!existingGroup.Any()) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Endpoint not found.");
                }

                if (!nodeFound) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Node not found in endpoint.");
                }

                foreach (var entry in existingGroup) {
                    if (!entry.OpcNodes.Any()) {
                        _publishedNodesEntrys.Remove(entry);
                    }
                }

                var found = false;
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroup, _legacyCliModel);
                if (jobs.Any()) {
                    foreach (var job in jobs) {
                        var newJob = ToJobProcessingInstructionModel(job);
                        if (string.IsNullOrEmpty(newJob?.Job?.Id)) {
                            continue;
                        }
                        foreach (var assignedJob in _assignedJobs) {
                            if (newJob.Job.Id == assignedJob.Value.Job.Id) {
                                _assignedJobs[assignedJob.Key] = newJob;
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            if (_availableJobs.ContainsKey(newJob.Job.Id)) {
                                _availableJobs[newJob.Job.Id] = newJob;
                                found = true;
                            }
                        }
                    }
                }

                if (!found) {
                    var entryJobId = _publishedNodesJobConverter.
                        ToConnectionModel(request, _legacyCliModel).CreateConnectionId();
                    foreach (var assignedJob in _assignedJobs) {
                        if (entryJobId == assignedJob.Value.Job.Id) {
                            found = _assignedJobs.TryRemove(assignedJob.Key, out _);
                            break;
                        }
                    }
                    if (!found) {
                        found = _availableJobs.TryRemove(entryJobId, out _);
                    }
                }

                if (!found) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Endpoint not found.");
                }

                // fire config update so that the worker supervisor pickes up the changes ASAP
                TriggerAgentConfigUpdate();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _lock.Release();
            }

            return new List<string> { "Succeeded" };
        }

        /// <inheritdoc/>
        public async Task UnpublishAllNodesAsync(CancellationToken ct) {
            _logger.Information("{nameof} method triggered", nameof(UnpublishAllNodesAsync));
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try {
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task GetConfiguredEndpointsAsync(CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(GetConfiguredEndpointsAsync));
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try {
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task GetConfiguredNodesOnEndpointAsync(CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(GetConfiguredNodesOnEndpointAsync));
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try {
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        private readonly IJobSerializer _jobSerializer;
        private readonly LegacyCliModel _legacyCliModel;
        private readonly IAgentConfigProvider _agentConfig;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly PublishedNodesProvider _publishedNodesProvider;
        private readonly SemaphoreSlim _lock;
        private readonly List<PublishedNodesEntryModel> _publishedNodesEntrys;
        private ConcurrentDictionary<string, JobProcessingInstructionModel> _assignedJobs;
        private ConcurrentDictionary<string, JobProcessingInstructionModel> _availableJobs;
        private string _lastKnownFileHash = string.Empty;
        private DateTime _lastRead = DateTime.MinValue;
    }
}