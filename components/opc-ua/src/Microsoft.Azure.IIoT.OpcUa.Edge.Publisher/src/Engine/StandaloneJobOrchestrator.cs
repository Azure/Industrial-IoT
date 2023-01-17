// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job orchestrator represents the used in the standalone mode, based on configuration stored in
    /// publishednodes.json with specific standalone command line arguments.
    /// </summary>
    public class StandaloneJobOrchestrator : IJobOrchestrator, IPublisherConfigServices, IDisposable {
        /// <summary>
        /// Creates a new class of the StandaloneJobOrchestrator.
        /// </summary>
        public StandaloneJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter,
            IStandaloneCliModelProvider standaloneCliModelProvider, IAgentConfigProvider agentConfigProvider,
            IJobSerializer jobSerializer, ILogger logger, IPublishedNodesProvider publishedNodesProvider,
            IJsonSerializer jsonSerializer
        ) {
            _publishedNodesJobConverter = publishedNodesJobConverter
                ?? throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _standaloneCliModel = standaloneCliModelProvider.StandaloneCliModel
                    ?? throw new ArgumentNullException(nameof(standaloneCliModelProvider));
            _agentConfig = agentConfigProvider
                    ?? throw new ArgumentNullException(nameof(agentConfigProvider));

            _jobSerializer = jobSerializer ?? throw new ArgumentNullException(nameof(jobSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publishedNodesProvider = publishedNodesProvider ?? throw new ArgumentNullException(nameof(publishedNodesProvider));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));

            _availableJobs = new Dictionary<string, JobProcessingInstructionModel>();
            _assignedJobs = new Dictionary<string, JobProcessingInstructionModel>();

            _lockConfig = new SemaphoreSlim(1, 1);
            _lockJobs = new SemaphoreSlim(1, 1);

            _publishedNodesEntries = new List<PublishedNodesEntryModel>();
            _publisherDiagnosticInfo = new Dictionary<string, JobDiagnosticInfoModel>();

            RefreshJobFromFile();
            _publishedNodesProvider.Changed += _fileSystemWatcher_Changed;
            _publishedNodesProvider.Created += _fileSystemWatcher_Created;
            _publishedNodesProvider.Renamed += _fileSystemWatcher_Renamed;
            _publishedNodesProvider.Deleted += _fileSystemWatcher_Deleted;
            _publishedNodesProvider.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Gets the next available job - this will always return the job representation of the standalone
        /// mode publishednodes.json along with respective  command line arguments.
        /// </summary>
        public async Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId,
            JobRequestModel request, CancellationToken ct = default) {
            await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
            try {
                ct.ThrowIfCancellationRequested();
                if (_assignedJobs.TryGetValue(workerId, out var job)) {
                    return job;
                }
                if (_availableJobs.Any() && _availableJobs.Remove(_availableJobs.First().Key, out job)) {
                    _assignedJobs.AddOrUpdate(workerId, job);
                    return job;
                }
                else {
                    // There are no available jobs or we were not able to get a job from _availableJobs.
                    return null;
                }
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
                _lockJobs.Release();
            }
        }

        /// <summary>
        /// Receives the heartbeat from the StandaloneJobOrchestrator, JobProcess; used
        /// to control lifetime of job (cancel, update, restart, keep).
        /// Used also to receive the diagnostic info
        /// </summary>
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(
            HeartbeatModel heartbeat,
            JobDiagnosticInfoModel diagInfo,
            CancellationToken ct = default) {
            if (heartbeat == null || heartbeat.Worker == null) {
                throw new ArgumentNullException(nameof(heartbeat));
            }
            await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
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
                                    HeartbeatInstruction = HeartbeatInstruction.Update,
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
                if (diagInfo != null) {
                    foreach (var assignedJob in _assignedJobs) {

                        if (diagInfo.Id == assignedJob.Value.Job.Id) {
                            _publisherDiagnosticInfo.AddOrUpdate(assignedJob.Value.Job.Id, diagInfo);
                        }
                    }
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
                _lockJobs.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_publishedNodesProvider != null) {
                _publishedNodesProvider.EnableRaisingEvents = false;
                _publishedNodesProvider.Changed -= _fileSystemWatcher_Changed;
                _publishedNodesProvider.Created -= _fileSystemWatcher_Created;
                _publishedNodesProvider.Renamed -= _fileSystemWatcher_Renamed;
                _publishedNodesProvider.Deleted -= _fileSystemWatcher_Deleted;
            }
            _lockConfig?.Dispose();
            _lockJobs?.Dispose();
        }

        private void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} changed. Triggering file refresh ...", _standaloneCliModel.PublishedNodesFile);
            RefreshJobFromFile();
        }

        private void _fileSystemWatcher_Created(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} created. Triggering file refresh ...", _standaloneCliModel.PublishedNodesFile);
            RefreshJobFromFile();
        }

        private void _fileSystemWatcher_Renamed(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} renamed. Triggering file refresh ...", _standaloneCliModel.PublishedNodesFile);
            RefreshJobFromFile();
        }

        private void _fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} deleted. Clearing configuration ...", _standaloneCliModel.PublishedNodesFile);
            _lockConfig.Wait();
            try {
                _publishedNodesEntries.Clear();
                _lastKnownFileHash = string.Empty;
                _lockJobs.Wait();
                try {
                    _availableJobs.Clear();
                    _assignedJobs.Clear();
                    _publisherDiagnosticInfo.Clear();
                }
                finally {
                    _lockJobs.Release();
                }
            }
            finally {
                _lockConfig.Release();
            }
        }

        private static string GetChecksum(string content) {
            if (string.IsNullOrEmpty(content)) {
                return null;
            }
            using (var sha = SHA256.Create()) {
                var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// Deserialize string representing published nodes file into PublishedNodesEntryModel entries and
        /// run schema validation if that is enables.
        /// </summary>
        private IEnumerable<PublishedNodesEntryModel> DeserializePublishedNodes(string content) {
            bool ioErrorEncountered = false;
            if (File.Exists(_standaloneCliModel.PublishedNodesSchemaFile)) {
                try {
                    using (var fileSchemaReader = new StreamReader(_standaloneCliModel.PublishedNodesSchemaFile)) {
                        return _publishedNodesJobConverter.Read(content, fileSchemaReader);
                    }
                }
                catch (IOException e) {
                    _logger.Warning(e, "File IO exception when reading published nodes schema file at \"{path}\"." +
                        "Falling back to deserializing content of published nodes file without schema validation.",
                        _standaloneCliModel.PublishedNodesSchemaFile);
                    ioErrorEncountered = true;
                }
            }

            // Deserialize without schema validation.
            if (!ioErrorEncountered) {
                _logger.Information("Validation schema file {PublishedNodesSchemaFile} does not exist or is disabled, " +
                    "ignoring validation of {publishedNodesFile} file.",
                    _standaloneCliModel.PublishedNodesSchemaFile, _standaloneCliModel.PublishedNodesFile);
            }

            return _publishedNodesJobConverter.Read(content, null);
        }

        private void RefreshJobs(IEnumerable<PublishedNodesEntryModel> entries) {
            var availableJobs = new Dictionary<string, JobProcessingInstructionModel>();
            var assignedJobs = new Dictionary<string, JobProcessingInstructionModel>();
            var publisherDiagnosticInfo = new Dictionary<string, JobDiagnosticInfoModel>();

            var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(entries, _standaloneCliModel);

            _lockJobs.Wait();
            try {
                // Create JobId to WorkerId dictionary for fast lookup.
                var jobIdDict = _assignedJobs
                    .Select(kvp => Tuple.Create(kvp.Value.Job.Id, kvp.Key))
                    .ToDictionary(tuple => tuple.Item1);

                if (jobs.Any()) {
                    foreach (var job in jobs) {
                        var newJob = ToJobProcessingInstructionModel(job);
                        if (string.IsNullOrEmpty(newJob?.Job?.Id)) {
                            continue;
                        }

                        var newJobId = newJob.Job.Id;
                        if (jobIdDict.ContainsKey(newJobId)) {
                            assignedJobs[jobIdDict[newJobId].Item2] = newJob;
                            if (_publisherDiagnosticInfo.ContainsKey(newJobId)) {
                                publisherDiagnosticInfo.Add(newJobId, _publisherDiagnosticInfo[newJobId]);
                            }
                        }
                        else {
                            availableJobs.Add(newJobId, newJob);
                        }
                    }
                }

                // Update local state.
                _availableJobs = availableJobs;
                _assignedJobs = assignedJobs;
                _publisherDiagnosticInfo = publisherDiagnosticInfo;

                AdjustMaxWorkersAgentConfig();
            }
            finally {
                _lockJobs.Release();
            }
        }

        /// <summary>
        /// Remove entries from list of PublishedNodesEntryModel objects that do not contain any node definition.
        /// </summary>
        /// <remarks>
        /// Note that the method does not do any locking.
        /// </remarks>
        /// <param name="entries"></param>
        private static void RemoveEntriesWithoutNodes(List<PublishedNodesEntryModel> entries) {
            if (entries == null) {
                return;
            }

            entries.RemoveAll(entry => entry.OpcNodes == null || entry.OpcNodes.Count == 0);
        }

        /// <summary>
        /// Transforms legacy entries that use NodeId into ones using OpcNodes.
        /// The transformation will happen in-place.
        /// </summary>
        /// <param name="entries"></param>
        private static void TransformFromLegacyNodeId(List<PublishedNodesEntryModel> entries) {
            if (entries == null) {
                return;
            }

            foreach (var entry in entries) {
                if (!string.IsNullOrEmpty(entry.NodeId?.Identifier)) {
                    if (entry.OpcNodes == null) {
                        entry.OpcNodes = new List<OpcNodeModel>();
                    }

                    if (entry.OpcNodes.Count != 0) {
                        throw new SerializerException($"Published nodes file contains DataSetWriter entry which " +
                            $"defines both {nameof(entry.OpcNodes)} and {nameof(entry.NodeId)}." +
                            $"This is not supported. Please fix published nodes file.");
                    }

                    entry.OpcNodes.Add(new OpcNodeModel {
                        Id = entry.NodeId.Identifier,
                    });

                    entry.NodeId = null;
                }
            }
        }

        private void RefreshJobFromFile() {
            var retryCount = 3;
            var lastWriteTime = _publishedNodesProvider.GetLastWriteTime();
            while (true) {
                _lockConfig.Wait();
                try {
                    var content = _publishedNodesProvider.ReadContent();
                    var lastValidFileHash = _lastKnownFileHash;
                    var currentFileHash = GetChecksum(content);

                    if (currentFileHash != _lastKnownFileHash) {
                        _logger.Information("File {publishedNodesFile} has changed, last known hash {LastHash}, new hash {NewHash}, reloading...",
                            _standaloneCliModel.PublishedNodesFile,
                            _lastKnownFileHash,
                            currentFileHash);

                        _lastKnownFileHash = currentFileHash;
                        if (!string.IsNullOrEmpty(content)) {
                            List<PublishedNodesEntryModel> entries = null;

                            try {
                                entries = DeserializePublishedNodes(content).ToList();
                                TransformFromLegacyNodeId(entries);
                            }
                            catch (IOException) {
                                throw; //pass it thru, to handle retries
                            }
                            catch (SerializerException ex) {
                                _logger.Warning(ex, "Failed to deserialize {publishedNodesFile}, aborting reload...",
                                    _standaloneCliModel.PublishedNodesFile);
                                _lastKnownFileHash = lastValidFileHash;
                                break;
                            }

                            // Remove entries without node definitions.
                            RemoveEntriesWithoutNodes(entries);

                            _publishedNodesEntries.Clear();
                            if (entries != null) {
                                _publishedNodesEntries.AddRange(entries);
                            }

                            RefreshJobs(entries);
                        }
                        else {
                            _lockJobs.Wait();
                            try {
                                _availableJobs.Clear();
                                _assignedJobs.Clear();
                                _publisherDiagnosticInfo.Clear();
                            }
                            finally {
                                _lockJobs.Release();
                            }

                            _publishedNodesEntries.Clear();
                        }

                        // fire config update so that the worker supervisor pickes up the changes ASAP
                        TriggerAgentConfigUpdate();
                    }
                    else {
                        //avoid double events from FileSystemWatcher
                        if (lastWriteTime - _lastRead > TimeSpan.FromMilliseconds(10)) {
                            _logger.Information("File {publishedNodesFile} has changed and content-hash" +
                                " is equal to last one, nothing to do", _standaloneCliModel.PublishedNodesFile);
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
                    _logger.Error(e, "Error while reloading {PublishedNodesFile}. Reseting the configuration.",
                        _standaloneCliModel.PublishedNodesFile);
                    _lockJobs.Wait();
                    try {
                        _availableJobs.Clear();
                        _assignedJobs.Clear();
                        _publisherDiagnosticInfo.Clear();
                    }
                    finally {
                        _lockJobs.Release();
                    }
                    _publishedNodesEntries.Clear();
                    _lastKnownFileHash = string.Empty;
                    break;
                }
                finally {
                    _lockConfig.Release();
                }
            }
        }

        private JobProcessingInstructionModel ToJobProcessingInstructionModel(WriterGroupJobModel job){
            if (job == null) {
                return null;
            }

            var jobId = job.GetJobId();
            var dataSetWriters = string.Join(", ", job.WriterGroup.DataSetWriters.Select(w => w.DataSetWriterName));
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
        /// Adjust the configuration of max workers in the Agent's config
        /// </summary>
        private void AdjustMaxWorkersAgentConfig() {
            if (_agentConfig.Config.MaxWorkers < _availableJobs.Count + _assignedJobs.Count) {
                _agentConfig.Config.MaxWorkers = _availableJobs.Count + _assignedJobs.Count;
            }
        }

        /// <summary>
        /// Notify the worker supervisor about a configuration change
        /// </summary>
        private void TriggerAgentConfigUpdate() {
            _agentConfig.TriggerConfigUpdate(this, new EventArgs());
        }

        private void ValidateRequest(PublishedNodesEntryModel request) {
            var requestList = new List<PublishedNodesEntryModel> { request };
            var requestJson = _jsonSerializer.SerializeToString(requestList);

            // This will throw SerializerException if values of request fields are not conformant.
            DeserializePublishedNodes(requestJson);
        }

        /// <summary>
        /// Persist _publishedNodesEntries to published nodes file.
        /// </summary>
        /// <remarks>
        /// Note that the method assumes that it is called from a locked block on _lockConfig.
        /// So please acquire the lock on _lockConfig before performing the call.
        /// </remarks>
        private void PersistPublishedNodes() {
            var updatedContent = _jsonSerializer.SerializeToString(_publishedNodesEntries, SerializeOption.Indented);
            _publishedNodesProvider.WriteContent(updatedContent, true);

            // Update _lastKnownFileHash
            _lastKnownFileHash = GetChecksum(updatedContent);
        }

        /// <inheritdoc/>
        public async Task PublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered ... ", nameof(PublishNodesAsync));
            var sw = Stopwatch.StartNew();

            if (request is null || request.OpcNodes is null || request.OpcNodes.Count == 0) {
                var message = request is null
                    ? kNullRequestMessage
                    : kNullOrEmptyOpcNodesMessage;

                _logger.Information("{nameof} method finished in {elapsed}", nameof(PublishNodesAsync), sw.Elapsed);
                sw.Stop();

                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }

            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                // ToDo: Uncomment ValidateRequest() once our test requests pass validation.
                // This will throw SerializerException if values of request fields are not conformant.
                //ValidateRequest(request);

                var dataSetFound = false;
                var existingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in _publishedNodesEntries) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will add nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request)) {
                            // Create HashSet of nodes for this entry.
                            var existingNodesSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                            existingNodesSet.UnionWith(entry.OpcNodes);

                            foreach (var nodeToAdd in request.OpcNodes) {
                                if (!existingNodesSet.Contains(nodeToAdd)) {
                                    entry.OpcNodes.Add(nodeToAdd);
                                    existingNodesSet.Add(nodeToAdd);
                                }
                                else {
                                    _logger.Debug("Node \"{node}\" is already present for entry with \"{endpoint}\" endpoint.",
                                        nodeToAdd.Id, entry.EndpointUrl);
                                }
                            }

                            // refresh the Tag if a new one is provided
                            entry.Tag = request.Tag;

                            dataSetFound = true;
                        }

                        // Even if DataSets did not match, we need to add this entry to existingGroups
                        // so that generated job definition is complete.
                        existingGroups.Add(entry);
                    }
                }

                if (!dataSetFound) {
                    existingGroups.Add(request);
                    _publishedNodesEntries.Add(request);
                }

                PersistPublishedNodes();

                var found = false;
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups, _standaloneCliModel);

                if (jobs.Any()) {
                    await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
                    try {
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
                        if (found) {
                            AdjustMaxWorkersAgentConfig();
                        }
                    }
                    finally {
                        _lockJobs.Release();
                    }
                }

                if (!found) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                        $"Endpoint not found: {request.EndpointUrl}");
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
                _logger.Information("{nameof} method finished in {elapsed}", nameof(PublishNodesAsync), sw.Elapsed);
                sw.Stop();
                _lockConfig.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered ...", nameof(UnpublishNodesAsync));
            var sw = Stopwatch.StartNew();

            if (request is null) {
                _logger.Information("{nameof} method finished in {elapsed}", nameof(UnpublishNodesAsync), sw.Elapsed);
                sw.Stop();

                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, kNullRequestMessage);
            }

            // When no node is specified then remove the whole data set.
            // This behavior ensures backwards compatibility with UnpublishNodes
            // direct method of OPC Publisher 2.5.x.
            var purgeDataSet = (request.OpcNodes is null || request.OpcNodes.Count == 0);

            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                // ToDo: Uncomment ValidateRequest() once our test requests pass validation.
                // This will throw SerializerException if values of request fields are not conformant.
                //ValidateRequest(request);

                // Create HashSet of nodes to remove.
                var nodesToRemoveSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                if (!purgeDataSet) {
                    nodesToRemoveSet.UnionWith(request.OpcNodes);
                }

                // Perform first pass to determine if we can find all nodes to remove.
                var matchingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in _publishedNodesEntries) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will remove nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request)) {
                            foreach (var node in entry.OpcNodes) {
                                if (nodesToRemoveSet.Contains(node)) {
                                    // Found a node. Remove it from hash set.
                                    nodesToRemoveSet.Remove(node);
                                }
                            }

                            matchingGroups.Add(entry);
                        }
                    }
                }

                // Report error if no matching endpoint was found.
                if (matchingGroups.Count == 0) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                        $"Endpoint not found: {request.EndpointUrl}");
                }

                // Report error if there were entries that we were not able to find.
                if (nodesToRemoveSet.Count != 0) {
                    request.OpcNodes = nodesToRemoveSet.ToList();
                    var entriesNotFoundJson = _jsonSerializer.SerializeToString(request);
                    throw new MethodCallStatusException(entriesNotFoundJson, (int)HttpStatusCode.NotFound, "Nodes not found");
                }

                // Create HashSet of nodes to remove again for the second pass.
                nodesToRemoveSet.Clear();
                if (!purgeDataSet) {
                    nodesToRemoveSet.UnionWith(request.OpcNodes);
                }

                // Perform second pass and remove entries this time.
                var existingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in _publishedNodesEntries) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will remove nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request)) {
                            if (!purgeDataSet) {
                                var updatedNodes = new List<OpcNodeModel>();

                                foreach (var node in entry.OpcNodes) {
                                    if (nodesToRemoveSet.Contains(node)) {
                                        // Found a node. Remove it from hash set.
                                        nodesToRemoveSet.Remove(node);
                                    }
                                    else {
                                        updatedNodes.Add(node);
                                    }
                                }

                                entry.OpcNodes = updatedNodes;

                                // refresh the Tag if a new one is provided
                                entry.Tag = request.Tag;
                            }
                            else {
                                entry.OpcNodes.Clear();
                            }
                        }

                        // Even if DataSets did not match, we need to add this entry to existingGroups
                        // so that generated job definition is complete.
                        existingGroups.Add(entry);
                    }
                }

                // Remove entries without node definitions.
                RemoveEntriesWithoutNodes(_publishedNodesEntries);

                PersistPublishedNodes();

                var found = false;
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups, _standaloneCliModel);

                await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
                try {
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
                            if (!found && _availableJobs.ContainsKey(newJob.Job.Id)) {
                                _availableJobs[newJob.Job.Id] = newJob;
                                found = true;
                            }
                        }
                    }

                    // In the case that all OpcNodes entries were removed from existingGroups
                    // ToWriterGroupJobs() will return an empty result. So we need to manually
                    // remove corresponsing entries from _assignedJobs and _availableJobs.
                    if (!found) {
                        var entryJobId = _publishedNodesJobConverter.
                            ToConnectionModel(request, _standaloneCliModel).CreateConnectionId();
                        foreach (var assignedJob in _assignedJobs) {
                            if (entryJobId == assignedJob.Value.Job.Id) {
                                found = _assignedJobs.Remove(assignedJob.Key, out _);
                                _publisherDiagnosticInfo.Remove(assignedJob.Value.Job.Id, out _);
                                break;
                            }
                        }
                        if (!found) {
                            found = _availableJobs.Remove(entryJobId, out _);
                        }
                    }
                }
                finally {
                    _lockJobs.Release();
                }

                if (!found) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                        $"Endpoint not found: {request.EndpointUrl}");
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
                _logger.Information("{nameof} method finished in {elapsed}", nameof(UnpublishNodesAsync), sw.Elapsed);
                sw.Stop();
                _lockConfig.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishAllNodesAsync(
            PublishedNodesEntryModel request,
            CancellationToken ct) {
            _logger.Information("{nameof} method triggered", nameof(UnpublishAllNodesAsync));
            var sw = Stopwatch.StartNew();
            // when no endpoint is specified remove all the configuration
            // purge content feature is implemented to ensure the backwards compatibility
            // with V2.5.x of the publisher
            var purge = null == request?.EndpointUrl;
            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (!purge) {
                    var found = false;
                    // Perform pass to determine existing groups
                    var matchingGroups = new List<PublishedNodesEntryModel>();
                    foreach (var entry in _publishedNodesEntries) {
                        if (entry.HasSameGroup(request)) {
                            // We may have several entries with the same DataSetGroup definition,
                            // so we will remove nodes only if the whole DataSet definition matches.
                            if (entry.HasSameDataSet(request)) {
                                entry.OpcNodes.Clear();
                                found = true;
                            }
                            matchingGroups.Add(entry);
                        }
                    }

                    // Report error if there were entries that did not have any nodes
                    if (!found) {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound, $"Endpoint or node not found: {request.EndpointUrl}");
                    }

                    // Remove entries without node definitions.
                    RemoveEntriesWithoutNodes(_publishedNodesEntries);

                    PersistPublishedNodes();

                    found = false;
                    var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(matchingGroups, _standaloneCliModel);

                    await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
                    try {
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
                                if (!found && _availableJobs.ContainsKey(newJob.Job.Id)) {
                                    _availableJobs[newJob.Job.Id] = newJob;
                                    found = true;
                                }
                            }
                        }

                        if (!found) {
                            var entryJobId = _publishedNodesJobConverter.
                                ToConnectionModel(request, _standaloneCliModel).CreateConnectionId();
                            foreach (var assignedJob in _assignedJobs) {
                                if (entryJobId == assignedJob.Value.Job.Id) {
                                    found = _assignedJobs.Remove(assignedJob.Key, out _);
                                    _publisherDiagnosticInfo.Remove(assignedJob.Value.Job.Id, out _);
                                    break;
                                }
                            }
                            if (!found) {
                                found = _availableJobs.Remove(entryJobId, out _);
                            }
                        }
                    }
                    finally {
                        _lockJobs.Release();
                    }

                    if (!found) {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                            $"Endpoint not found: {request.EndpointUrl}");
                    }
                }
                else {

                    // Remove all entries
                    _publishedNodesEntries.Clear();
                    PersistPublishedNodes();
                    await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
                    try {
                        _assignedJobs.Clear();
                        _availableJobs.Clear();
                        _publisherDiagnosticInfo.Clear();
                    }
                    finally {
                        _lockJobs.Release();
                    }
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
                _logger.Information("{nameof} method finished in {elapsed}", nameof(UnpublishAllNodesAsync), sw.Elapsed);
                sw.Stop();
                _lockConfig.Release();
            }
        }

        /// <inheritdoc/>
        public async Task AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request,
            CancellationToken ct = default) {

            var methodName = nameof(AddOrUpdateEndpointsAsync);
            _logger.Information("{methodName} method triggered ... ", methodName);
            var sw = Stopwatch.StartNew();

            if (request is null) {
                _logger.Information("{methodName} method finished in {elapsed}", methodName, sw.Elapsed);
                sw.Stop();

                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, kNullRequestMessage);
            }

            // First, let's check that there are no 2 entries for the same endpoint in the request.
            for (int itemIndex = 1; itemIndex < request.Count; itemIndex++) {
                for (int prevItemIndex = 0; prevItemIndex < itemIndex; prevItemIndex++) {
                    if (request[itemIndex].HasSameDataSet(request[prevItemIndex])) {
                        throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                            $"Request contains two entries for the same endpoint at index {prevItemIndex} and {itemIndex}");
                    }
                }
            }

            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                // ToDo: Uncomment ValidateRequest() once our test requests pass validation.
                // This will throw SerializerException if values of request fields are not conformant.
                //ValidateRequest(request);

                // Second, let's check that endpoints that we are asked to remove exist.
                var dataSetsToRemove = request.Where(e => e.OpcNodes is null || e.OpcNodes.Count == 0).ToList();

                foreach (var dataSetToRemove in dataSetsToRemove) {
                    var foundDataSet = false;
                    foreach (var entry in _publishedNodesEntries) {
                        foundDataSet = foundDataSet || entry.HasSameDataSet(dataSetToRemove);
                    }
                    if (!foundDataSet) {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                            $"Endpoint not found: {dataSetToRemove.EndpointUrl}");
                    }
                }

                var existingGroups = new List<PublishedNodesEntryModel>();
                var requestDataSetsFound = Enumerable.Repeat(false, request.Count).ToList(); ;

                foreach (var entry in _publishedNodesEntries) {
                    var groupFound = false;

                    for (var k = 0; k < request.Count; ++k) {
                        var dataSetToUpdate = request[k];
                        if (entry.HasSameGroup(dataSetToUpdate)) {
                            groupFound = true;

                            // We may have several entries with the same DataSetGroup definition,
                            // so we will update nodes only if the whole DataSet definition matches.
                            if (entry.HasSameDataSet(dataSetToUpdate)) {
                                if (dataSetToUpdate.OpcNodes is null || dataSetToUpdate.OpcNodes.Count == 0 || requestDataSetsFound[k]) {
                                    // In this case existing OpcNodes entries should be cleaned up.
                                    entry.OpcNodes?.Clear();
                                }
                                else {
                                    // We will add OpcNodes to the first matching entry
                                    // and the rest will be cleaned up.
                                    entry.OpcNodes = dataSetToUpdate.OpcNodes;
                                }

                                // We do not need to look for another matching data set in request.
                                requestDataSetsFound[k] = true;

                                // refresh the Tag if a new one is provided
                                entry.Tag = dataSetToUpdate.Tag;
                                break;
                            }
                        }
                    }

                    if (groupFound) {
                        // Even if DataSets did not match, we need to add this entry to existingGroups
                        // so that generated job definition is complete.
                        existingGroups.Add(entry);
                    }
                }

                // Add new data sets from request.
                for (var k = 0; k < request.Count; ++k) {
                    if (!requestDataSetsFound[k]) {
                        existingGroups.Add(request[k]);
                        _publishedNodesEntries.Add(request[k]);
                    }
                }

                // Remove entries without node definitions.
                RemoveEntriesWithoutNodes(_publishedNodesEntries);

                PersistPublishedNodes();

                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups, _standaloneCliModel);

                await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
                try {
                    // We will first update existing jobs. Then we will cleanup empty ones.
                    var processedJobIds = new HashSet<string>();

                    if (jobs.Any()) {
                        foreach (var job in jobs) {
                            var newJob = ToJobProcessingInstructionModel(job);

                            if (string.IsNullOrEmpty(newJob?.Job?.Id)) {
                                continue;
                            }

                            var jobFound = false;
                            foreach (var assignedJob in _assignedJobs) {
                                if (newJob.Job.Id == assignedJob.Value.Job.Id) {
                                    _assignedJobs[assignedJob.Key] = newJob;
                                    jobFound = true;
                                    break;
                                }
                            }
                            if (!jobFound) {
                                _availableJobs.AddOrUpdate(newJob.Job.Id, newJob);
                            }

                            processedJobIds.Add(newJob.Job.Id);
                        }
                    }

                    // In the case that all OpcNodes entries were removed from existingGroups
                    // ToWriterGroupJobs() will return an empty result. So we need to manually
                    // remove entries from _assignedJobs and _availableJobs that were marked for
                    // removal in request.
                    foreach (var dataSetToRemove in dataSetsToRemove) {
                        var entryJobId = _publishedNodesJobConverter.
                            ToConnectionModel(dataSetToRemove, _standaloneCliModel).CreateConnectionId();

                        // If entry with this JobId was already processed, then there already
                        // was an update operation for it. We will skip its removal.
                        if (!processedJobIds.Contains(entryJobId)) {
                            var jobFound = false;
                            foreach (var assignedJob in _assignedJobs) {
                                if (entryJobId == assignedJob.Value.Job.Id) {
                                    jobFound = _assignedJobs.Remove(assignedJob.Key);
                                    _publisherDiagnosticInfo.Remove(assignedJob.Value.Job.Id);
                                    break;
                                }
                            }
                            if (!jobFound) {
                                _availableJobs.Remove(entryJobId);
                            }
                        }
                    }

                    AdjustMaxWorkersAgentConfig();
                }
                finally {
                    _lockJobs.Release();
                }

                // fire config update so that the worker supervisor pickes up the changes ASAP
                TriggerAgentConfigUpdate();
            }
            finally {
                _lockConfig.Release();

                _logger.Information("{methodName} method finished in {elapsed}", methodName, sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(
            CancellationToken ct = default) {

            var methodName = nameof(GetConfiguredEndpointsAsync);
            _logger.Information("{nameof} method triggered", methodName);
            var sw = Stopwatch.StartNew();

            var endpoints = new List<PublishedNodesEntryModel>();
            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);

            try {
                endpoints = _publishedNodesEntries.Select(model => new PublishedNodesEntryModel {
                    EndpointUrl = model.EndpointUrl,
                    UseSecurity = model.UseSecurity,
                    OpcAuthenticationMode = model.OpcAuthenticationMode,
                    OpcAuthenticationUsername = model.OpcAuthenticationUsername,
                    DataSetWriterGroup = model.DataSetWriterGroup,
                    DataSetWriterId = model.DataSetWriterId,
                    DataSetName = model.DataSetName,
                    DataSetDescription = model.DataSetDescription,
                    DataSetKeyFrameCount = model.DataSetKeyFrameCount,
                    DataSetMetaDataSendInterval = model.DataSetMetaDataSendInterval,
                    DataSetClassId = model.DataSetClassId,
                    Tag = model.Tag,
                    DataSetPublishingIntervalTimespan = model.DataSetPublishingIntervalTimespan,
                    DataSetPublishingInterval = !model.DataSetPublishingIntervalTimespan.HasValue
                        ? model.DataSetPublishingInterval
                        :null,
                }).ToList();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _lockConfig.Release();

                _logger.Information("{methodName} method finished in {elapsed}", methodName, sw.Elapsed);
                sw.Stop();
            }
            return endpoints;
        }

        /// <inheritdoc/>
        public async Task<List<OpcNodeModel>> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request,
            CancellationToken ct = default) {

            _logger.Information("{nameof} method triggered", nameof(GetConfiguredNodesOnEndpointAsync));
            var sw = Stopwatch.StartNew();

            if (request is null) {
                _logger.Information("{nameof} method finished in {elapsed}", nameof(GetConfiguredNodesOnEndpointAsync), sw.Elapsed);
                sw.Stop();

                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, kNullRequestMessage);
            }

            List<OpcNodeModel> response = new List<OpcNodeModel>();
            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                var endpointFound = false;

                foreach (var entry in _publishedNodesEntries) {
                    if (entry.HasSameDataSet(request)) {
                        endpointFound = true;
                        if (entry.OpcNodes != null) {
                            response.AddRange(entry.OpcNodes);
                        }
                    }
                }

                if (!endpointFound) {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                        $"Endpoint not found: {request.EndpointUrl}");
                }
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _logger.Information("{nameof} method finished in {elapsed}", nameof(GetConfiguredNodesOnEndpointAsync), sw.Elapsed);
                sw.Stop();
                _lockConfig.Release();
            }
            return response;
        }

        /// <inheritdoc/>
        public async Task<List<JobDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(GetDiagnosticInfoAsync));
            var sw = Stopwatch.StartNew();
            await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
            try {
                return _publisherDiagnosticInfo.Values.ToList();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _logger.Information("{nameof} method finished in {elapsed}", nameof(GetDiagnosticInfoAsync), sw.Elapsed);
                sw.Stop();
                _lockJobs.Release();
            }
        }

        private readonly static string kNullRequestMessage = "null request is provided";
        private readonly static string kNullOrEmptyOpcNodesMessage = "null or empty OpcNodes is provided in request";

        private readonly IJobSerializer _jobSerializer;
        private readonly StandaloneCliModel _standaloneCliModel;
        private readonly IAgentConfigProvider _agentConfig;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly IPublishedNodesProvider _publishedNodesProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly SemaphoreSlim _lockJobs;
        private readonly SemaphoreSlim _lockConfig;
        private readonly List<PublishedNodesEntryModel> _publishedNodesEntries;
        private Dictionary<string, JobProcessingInstructionModel> _assignedJobs;
        private Dictionary<string, JobProcessingInstructionModel> _availableJobs;
        private string _lastKnownFileHash = string.Empty;
        private DateTime _lastRead = DateTime.MinValue;
        private Dictionary<string, JobDiagnosticInfoModel> _publisherDiagnosticInfo;
    }
}
