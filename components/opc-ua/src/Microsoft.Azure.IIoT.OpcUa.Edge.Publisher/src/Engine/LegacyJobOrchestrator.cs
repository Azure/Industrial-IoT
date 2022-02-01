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
        /// <param name="jsonSerializer">Json serializer.</param>
        public LegacyJobOrchestrator(PublishedNodesJobConverter publishedNodesJobConverter,
            ILegacyCliModelProvider legacyCliModelProvider, IAgentConfigProvider agentConfigProvider,
            IJobSerializer jobSerializer, ILogger logger, IPublishedNodesProvider publishedNodesProvider,
            IJsonSerializer jsonSerializer
        ) {
            _publishedNodesJobConverter = publishedNodesJobConverter
                ?? throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _legacyCliModel = legacyCliModelProvider.LegacyCliModel
                    ?? throw new ArgumentNullException(nameof(legacyCliModelProvider));
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

            RefreshJobFromFile();
            _publishedNodesProvider.Changed += _fileSystemWatcher_Changed;
            _publishedNodesProvider.Created += _fileSystemWatcher_Created;
            _publishedNodesProvider.Renamed += _fileSystemWatcher_Renamed;
            _publishedNodesProvider.Deleted += _fileSystemWatcher_Deleted;
            _publishedNodesProvider.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Gets the next available job - this will always return the job representation of the legacy publishednodes.json
        /// along with legacy command line arguments.
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<JobProcessingInstructionModel> GetAvailableJobAsync(
            string workerId,
            JobRequestModel request,
            CancellationToken ct = default
        ) {
            await _lockJobs.WaitAsync(ct).ConfigureAwait(false);
            try {
                ct.ThrowIfCancellationRequested();
                if (_assignedJobs.TryGetValue(workerId, out var job)) {
                    return job;
                }
                if (_availableJobs.Count > 0 && _availableJobs.Remove(_availableJobs.First().Key, out job)) {
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
        /// Receives the heartbeat from the LegacyJobOrchestrator, JobProcess; used to control lifetime of job (cancel, restart, keep).
        /// </summary>
        /// <param name="heartbeat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat, CancellationToken ct = default) {
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
            _lockConfig.Wait();
            try {
                _publishedNodesEntries.Clear();
                _lastKnownFileHash = string.Empty;
                _lockJobs.Wait();
                try {
                    _availableJobs.Clear();
                    _assignedJobs.Clear();
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
            var sha = new SHA256Managed();
            var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(checksum).Replace("-", string.Empty);
        }

        /// <summary>
        /// Deserialize string representing published nodes file into PublishedNodesEntryModel entries and
        /// run schema validation if that is enables.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private IEnumerable<PublishedNodesEntryModel> DeserializePublishedNodes(string content) {
            bool ioErrorEncountered = false;
            if (File.Exists(_legacyCliModel.PublishedNodesSchemaFile)) {
                try {
                    using (var fileSchemaReader = new StreamReader(_legacyCliModel.PublishedNodesSchemaFile)) {
                        return _publishedNodesJobConverter.Read(content, fileSchemaReader);
                    }
                }
                catch (IOException e) {
                    _logger.Warning(e, "File IO exception when reading published nodes schema file at \"{path}\"." +
                        "Falling back to deserializing content of published nodes file without schema validation.",
                        _legacyCliModel.PublishedNodesSchemaFile);
                    ioErrorEncountered = true;
                }
            }

            // Deserialize without schema validation.
            if (!ioErrorEncountered) {
                _logger.Information("Validation schema file {PublishedNodesSchemaFile} does not exist or is disabled, " +
                    "ignoring validation of {publishedNodesFile} file.",
                    _legacyCliModel.PublishedNodesSchemaFile, _legacyCliModel.PublishedNodesFile);
            }

            return _publishedNodesJobConverter.Read(content, null);
        }

        private void RefreshJobs(IEnumerable<PublishedNodesEntryModel> entries) {
            var availableJobs = new Dictionary<string, JobProcessingInstructionModel>();
            var assignedJobs = new Dictionary<string, JobProcessingInstructionModel>();

            var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(entries, _legacyCliModel);

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
                        }
                        else {
                            availableJobs.Add(newJobId, newJob);
                        }
                    }
                }

                // Update local state.
                _availableJobs = availableJobs;
                _assignedJobs = assignedJobs;

                AdjustMaxWorkersAgentConfig();
            }
            finally {
                _lockJobs.Release();
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
                            _legacyCliModel.PublishedNodesFile,
                            _lastKnownFileHash,
                            currentFileHash);

                        _lastKnownFileHash = currentFileHash;
                        if (!string.IsNullOrEmpty(content)) {
                            List<PublishedNodesEntryModel> entries = null;

                            try {
                                entries = DeserializePublishedNodes(content).ToList();
                            }
                            catch (IOException) {
                                throw; //pass it thru, to handle retries
                            }
                            catch (SerializerException ex) {
                                _logger.Warning(ex, "Failed to deserialize {publishedNodesFile}, aborting reload...",
                                    _legacyCliModel.PublishedNodesFile);
                                _lastKnownFileHash = lastValidFileHash;
                                break;
                            }

                            // Remove entries with null or empty OpcNodes.
                            entries.RemoveAll(entry => entry.OpcNodes == null || entry.OpcNodes.Count == 0);

                            _publishedNodesEntries.Clear();
                            _publishedNodesEntries.AddRange(entries);

                            RefreshJobs(entries);
                        }
                        else {
                            _lockJobs.Wait();
                            try {
                                _availableJobs.Clear();
                                _assignedJobs.Clear();
                            }
                            finally {
                                _lockJobs.Release();
                            }

                            _publishedNodesEntries.Clear();
                        }

                        TriggerAgentConfigUpdate();
                    }
                    else {
                        //avoid double events from FileSystemWatcher
                        if (lastWriteTime - _lastRead > TimeSpan.FromMilliseconds(10)) {
                            _logger.Information("File {publishedNodesFile} has changed and content-hash" +
                                " is equal to last one, nothing to do", _legacyCliModel.PublishedNodesFile);
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
                        _legacyCliModel.PublishedNodesFile);
                    _lockJobs.Wait();
                    try {
                        _availableJobs.Clear();
                        _assignedJobs.Clear();
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
        public async Task<List<string>> PublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default) {
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
            var response = new List<string>();
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
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups, _legacyCliModel);

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
                response.Add($"Publishing succeeded for EndpointUrl: {request.EndpointUrl}");
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

            return response;
        }

        /// <inheritdoc/>
        public async Task<List<string>> UnpublishNodesAsync(PublishedNodesEntryModel request, CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered ...", nameof(UnpublishNodesAsync));
            var sw = Stopwatch.StartNew();

            if (request is null || request.OpcNodes is null || request.OpcNodes.Count == 0) {
                var message = request is null
                    ? kNullRequestMessage
                    : kNullOrEmptyOpcNodesMessage;

                _logger.Information("{nameof} method finished in {elapsed}", nameof(UnpublishNodesAsync), sw.Elapsed);
                sw.Stop();

                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }

            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            var response = new List<string>();
            try {
                // ToDo: Uncomment ValidateRequest() once our test requests pass validation.
                // This will throw SerializerException if values of request fields are not conformant.
                //ValidateRequest(request);

                // Create HashSet of nodes to remove.
                var nodesToRemoveSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                nodesToRemoveSet.UnionWith(request.OpcNodes);

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
                nodesToRemoveSet.UnionWith(request.OpcNodes);

                // Perform second pass and remove entries this time.
                var existingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in _publishedNodesEntries) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will remove nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request)) {
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
                        }

                        // Even if DataSets did not match, we need to add this entry to existingGroups
                        // so that generated job definition is complete.
                        existingGroups.Add(entry);
                    }
                }

                // Remove entries without nodes.
                _publishedNodesEntries.RemoveAll(entry => entry.OpcNodes == null || entry.OpcNodes.Count == 0);

                PersistPublishedNodes();

                var found = false;
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups, _legacyCliModel);

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
                            ToConnectionModel(request, _legacyCliModel).CreateConnectionId();
                        foreach (var assignedJob in _assignedJobs) {
                            if (entryJobId == assignedJob.Value.Job.Id) {
                                found = _assignedJobs.Remove(assignedJob.Key, out _);
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
                response.Add($"Unpublishing succeeded for EndpointUrl: {request.EndpointUrl}");
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

            return response;
        }

        /// <inheritdoc/>
        public async Task<List<string>> UnpublishAllNodesAsync(
            PublishedNodesEntryModel request,
            CancellationToken ct) {
            _logger.Information("{nameof} method triggered", nameof(UnpublishAllNodesAsync));
            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lockConfig.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> AddOrUpdateEndpointsAsync(
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

            var response = new List<string>();

            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                // ToDo: Uncomment ValidateRequest() once our test requests pass validation.
                // This will throw SerializerException if values of request fields are not conformant.
                //ValidateRequest(request);

                // First let's check that endpoints that we are asked to remove exist.
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

                    response.Add($"Update succeeded for EndpointUrl: {request[k].EndpointUrl}");
                }

                // Remove entries without nodes.
                _publishedNodesEntries.RemoveAll(entry => entry.OpcNodes == null || entry.OpcNodes.Count == 0);

                PersistPublishedNodes();

                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups, _legacyCliModel);

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
                            ToConnectionModel(dataSetToRemove, _legacyCliModel).CreateConnectionId();

                        // If entry with this JobId was already processed, then there already
                        // was an update operation for it. We will skip its removal.
                        if (!processedJobIds.Contains(entryJobId)) {
                            var jobFound = false;
                            foreach (var assignedJob in _assignedJobs) {
                                if (entryJobId == assignedJob.Value.Job.Id) {
                                    jobFound = _assignedJobs.Remove(assignedJob.Key);
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
            }
            finally {
                _lockConfig.Release();

                _logger.Information("{methodName} method finished in {elapsed}", methodName, sw.Elapsed);
                sw.Stop();
            }

            return response;
        }

        /// <inheritdoc/>
        public async Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(
            CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(GetConfiguredEndpointsAsync));
            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            var endpoints = new List<PublishedNodesEntryModel>();

            try {
                endpoints = _publishedNodesEntries.Select(model => new PublishedNodesEntryModel {
                    EndpointUrl = model.EndpointUrl,
                    UseSecurity = model.UseSecurity,
                    OpcAuthenticationMode = model.OpcAuthenticationMode,
                    OpcAuthenticationUsername = model.OpcAuthenticationUsername,
                    DataSetWriterGroup = model.DataSetWriterGroup,
                    DataSetWriterId = model.DataSetWriterId,
                    DataSetPublishingInterval = model.DataSetPublishingInterval,
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
            }
            return endpoints;
        }

        /// <inheritdoc/>
        public async Task<List<OpcNodeModel>> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request,
            CancellationToken ct = default
        ) {
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
                        response.AddRange(entry.OpcNodes);
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
        public async Task<PublishedNodesEntryModel> GetDiagnosticInfoAsync(
            PublishedNodesEntryModel request,
            CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(GetDiagnosticInfoAsync));
            await _lockConfig.WaitAsync(ct).ConfigureAwait(false);
            try {
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lockConfig.Release();
            }
        }

        private readonly static string kNullRequestMessage = "null request is provided";
        private readonly static string kNullOrEmptyOpcNodesMessage = "null or empty OpcNodes is provided in request";

        private readonly IJobSerializer _jobSerializer;
        private readonly LegacyCliModel _legacyCliModel;
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
    }
}