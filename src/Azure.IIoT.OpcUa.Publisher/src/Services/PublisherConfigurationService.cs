// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Autofac;
    using Furly;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides configuration services for publisher using either published nodes
    /// configuration update or api services.
    /// </summary>
    public sealed class PublisherConfigurationService : IConfigurationServices,
        IAwaitable<PublisherConfigurationService>, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Create publisher configuration services
        /// </summary>
        /// <param name="publishedNodesJobConverter"></param>
        /// <param name="configuration"></param>
        /// <param name="publisherHost"></param>
        /// <param name="logger"></param>
        /// <param name="publishedNodesProvider"></param>
        /// <param name="jsonSerializer"></param>
        /// <param name="diagnostics"></param>
        public PublisherConfigurationService(PublishedNodesConverter publishedNodesJobConverter,
            IOptions<PublisherOptions> configuration, IPublisher publisherHost,
            ILogger<PublisherConfigurationService> logger, IStorageProvider publishedNodesProvider,
            IJsonSerializer jsonSerializer, IDiagnosticCollector? diagnostics = null)
        {
            _publishedNodesJobConverter = publishedNodesJobConverter ??
                throw new ArgumentNullException(nameof(publishedNodesJobConverter));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _publishedNodesProvider = publishedNodesProvider ??
                throw new ArgumentNullException(nameof(publishedNodesProvider));
            _jsonSerializer = jsonSerializer ??
                throw new ArgumentNullException(nameof(jsonSerializer));
            _publisherHost = publisherHost ??
                throw new ArgumentNullException(nameof(publisherHost));
            _diagnostics = diagnostics; // Optional
            _started = new TaskCompletionSource();
            _fileChanges = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _fileChangeProcessor = Task.Factory.StartNew(ProcessFileChangesAsync,
                default, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _fileChanges.Writer.TryWrite(false); // Read from file
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> PublishStartAsync(ConnectionModel endpoint,
            PublishStartRequestModel request, CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered...", nameof(PublishStartAsync));
            var sw = Stopwatch.StartNew();
            if (request?.Item is null)
            {
                var message = request is null ? kNullRequestMessage : kNullOrEmptyOpcNodesMessage;
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishStartAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var entry = endpoint.ToPublishedNodesEntry();
                var currentNodes = GetCurrentPublishedNodes().ToList();
                AddItem(currentNodes, entry, request.Item);
                var jobs = _publishedNodesJobConverter.ToWriterGroups(currentNodes,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
                return new PublishStartResponseModel();
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishStartAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> PublishStopAsync(ConnectionModel endpoint,
            PublishStopRequestModel request, CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered...", nameof(PublishStopAsync));
            var sw = Stopwatch.StartNew();
            if (request?.NodeId is null)
            {
                var message = request is null ? kNullRequestMessage : kNullOrEmptyOpcNodesMessage;
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishStopAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var currentNodes = GetCurrentPublishedNodes().ToList();
                var entry = endpoint.ToPublishedNodesEntry();
                foreach (var nodeset in currentNodes.Where(n => n.HasSameDataSet(entry)))
                {
                    nodeset.OpcNodes = nodeset.OpcNodes?.Where(n => n.Id != request.NodeId).ToList();
                }
                var jobs = _publishedNodesJobConverter.ToWriterGroups(currentNodes,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
                return new PublishStopResponseModel();
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishStopAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> PublishBulkAsync(ConnectionModel endpoint,
            PublishBulkRequestModel request, CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered...", nameof(PublishBulkAsync));
            var sw = Stopwatch.StartNew();
            if (request?.NodesToAdd is null && request?.NodesToRemove is null)
            {
                var message = request is null ? kNullRequestMessage : kNullOrEmptyOpcNodesMessage;
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishBulkAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var currentNodes = GetCurrentPublishedNodes().ToList();
                var entry = endpoint.ToPublishedNodesEntry();
                // Remove all nodes
                if (request.NodesToRemove != null)
                {
                    foreach (var nodeset in currentNodes.Where(n => n.HasSameDataSet(entry)))
                    {
                        nodeset.OpcNodes = nodeset.OpcNodes?
                            .Where(n => !request.NodesToRemove.Contains(n.Id!))
                            .ToList();
                    }
                }
                if (request.NodesToAdd != null)
                {
                    foreach (var item in request.NodesToAdd)
                    {
                        AddItem(currentNodes, entry, item);
                    }
                }
                var jobs = _publishedNodesJobConverter.ToWriterGroups(currentNodes,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
                return new PublishBulkResponseModel();
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishBulkAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> PublishListAsync(ConnectionModel endpoint,
            PublishedItemListRequestModel request, CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered...", nameof(PublishListAsync));
            var sw = Stopwatch.StartNew();
            if (request is null)
            {
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishListAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, kNullRequestMessage);
            }
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var entry = endpoint.ToPublishedNodesEntry();
                var existingGroups = new List<PublishedNodesEntryModel>();
                return new PublishedItemListResponseModel
                {
                    Items = GetCurrentPublishedNodes()
                        .Where(n => n.HasSameDataSet(entry))
                        .SelectMany(n => n.OpcNodes ?? new List<OpcNodeModel>())
                        .Where(n => n.EventFilter == null) // Exclude event filtering
                        .Select(n => new PublishedItemModel
                        {
                            NodeId = n.Id,
                            DisplayName = n.DisplayName,
                            HeartbeatInterval = n.HeartbeatIntervalTimespan,
                            PublishingInterval = n.OpcPublishingIntervalTimespan,
                            SamplingInterval = n.OpcSamplingIntervalTimespan
                        })
                        .ToList()
                };
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishListAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task PublishNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered ...", nameof(PublishNodesAsync));
            var sw = Stopwatch.StartNew();
            if (request is null || request.OpcNodes is null || request.OpcNodes.Count == 0)
            {
                var message = request is null ? kNullRequestMessage : kNullOrEmptyOpcNodesMessage;
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishNodesAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, message);
            }
            request.PropagatePublishingIntervalToNodes();
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var dataSetFound = false;
                var existingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in GetCurrentPublishedNodes())
                {
                    if (entry.HasSameGroup(request))
                    {
                        // We may have several entries with the same DataSetGroup definition,
                        // so we will add nodes only if the whole DataSet definition matches.
                        if (entry.HasSameDataSet(request))
                        {
                            // Create HashSet of nodes for this entry.
                            var existingNodesSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                            entry.OpcNodes ??= new List<OpcNodeModel>();
                            existingNodesSet.UnionWith(entry.OpcNodes);

                            foreach (var nodeToAdd in request.OpcNodes)
                            {
                                if (!existingNodesSet.Contains(nodeToAdd))
                                {
                                    entry.OpcNodes.Add(nodeToAdd);
                                    existingNodesSet.Add(nodeToAdd);
                                }
                                else
                                {
                                    _logger.LogDebug("Node \"{Node}\" is already present " +
                                        "for entry with \"{Endpoint}\" endpoint.",
                                        nodeToAdd.Id, entry.EndpointUrl);
                                }
                            }
                            dataSetFound = true;
                        }
                        existingGroups.Add(entry);
                    }
                }
                if (!dataSetFound)
                {
                    existingGroups.Add(request);
                }
                var jobs = _publishedNodesJobConverter.ToWriterGroups(existingGroups,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(PublishNodesAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered ...", nameof(UnpublishNodesAsync));
            var sw = Stopwatch.StartNew();
            if (request is null)
            {
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
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
            request.PropagatePublishingIntervalToNodes();
            var purgeDataSet = request.OpcNodes is null || request.OpcNodes.Count == 0;
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Create HashSet of nodes to remove.
                var nodesToRemoveSet = new HashSet<OpcNodeModel>(OpcNodeModelEx.Comparer);
                if (!purgeDataSet && request.OpcNodes != null)
                {
                    nodesToRemoveSet.UnionWith(request.OpcNodes);
                }
                var currentNodes = GetCurrentPublishedNodes().ToList();
                // Perform first pass to determine if we can find all nodes to remove.
                var matchingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in currentNodes)
                {
                    // We may have several entries with the same DataSetGroup definition,
                    // so we will remove nodes only if the whole DataSet definition matches.
                    if (entry.HasSameDataSet(request))
                    {
                        if (entry.OpcNodes != null)
                        {
                            foreach (var node in entry.OpcNodes)
                            {
                                nodesToRemoveSet.Remove(node);
                            }
                        }
                        matchingGroups.Add(entry);
                    }
                }

                // Report error if no matching endpoint was found.
                if (matchingGroups.Count == 0)
                {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                        $"Endpoint not found: {request.EndpointUrl}");
                }

                // Report error if there were entries that we were not able to find.
                if (nodesToRemoveSet.Count != 0)
                {
                    request.OpcNodes = nodesToRemoveSet.ToList();
                    var entriesNotFoundJson = _jsonSerializer.SerializeToMemory(request);
                    throw new MethodCallStatusException(entriesNotFoundJson,
                        (int)HttpStatusCode.NotFound, "Nodes not found");
                }

                // Create HashSet of nodes to remove again for the second pass.
                nodesToRemoveSet.Clear();
                if (!purgeDataSet && request.OpcNodes != null)
                {
                    nodesToRemoveSet.UnionWith(request.OpcNodes);
                }

                // Perform second pass and remove entries this time.
                var existingGroups = new List<PublishedNodesEntryModel>();
                foreach (var entry in currentNodes)
                {
                    // We may have several entries with the same DataSetGroup definition,
                    // so we will remove nodes only if the whole DataSet definition matches.
                    if (entry.HasSameDataSet(request))
                    {
                        if (!purgeDataSet)
                        {
                            var updatedNodes = new List<OpcNodeModel>();

                            if (entry.OpcNodes != null)
                            {
                                foreach (var node in entry.OpcNodes)
                                {
                                    if (!nodesToRemoveSet.Remove(node))
                                    {
                                        updatedNodes.Add(node);
                                    }
                                }
                            }

                            entry.OpcNodes = updatedNodes;
                        }
                        else
                        {
                            entry.OpcNodes?.Clear();
                        }
                    }

                    // Even if DataSets did not match, we need to add this entry to existingGroups
                    // so that generated job definition is complete.
                    existingGroups.Add(entry);
                }

                var jobs = _publishedNodesJobConverter.ToWriterGroups(existingGroups,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(UnpublishNodesAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishAllNodesAsync(PublishedNodesEntryModel request,
            CancellationToken ct)
        {
            _logger.LogInformation("{Method} method triggered...", nameof(UnpublishAllNodesAsync));
            //
            // when no endpoint is specified remove all the configuration
            // purge content feature is implemented to ensure the backwards compatibility
            // with V2.5.x of the publisher
            //
            var purge = request.EndpointUrl == null;
            request.PropagatePublishingIntervalToNodes();
            var sw = Stopwatch.StartNew();
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!purge)
                {
                    var found = false;
                    // Perform pass to determine existing groups
                    var matchingGroups = new List<PublishedNodesEntryModel>();
                    foreach (var entry in GetCurrentPublishedNodes())
                    {
                        if (entry.HasSameGroup(request))
                        {
                            // We may have several entries with the same DataSetGroup definition,
                            // so we will remove nodes only if the whole DataSet definition matches.
                            if (entry.HasSameDataSet(request))
                            {
                                entry.OpcNodes?.Clear();
                                found = true;
                            }
                            matchingGroups.Add(entry);
                        }
                    }

                    // Report error if there were entries that did not have any nodes
                    if (!found)
                    {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                            $"Endpoint or node not found: {request.EndpointUrl}");
                    }

                    var jobs = _publishedNodesJobConverter.ToWriterGroups(matchingGroups,
                        _configuration.Value);
                    await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                }
                else
                {
                    await _publisherHost.UpdateAsync(Enumerable.Empty<WriterGroupModel>()).ConfigureAwait(false);
                }
                await PersistPublishedNodesAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(UnpublishAllNodesAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task SetConfiguredEndpointsAsync(IReadOnlyList<PublishedNodesEntryModel> request,
            CancellationToken ct = default)
        {
            const string methodName = nameof(SetConfiguredEndpointsAsync);
            _logger.LogInformation("{Method} method triggered...", methodName);
            var sw = Stopwatch.StartNew();

            if (request is null)
            {
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    methodName, sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    kNullRequestMessage);
            }
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var jobs = _publishedNodesJobConverter.ToWriterGroups(request,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    methodName, sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task AddOrUpdateEndpointsAsync(IReadOnlyList<PublishedNodesEntryModel> request,
            CancellationToken ct = default)
        {
            const string methodName = nameof(AddOrUpdateEndpointsAsync);
            _logger.LogInformation("{Method} method triggered...", methodName);
            var sw = Stopwatch.StartNew();

            if (request is null)
            {
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    methodName, sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    kNullRequestMessage);
            }

            // First, let's check that there are no 2 entries for the same endpoint in the request.
            if (request.Count > 0)
            {
                request[0].PropagatePublishingIntervalToNodes();
                for (var itemIndex = 1; itemIndex < request.Count; itemIndex++)
                {
                    for (var prevItemIndex = 0; prevItemIndex < itemIndex; prevItemIndex++)
                    {
                        request[itemIndex].PropagatePublishingIntervalToNodes();
                        if (request[itemIndex].HasSameDataSet(request[prevItemIndex]))
                        {
                            throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                                "Request contains two entries for the same endpoint " +
                                $"at index {prevItemIndex} and {itemIndex}");
                        }
                    }
                }
            }

            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var currentNodes = GetCurrentPublishedNodes().ToHashSet();
                // Check that endpoints that we are asked to remove exist.
                foreach (var removeRequest in request.Where(e => (e.OpcNodes?.Count ?? 0) == 0))
                {
                    var removed = currentNodes.RemoveWhere(entry => entry.HasSameDataSet(removeRequest));
                    if (removed == 0)
                    {
                        throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                            $"Endpoint not found: {removeRequest.EndpointUrl}");
                    }
                }
                foreach (var updateRequest in request.Where(e => e.OpcNodes?.Count > 0))
                {
                    // We will add the update request entry and clean up anything else matching.
                    var found = currentNodes.FirstOrDefault(entry => entry.HasSameDataSet(updateRequest));
                    if (found != null)
                    {
                        currentNodes.RemoveWhere(entry => entry.HasSameDataSet(updateRequest));
                        found.OpcNodes = updateRequest.OpcNodes;
                        currentNodes.Add(found);
                    }
                    else
                    {
                        // Nothing matching add here
                        currentNodes.Add(updateRequest);
                    }
                }
                var jobs = _publishedNodesJobConverter.ToWriterGroups(currentNodes,
                    _configuration.Value);
                await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                await PersistPublishedNodesAsync().ConfigureAwait(false);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    methodName, sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<List<PublishedNodesEntryModel>> GetConfiguredEndpointsAsync(
            bool includeNodes = false, CancellationToken ct = default)
        {
            const string methodName = nameof(GetConfiguredEndpointsAsync);
            _logger.LogInformation("{Method} method triggered...", methodName);
            var sw = Stopwatch.StartNew();
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var endpoints = GetCurrentPublishedNodes();
                if (!includeNodes)
                {
                    endpoints = endpoints.Select(e => e.ToDataSetEntry());
                }
                return endpoints.ToList();
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    methodName, sw.Elapsed);
                sw.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<List<OpcNodeModel>> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered...",
                nameof(GetConfiguredNodesOnEndpointAsync));
            var sw = Stopwatch.StartNew();

            if (request is null)
            {
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(GetConfiguredNodesOnEndpointAsync), sw.Elapsed);
                sw.Stop();
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    kNullRequestMessage);
            }

            request.PropagatePublishingIntervalToNodes();
            var response = new List<OpcNodeModel>();
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var endpointFound = false;
                foreach (var entry in GetCurrentPublishedNodes())
                {
                    if (entry.HasSameDataSet(request))
                    {
                        endpointFound = true;
                        if (entry.OpcNodes != null)
                        {
                            response.AddRange(entry.OpcNodes);
                        }
                    }
                }

                if (!endpointFound)
                {
                    throw new MethodCallStatusException((int)HttpStatusCode.NotFound,
                        $"Endpoint not found: {request.EndpointUrl}");
                }
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest,
                    e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(GetConfiguredNodesOnEndpointAsync), sw.Elapsed);
                sw.Stop();
            }
            return response;
        }

        /// <inheritdoc/>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default)
        {
            _logger.LogInformation("{Method} method triggered...", nameof(GetDiagnosticInfoAsync));
            var sw = Stopwatch.StartNew();
            await _api.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var result = new List<PublishDiagnosticInfoModel>();
                if (_diagnostics == null)
                {
                    // Diagnostics disabled
                    throw new MethodCallStatusException((int)HttpStatusCode.ServiceUnavailable,
                        "Diagnostics service is disabled.");
                }
                foreach (var nodes in GetCurrentPublishedNodes())
                {
                    if (!_diagnostics.TryGetDiagnosticsForWriterGroup(
                        nodes.DataSetWriterGroup ?? Constants.DefaultWriterGroupId, out var model))
                    {
                        continue;
                    }
                    result.Add(new PublishDiagnosticInfoModel
                    {
                        Endpoint = nodes,
                        SentMessagesPerSec = model.SentMessagesPerSec,
                        IngestionDuration = DateTime.UtcNow - model.IngestionStart,
                        IngressDataChanges = model.IngressDataChanges,
                        IngressValueChanges = model.IngressValueChanges,
                        IngressBatchBlockBufferSize = model.IngressBatchBlockBufferSize,
                        EncodingBlockInputSize = model.EncodingBlockInputSize,
                        EncodingBlockOutputSize = model.EncodingBlockOutputSize,
                        EncoderNotificationsProcessed = model.EncoderNotificationsProcessed,
                        EncoderNotificationsDropped = model.EncoderNotificationsDropped,
                        EncoderMaxMessageSplitRatio = model.EncoderMaxMessageSplitRatio,
                        EncoderIoTMessagesProcessed = model.EncoderIoTMessagesProcessed,
                        EncoderAvgNotificationsMessage = model.EncoderAvgNotificationsMessage,
                        EncoderAvgIoTMessageBodySize = model.EncoderAvgIoTMessageBodySize,
                        EncoderAvgIoTChunkUsage = model.EncoderAvgIoTChunkUsage,
                        EstimatedIoTChunksPerDay = model.EstimatedIoTChunksPerDay,
                        OutgressInputBufferCount = model.OutgressInputBufferCount,
                        OutgressInputBufferDropped = model.OutgressInputBufferDropped,
                        OutgressIoTMessageCount = model.OutgressIoTMessageCount,
                        ConnectionRetries = model.ConnectionRetries,
                        OpcEndpointConnected = model.OpcEndpointConnected,
                        MonitoredOpcNodesSucceededCount = model.MonitoredOpcNodesSucceededCount,
                        MonitoredOpcNodesFailedCount = model.MonitoredOpcNodesFailedCount,
                        IngressEventNotifications = model.IngressEventNotifications,
                        IngressCyclicReads = model.IngressCyclicReads,
                        IngressHeartbeats = model.IngressHeartbeats,
                        IngressDataChangesInLastMinute = model.IngressDataChangesInLastMinute,
                        IngressValueChangesInLastMinute = model.IngressValueChangesInLastMinute,
                        IngressEvents = model.IngressEvents
                    });
                }
                return result;
            }
            catch (Exception e) when (e is not MethodCallStatusException)
            {
                _logger.LogError(e, "Failed to get diagnostics information.");
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                _api.Release();
                _logger.LogInformation("{Method} method finished in {Elapsed}.",
                    nameof(GetDiagnosticInfoAsync), sw.Elapsed);
                sw.Stop();
            }
        }

        /// <summary>
        /// Persist Published nodes to published nodes file.
        /// </summary>
        private async Task PersistPublishedNodesAsync()
        {
            await _file.WaitAsync().ConfigureAwait(false);
            try
            {
                var currentNodes = GetCurrentPublishedNodes(preferTimespan: false);
                var updatedContent = _jsonSerializer.SerializeToString(
                    currentNodes, SerializeOption.Indented) ?? string.Empty;

                _publishedNodesProvider.WriteContent(updatedContent, true);
                // Update _lastKnownFileHash
                _lastKnownFileHash = GetChecksum(updatedContent);
            }
            finally
            {
                _file.Release();
            }
        }

        /// <summary>
        /// Get current published nodes from publisher
        /// </summary>
        /// <param name="preferTimespan"></param>
        /// <returns></returns>
        private IEnumerable<PublishedNodesEntryModel> GetCurrentPublishedNodes(
            bool preferTimespan = true)
        {
            return _publishedNodesJobConverter
                .ToPublishedNodes(_publisherHost.Version, _publisherHost.LastChange,
                    _publisherHost.WriterGroups, preferTimespan)
                .Select(p => p.PropagatePublishingIntervalToNodes());
        }

        /// <inheritdoc/>
        public IAwaiter<PublisherConfigurationService> GetAwaiter()
        {
            return (_started?.Task ?? Task.CompletedTask).AsAwaiter(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                _fileChanges.Writer.TryComplete();
                if (_started != null)
                {
                    await _fileChangeProcessor.ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            finally
            {
                _started = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _fileChanges.Writer.TryComplete();
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                _api.Dispose();
                _file.Dispose();
            }
        }

        /// <summary>
        /// Handle file changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object? sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("File {File} changed. Triggering file refresh ...",
                e.Name);
            _fileChanges.Writer.TryWrite(false);
        }

        /// <summary>
        /// Handle creation of file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCreated(object? sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("File {File} created. Triggering file refresh ...",
                e.Name);
            _fileChanges.Writer.TryWrite(false);
        }

        /// <summary>
        /// Handle removal of file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRenamed(object? sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("File {File} renamed. Triggering file refresh ...",
                e.Name);
            _fileChanges.Writer.TryWrite(false);
        }

        /// <summary>
        /// Handle deletion of the file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeleted(object? sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("File {File} deleted. Clearing configuration ...",
                e.Name);
            _fileChanges.Writer.TryWrite(true);
        }

        /// <summary>
        /// Refresh processor
        /// </summary>
        /// <returns></returns>
        private async Task ProcessFileChangesAsync()
        {
            try
            {
                _publishedNodesProvider.Changed += OnChanged;
                _publishedNodesProvider.Created += OnCreated;
                _publishedNodesProvider.Renamed += OnRenamed;
                _publishedNodesProvider.Deleted += OnDeleted;
                _publishedNodesProvider.EnableRaisingEvents = true;

                var retryCount = 0;
                await foreach (var clear in _fileChanges.Reader.ReadAllAsync())
                {
                    await _file.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var lastWriteTime = _publishedNodesProvider.GetLastWriteTime();
                        try
                        {
                            // Force empty content when clearing but dont touch the deleted file
                            var content = clear ? string.Empty : _publishedNodesProvider.ReadContent();
                            var lastValidFileHash = _lastKnownFileHash;
                            var currentFileHash = GetChecksum(content);

                            if (currentFileHash != _lastKnownFileHash)
                            {
                                var jobs = Enumerable.Empty<WriterGroupModel>();
                                if (!clear && !string.IsNullOrEmpty(content))
                                {
                                    if (string.IsNullOrEmpty(_lastKnownFileHash))
                                    {
                                        _logger.LogInformation(
                                            "Found published Nodes File with hash {NewHash}, loading...",
                                            currentFileHash);
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Published Nodes File changed, " +
                                            "last known hash {LastHash}, new hash {NewHash}, reloading...",
                                            _lastKnownFileHash, currentFileHash);
                                    }

                                    var entries = _publishedNodesJobConverter.Read(content).ToList();
                                    TransformFromLegacyNodeId(entries);
                                    jobs = _publishedNodesJobConverter.ToWriterGroups(entries,
                                        _configuration.Value);
                                    _logger.LogInformation("{Action} publisher configuration completed.",
                                        clear ? "Resetting" : "Refreshing");
                                }
                                try
                                {
                                    await _publisherHost.UpdateAsync(jobs).ConfigureAwait(false);
                                    _lastKnownFileHash = currentFileHash;
                                    // Mark as started
                                    _started?.TrySetResult();
                                }
                                catch (Exception ex) when (_started?.Task.IsCompletedSuccessfully ?? false)
                                {
                                    if (_publisherHost.TryUpdate(jobs))
                                    {
                                        _logger.LogDebug(ex, "Not initializing, update without waiting.");
                                        _lastKnownFileHash = currentFileHash;
                                    }
                                }
                                // Otherwise throw and retry
                            }
                            else
                            {
                                // avoid double events from FileSystemWatcher
                                if (lastWriteTime - _lastRead > TimeSpan.FromMilliseconds(10))
                                {
                                    _logger.LogDebug("Published Nodes File changed but " +
                                        "content-hash is equal to last one, nothing to do...");
                                }
                            }
                            _lastRead = lastWriteTime;
                            retryCount = 0;
                            // Success
                        }
                        catch (IOException ex)
                        {
                            if (++retryCount <= 3)
                            {
                                Debug.Assert(!clear);
                                _logger.LogDebug(ex,
                                    "Error while loading job from file. Attempt #{Count}...",
                                    retryCount);

                                // Queue another one and wait a bit
                                _fileChanges.Writer.TryWrite(false);
                                await Task.Delay(500).ConfigureAwait(false);
                                continue;
                            }
                            _logger.LogError(ex,
                                "Error while loading job from file. Retry expired, giving up.");
                        }
                        catch (SerializerException sx)
                        {
                            Debug.Assert(!clear);
                            const string error = "SerializerException while loading job from file.";
                            if (_logger.IsEnabled(LogLevel.Debug))
                            {
                                _logger.LogError(sx, error);
                            }
                            else
                            {
                                _logger.LogError(error);
                            }
                            retryCount = 0;
                            _started?.TrySetResult();
                        }
                        catch (Exception ex) when (ex is not ObjectDisposedException)
                        {
                            _logger.LogError(ex,
                                "Error during publisher {Action}. Retrying...",
                                clear ? "Reset" : "Update");
                            _fileChanges.Writer.TryWrite(clear);
                            retryCount = 0;
                            _started?.TrySetResult();
                        }
                    }
                    finally
                    {
                        _file.Release();
                    }
                }
            }
            finally
            {
                _publishedNodesProvider.EnableRaisingEvents = false;

                _publishedNodesProvider.Changed -= OnChanged;
                _publishedNodesProvider.Created -= OnCreated;
                _publishedNodesProvider.Renamed -= OnRenamed;
                _publishedNodesProvider.Deleted -= OnDeleted;

                _started = null;
            }
        }

        /// <summary>
        /// Add item to nodes
        /// </summary>
        /// <param name="currentNodes"></param>
        /// <param name="entry"></param>
        /// <param name="item"></param>
        private static void AddItem(List<PublishedNodesEntryModel> currentNodes,
            PublishedNodesEntryModel entry, PublishedItemModel item)
        {
            var found = currentNodes.Find(n => n.HasSameDataSet(entry));
            if (found == null)
            {
                currentNodes.Add(entry);
                found = entry;
            }
            found.OpcNodes ??= new List<OpcNodeModel>();
            var node = found.OpcNodes.FirstOrDefault(n => n.Id == item.NodeId);
            if (node == null)
            {
                found.OpcNodes.Add(new OpcNodeModel
                {
                    DisplayName = item.DisplayName,
                    Id = item.NodeId,
                    OpcSamplingIntervalTimespan = item.SamplingInterval,
                    HeartbeatIntervalTimespan = item.HeartbeatInterval,
                    OpcPublishingIntervalTimespan = item.PublishingInterval
                });
            }
            else
            {
                node.DisplayName = item.DisplayName;
                node.OpcSamplingIntervalTimespan = item.SamplingInterval;
                node.HeartbeatIntervalTimespan = item.HeartbeatInterval;
                node.OpcPublishingIntervalTimespan = item.PublishingInterval;
            }
        }

        /// <summary>
        /// Transforms legacy entries that use NodeId into ones using OpcNodes.
        /// The transformation will happen in-place.
        /// </summary>
        /// <param name="entries"></param>
        /// <exception cref="SerializerException"></exception>
        private static void TransformFromLegacyNodeId(List<PublishedNodesEntryModel> entries)
        {
            if (entries == null)
            {
                return;
            }
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.NodeId?.Identifier))
                {
                    entry.OpcNodes ??= new List<OpcNodeModel>();

                    if (entry.OpcNodes.Count != 0)
                    {
                        throw new SerializerException(
                            "Published nodes file contains DataSetWriter entry which " +
                            $"defines both {nameof(entry.OpcNodes)} and {nameof(entry.NodeId)}." +
                            "This is not supported. Please fix published nodes file.");
                    }

                    entry.OpcNodes.Add(new OpcNodeModel
                    {
                        Id = entry.NodeId.Identifier
                    });
                    entry.NodeId = null;
                }
            }
        }

        /// <summary>
        /// Create a checksum of the content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string GetChecksum(string content)
        {
            var checksum = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(checksum).Replace("-", string.Empty, StringComparison.Ordinal);
        }

        private const string kNullRequestMessage
            = "null request is provided";
        private const string kNullOrEmptyOpcNodesMessage
            = "null or empty OpcNodes is provided in request";

        private readonly ILogger _logger;
        private readonly IOptions<PublisherOptions> _configuration;
        private readonly PublishedNodesConverter _publishedNodesJobConverter;
        private readonly IStorageProvider _publishedNodesProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IDiagnosticCollector? _diagnostics;
        private readonly IPublisher _publisherHost;
        private string _lastKnownFileHash = string.Empty;
        private DateTime _lastRead = DateTime.MinValue;
        private TaskCompletionSource? _started;
        private readonly Task _fileChangeProcessor;
        private readonly Channel<bool> _fileChanges;
        private readonly SemaphoreSlim _api = new(1, 1);
        private readonly SemaphoreSlim _file = new(1, 1);
    }
}
