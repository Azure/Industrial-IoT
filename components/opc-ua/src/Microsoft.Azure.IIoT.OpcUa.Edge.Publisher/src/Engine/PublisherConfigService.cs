// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
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
    /// Provides configuration services for publisher using either published nodes
    /// configuration update or api services.
    /// </summary>
    public class PublisherConfigService : IPublisherConfigServices, IDisposable {

        /// <summary>
        /// Create publisher configuration services
        /// </summary>
        public PublisherConfigService(PublishedNodesJobConverter publishedNodesJobConverter,
            IStandaloneCliModelProvider standaloneCliModelProvider, IPublisher host,
            ILogger logger, IPublishedNodesProvider publishedNodesProvider,
            IJsonSerializer jsonSerializer) {

            _publishedNodesJobConverter = publishedNodesJobConverter ??
                throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _standaloneCliModel = standaloneCliModelProvider.StandaloneCliModel ??
                throw new ArgumentNullException(nameof(standaloneCliModelProvider));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _publishedNodesProvider = publishedNodesProvider ??
                throw new ArgumentNullException(nameof(publishedNodesProvider));
            _jsonSerializer = jsonSerializer ??
                throw new ArgumentNullException(nameof(jsonSerializer));

            _host = host;
            _lock = new SemaphoreSlim(1, 1);

            Refresh(true);
            _publishedNodesProvider.Changed += OnChanged;
            _publishedNodesProvider.Created += OnCreated;
            _publishedNodesProvider.Renamed += OnRenamed;
            _publishedNodesProvider.Deleted += OnDeleted;
            _publishedNodesProvider.EnableRaisingEvents = true;
        }

        /// <inheritdoc/>
        public async Task PublishNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered ... ", nameof(PublishNodesAsync));
            var sw = Stopwatch.StartNew();
            if (request is null || request.OpcNodes is null || request.OpcNodes.Count == 0) {
                var message = request is null ? kNullRequestMessage : kNullOrEmptyOpcNodesMessage;
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(PublishNodesAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }
            await _api.WaitAsync(ct);
            try {
                var dataSetFound = false;
                var existingGroups = new List<PublishedNodesEntryModel>();
                var currentNodes = GetCurrentPublishedNodes();
                foreach (var entry in currentNodes) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will add nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request, _standaloneCliModel.DefaultPublishingInterval)) {
                            // Create HashSet of nodes for this entry.
                            var existingNodesSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                            existingNodesSet.UnionWith(entry.OpcNodes);

                            foreach (var nodeToAdd in request.OpcNodes) {
                                if (!existingNodesSet.Contains(nodeToAdd)) {
                                    entry.OpcNodes.Add(nodeToAdd);
                                    existingNodesSet.Add(nodeToAdd);
                                }
                                else {
                                    _logger.Debug("Node \"{node}\" is already present " +
                                        "for entry with \"{endpoint}\" endpoint.",
                                        nodeToAdd.Id, entry.EndpointUrl);
                                }
                            }
                            // refresh the Tag if a new one is provided
                            dataSetFound = true;
                        }
                        existingGroups.Add(entry);
                    }
                }
                if (!dataSetFound) {
                    existingGroups.Add(request);
                }
                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups,
                    _standaloneCliModel);
                await _host.UpdateAsync(jobs);
                PersistPublishedNodes();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _api.Release();
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(PublishNodesAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered ...", nameof(UnpublishNodesAsync));
            var sw = Stopwatch.StartNew();
            if (request is null) {
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(UnpublishNodesAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    kNullRequestMessage);
            }

            //
            // When no node is specified then remove the whole data set.
            // This behavior ensures backwards compatibility with UnpublishNodes
            // direct method of OPC Publisher 2.5.x.
            //
            var purgeDataSet = request.OpcNodes is null || request.OpcNodes.Count == 0;
            await _api.WaitAsync(ct);
            try {
                // ToDo: Uncomment ValidateRequest() once our test requests pass validation.
                // This will throw SerializerException if values of request fields are not conformant.
                //ValidateRequest(request);

                // Create HashSet of nodes to remove.
                var nodesToRemoveSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                if (!purgeDataSet) {
                    nodesToRemoveSet.UnionWith(request.OpcNodes);
                }
                var currentNodes = GetCurrentPublishedNodes();
                // Perform first pass to determine if we can find all nodes to remove.
                var matchingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in currentNodes) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will remove nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request, _standaloneCliModel.DefaultPublishingInterval)) {
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
                    throw new MethodCallStatusException(entriesNotFoundJson,
                        (int)HttpStatusCode.NotFound, "Nodes not found");
                }

                // Create HashSet of nodes to remove again for the second pass.
                nodesToRemoveSet.Clear();
                if (!purgeDataSet) {
                    nodesToRemoveSet.UnionWith(request.OpcNodes);
                }

                // Perform second pass and remove entries this time.
                var existingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in currentNodes) {
                    if (entry.HasSameGroup(request)) {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will remove nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request, _standaloneCliModel.DefaultPublishingInterval)) {
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

                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups,
                    _standaloneCliModel);
                await _host.UpdateAsync(jobs);
                PersistPublishedNodes();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _api.Release();
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(UnpublishNodesAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishAllNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct) {
            _logger.Information("{nameof} method triggered", nameof(UnpublishAllNodesAsync));
            //
            // when no endpoint is specified remove all the configuration
            // purge content feature is implemented to ensure the backwards compatibility
            // with V2.5.x of the publisher
            //
            var purge = null == request?.EndpointUrl;
            var sw = Stopwatch.StartNew();
            await _api.WaitAsync(ct);
            try {
                var currentNodes = GetCurrentPublishedNodes();
                if (!purge) {
                    var found = false;
                    // Perform pass to determine existing groups
                    var matchingGroups = new List<PublishedNodesEntryModel>();
                    foreach (var entry in currentNodes) {
                        if (entry.HasSameGroup(request)) {
                            // We may have several entries with the same DataSetGroup definition,
                            // so we will remove nodes only if the whole DataSet definition matches.
                            if (entry.HasSameDataSet(request, _standaloneCliModel.DefaultPublishingInterval)) {
                                entry.OpcNodes.Clear();
                                found = true;
                            }
                            matchingGroups.Add(entry);
                        }
                    }

                    // Report error if there were entries that did not have any nodes
                    if (!found) {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                            $"Endpoint or node not found: {request.EndpointUrl}");
                    }

                    var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(matchingGroups,
                        _standaloneCliModel);
                    await _host.UpdateAsync(jobs);
                }
                else {
                    await _host.UpdateAsync(Enumerable.Empty<WriterGroupJobModel>());
                }
                PersistPublishedNodes();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _api.Release();
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(UnpublishAllNodesAsync), sw.Elapsed);
                sw.Stop();
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
                    if (request[itemIndex].HasSameDataSet(request[prevItemIndex],
                            _standaloneCliModel.DefaultPublishingInterval)) {
                        throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                            $"Request contains two entries for the same endpoint at index {prevItemIndex} and {itemIndex}");
                    }
                }
            }

            await _api.WaitAsync(ct);
            try {
                // Check that endpoints that we are asked to remove exist.
                var dataSetsToRemove = request.Where(e => e.OpcNodes is null || e.OpcNodes.Count == 0).ToList();
                var currentNodes = GetCurrentPublishedNodes();
                foreach (var dataSetToRemove in dataSetsToRemove) {
                    var foundDataSet = false;
                    foreach (var entry in currentNodes) {
                        foundDataSet = foundDataSet ||
                            entry.HasSameDataSet(dataSetToRemove, _standaloneCliModel.DefaultPublishingInterval);
                    }
                    if (!foundDataSet) {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                            $"Endpoint not found: {dataSetToRemove.EndpointUrl}");
                    }
                }

                var existingGroups = new List<PublishedNodesEntryModel>();
                var requestDataSetsFound = Enumerable.Repeat(false, request.Count).ToList(); ;

                foreach (var entry in currentNodes) {
                    var groupFound = false;

                    for (var k = 0; k < request.Count; ++k) {
                        var dataSetToUpdate = request[k];
                        if (entry.HasSameGroup(dataSetToUpdate)) {
                            groupFound = true;

                            // We may have several entries with the same DataSetGroup definition,
                            // so we will update nodes only if the whole DataSet definition matches.
                            if (entry.HasSameDataSet(dataSetToUpdate, _standaloneCliModel.DefaultPublishingInterval)) {
                                if (dataSetToUpdate.OpcNodes is null ||
                                    dataSetToUpdate.OpcNodes.Count == 0 ||
                                    requestDataSetsFound[k]) {
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
                    }
                }

                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(existingGroups,
                    _standaloneCliModel);
                await _host.UpdateAsync(jobs);
                PersistPublishedNodes();
            }
            finally {
                _api.Release();
                _logger.Information("{methodName} method finished in {elapsed}",
                    methodName, sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(
            CancellationToken ct = default) {

            var methodName = nameof(GetConfiguredEndpointsAsync);
            _logger.Information("{nameof} method triggered", methodName);
            var sw = Stopwatch.StartNew();

            var currentNodes = GetCurrentPublishedNodes();
            var endpoints = new List<PublishedNodesEntryModel>();
            try {
                endpoints = currentNodes.Select(model => new PublishedNodesEntryModel {
                    EndpointUrl = model.EndpointUrl,
                    Version = model.Version,
                    LastChange = model.LastChange,
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
                    DataSetPublishingIntervalTimespan = model.DataSetPublishingIntervalTimespan,
                    DataSetPublishingInterval = !model.DataSetPublishingIntervalTimespan.HasValue
                        ? model.DataSetPublishingInterval
                        : null,
                })
                .ToList();
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    e.Message);
            }
            finally {
                _logger.Information("{methodName} method finished in {elapsed}",
                    methodName, sw.Elapsed);
                sw.Stop();
            }
            return Task.FromResult(endpoints);
        }

        private IEnumerable<PublishedNodesEntryModel> GetCurrentPublishedNodes() {
            return _publishedNodesJobConverter.ToPublishedNodes(_host.Version, _host.LastChange,
                _host.WriterGroups, _standaloneCliModel);
        }

        /// <inheritdoc/>
        public Task<List<OpcNodeModel>> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request,
            CancellationToken ct = default) {

            _logger.Information("{nameof} method triggered",
                nameof(GetConfiguredNodesOnEndpointAsync));
            var sw = Stopwatch.StartNew();

            if (request is null) {
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(GetConfiguredNodesOnEndpointAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    kNullRequestMessage);
            }

            List<OpcNodeModel> response = new List<OpcNodeModel>();
            try {
                var endpointFound = false;
                var currentNodes = GetCurrentPublishedNodes();
                foreach (var entry in currentNodes) {
                    if (entry.HasSameDataSet(request, _standaloneCliModel.DefaultPublishingInterval)) {
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
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    e.Message);
            }
            finally {
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(GetConfiguredNodesOnEndpointAsync), sw.Elapsed);
                sw.Stop();
            }
            return Task.FromResult(response);
        }

        /// <inheritdoc/>
        public Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default) {
            _logger.Information("{nameof} method triggered", nameof(GetDiagnosticInfoAsync));
            var sw = Stopwatch.StartNew();
            try {
                return Task.FromResult(_host.DiagnosticInfo.ToList());
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally {
                _logger.Information("{nameof} method finished in {elapsed}",
                    nameof(GetDiagnosticInfoAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _publishedNodesProvider.EnableRaisingEvents = false;
            _publishedNodesProvider.Changed -= OnChanged;
            _publishedNodesProvider.Created -= OnCreated;
            _publishedNodesProvider.Renamed -= OnRenamed;
            _publishedNodesProvider.Deleted -= OnDeleted;
        }

        /// <summary>
        /// Handle file changes
        /// </summary>
        private void OnChanged(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} changed. Triggering file refresh ...",
                _standaloneCliModel.PublishedNodesFile);
            Refresh();
        }

        /// <summary>
        /// Handle creation of file
        /// </summary>
        private void OnCreated(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} created. Triggering file refresh ...",
                _standaloneCliModel.PublishedNodesFile);
            Refresh();
        }

        /// <summary>
        /// Handle removal of file
        /// </summary>
        private void OnRenamed(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} renamed. Triggering file refresh ...",
                _standaloneCliModel.PublishedNodesFile);
            Refresh();
        }

        /// <summary>
        /// Handle deletion of the file
        /// </summary>
        private void OnDeleted(object sender, FileSystemEventArgs e) {
            _logger.Debug("File {publishedNodesFile} deleted. Clearing configuration ...",
                _standaloneCliModel.PublishedNodesFile);
            Clear();
        }

        /// <summary>
        /// Create a checksum of the content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
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
                    using (var fileSchemaReader = new StreamReader(
                        _standaloneCliModel.PublishedNodesSchemaFile)) {
                        return _publishedNodesJobConverter.Read(content, fileSchemaReader);
                    }
                }
                catch (IOException e) {
                    _logger.Warning(e, "File IO exception when reading published nodes schema file " +
                        "at \"{path}\". Falling back to deserializing content of published nodes " +
                        "file without schema validation.",
                        _standaloneCliModel.PublishedNodesSchemaFile);
                    ioErrorEncountered = true;
                }
            }
            // Deserialize without schema validation.
            if (!ioErrorEncountered) {
                _logger.Information("Validation schema file {PublishedNodesSchemaFile} does not " +
                    "exist or is disabled, ignoring validation of {publishedNodesFile} file.",
                    _standaloneCliModel.PublishedNodesSchemaFile, _standaloneCliModel.PublishedNodesFile);
            }
            return _publishedNodesJobConverter.Read(content, null);
        }

        /// <summary>
        /// Transforms legacy entries that use NodeId into ones using OpcNodes.
        /// The transformation will happen in-place.
        /// </summary>
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
                        throw new SerializerException(
                            $"Published nodes file contains DataSetWriter entry which " +
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

        /// <summary>
        /// Clear configuration
        /// </summary>
        private void Clear() {
            lock (_lock) {
                _lastKnownFileHash = string.Empty;
                _host.TryUpdate(Enumerable.Empty<WriterGroupJobModel>());
            }
        }

        /// <summary>
        /// Refresh provider
        /// </summary>
        private void Refresh(bool wait = false) {
            var retryCount = 3;
            var lastWriteTime = _publishedNodesProvider.GetLastWriteTime();
            lock (_lock) {
                while (true) {
                    try {
                        var content = _publishedNodesProvider.ReadContent();
                        var lastValidFileHash = _lastKnownFileHash;
                        var currentFileHash = GetChecksum(content);

                        if (currentFileHash != _lastKnownFileHash) {
                            _logger.Information("File {publishedNodesFile} has changed, " +
                                "last known hash {LastHash}, new hash {NewHash}, reloading...",
                                _standaloneCliModel.PublishedNodesFile, _lastKnownFileHash,
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
                                    _logger.Warning(ex,
                                        "Failed to deserialize {publishedNodesFile}, aborting reload...",
                                        _standaloneCliModel.PublishedNodesFile);
                                    _lastKnownFileHash = lastValidFileHash;
                                    break;
                                }
                                var jobs = _publishedNodesJobConverter.ToWriterGroupJobs(entries,
                                    _standaloneCliModel);
                                if (wait) {
                                    try {
                                        // Block until update has completed.
                                        _host.UpdateAsync(jobs).GetAwaiter().GetResult();
                                    }
                                    catch (Exception ex) {
                                        _logger.Error(ex,
                                            "Error during publisher initialization. Retrying.");
                                        // Try again
                                        continue;
                                    }
                                }
                                else if (!_host.TryUpdate(jobs)) {
                                    // Should not happen
                                    continue; // Try again
                                }
                            }
                            else {
                                _lastKnownFileHash = string.Empty;
                                if (!_host.TryUpdate(Enumerable.Empty<WriterGroupJobModel>())) {
                                    continue;
                                }
                            }
                        }
                        else {
                            // avoid double events from FileSystemWatcher
                            if (lastWriteTime - _lastRead > TimeSpan.FromMilliseconds(10)) {
                                _logger.Information("File {publishedNodesFile} has changed and h" +
                                    "content-has is equal to last one, nothing to do...",
                                    _standaloneCliModel.PublishedNodesFile);
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
                        _logger.Error(e,
                            "Error while reloading {PublishedNodesFile}. Resetting the configuration.",
                            _standaloneCliModel.PublishedNodesFile);
                        _lastKnownFileHash = string.Empty;
                        _host.TryUpdate(Enumerable.Empty<WriterGroupJobModel>());
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Persist Published nodes to published nodes file.
        /// </summary>
        private void PersistPublishedNodes() {
            lock (_lock) {
                var currentNodes = GetCurrentPublishedNodes();
                var updatedContent = _jsonSerializer.SerializeToString(currentNodes,
                    SerializeOption.Indented);
                _publishedNodesProvider.WriteContent(updatedContent, true);
                // Update _lastKnownFileHash
                _lastKnownFileHash = GetChecksum(updatedContent);
            }
        }

        private readonly static string kNullRequestMessage
            = "null request is provided";
        private readonly static string kNullOrEmptyOpcNodesMessage
            = "null or empty OpcNodes is provided in request";

        private readonly ILogger _logger;
        private readonly StandaloneCliModel _standaloneCliModel;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly IPublishedNodesProvider _publishedNodesProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IPublisher _host;
        private string _lastKnownFileHash = string.Empty;
        private DateTime _lastRead = DateTime.MinValue;
        private SemaphoreSlim _api = new SemaphoreSlim(1, 1);
        private readonly object _lock = new object();
    }
}
