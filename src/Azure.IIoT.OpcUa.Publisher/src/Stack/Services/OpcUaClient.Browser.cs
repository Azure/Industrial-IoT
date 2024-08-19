// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    internal sealed partial class OpcUaClient
    {
        /// <summary>
        /// Browser utility class
        /// </summary>
        private sealed class Browser : IAsyncDisposable, IOpcUaBrowser
        {
            /// <summary>
            /// Reference changes
            /// </summary>
            public event EventHandler<Change<ReferenceDescription>>? OnReferenceChange;

            /// <summary>
            /// Node changes
            /// </summary>
            public event EventHandler<Change<Node>>? OnNodeChange;

            /// <summary>
            /// Create browser
            /// </summary>
            /// <param name="client"></param>
            /// <param name="subscriptionId"></param>
            /// <param name="browseDelay"></param>
            private Browser(OpcUaClient client, string subscriptionId, TimeSpan browseDelay)
            {
                _client = client;
                _logger = client._logger;
                _subscriptionId = subscriptionId;
                _browseDelay = browseDelay == TimeSpan.Zero ? Timeout.InfiniteTimeSpan : browseDelay;
                _channel = Channel.CreateUnbounded<bool>();

                // Order is important
                _rebrowseTimer = _client._timeProvider.CreateTimer(_ => _channel.Writer.TryWrite(true),
                    null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _browser = RunAsync(_cts.Token);
                _channel.Writer.TryWrite(true);
            }

            /// <inheritdoc/>
            public async ValueTask CloseAsync()
            {
                if (Release())
                {
                    await DisposeAsync().ConfigureAwait(false);
                }
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    try
                    {
                        _rebrowseTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                        _channel.Writer.TryComplete();

                        await _cts.CancelAsync().ConfigureAwait(false);

                        await _browser.ConfigureAwait(false);
                    }
                    finally
                    {
                        _cts.Dispose();
                        _rebrowseTimer.Dispose();
                    }
                }
            }

            /// <inheritdoc/>
            public void Rebrowse()
            {
                _channel.Writer.TryWrite(true);
            }

            /// <summary>
            /// Signal session connected
            /// </summary>
            public void OnConnected()
            {
                _channel.Writer.TryWrite(false);
            }

            /// <summary>
            /// Continously browse
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task RunAsync(CancellationToken ct)
            {
                _logger.LogDebug("Starting continous browsing process...");
                var sw = Stopwatch.StartNew();
                try
                {
                    await foreach (var result in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                    {
                        if (!result)
                        {
                            // Start browsing in 10 seconds
                            _rebrowseTimer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
                            continue;
                        }

                        _rebrowseTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                        try
                        {
                            var session = _client._session;
                            if (session?.Connected != true)
                            {
                                continue;
                            }

                            _logger.LogInformation("Browsing started after {Elapsed}...", sw.Elapsed);
                            sw.Restart();

                            await BrowseAddressSpaceAsync(session, ct).ConfigureAwait(false);

                            _logger.LogInformation("Browsing completed and took {Elapsed}. " +
                                "Added {AddedR}, removed {RemovedR} References and added {AddedN}, " +
                                "changed {ChangedN}, removed {RemovedN} Nodes with {Errors} errors.",
                                sw.Elapsed, _referencesAdded, _referencesRemoved, _nodesAdded,
                                _nodesChanged, _nodesRemoved, _errors);
                        }
                        catch (ServiceResultException sre)
                        {
                            _logger.LogInformation("Browsing completed due to error {Error} took {Elapsed}." +
                                "Added {AddedR}, removed {RemovedR} References and added {AddedN}, " +
                                "changed {ChangedN}, removed {RemovedN} Nodes with {Errors} errors.",
                                sre.Message, sw.Elapsed, _referencesAdded, _referencesRemoved,
                                _nodesAdded, _nodesChanged, _nodesRemoved, _errors);
                            if (!_client.IsConnected)
                            {
                                _logger.LogDebug("Not connected - waiting to reconnect.");
                                continue;
                            }
                            _logger.LogError(sre, "Error occurred during browsing");
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            // Continue
                            _logger.LogError(ex, "Browsing completed due to an exception and took {Elapsed}.",
                                sw.Elapsed);
                        }
                        finally
                        {
                            sw.Restart();
                            _referencesAdded = _referencesRemoved = 0;
                            _nodesAdded = _nodesChanged = _nodesRemoved = 0;
                            _errors = 0;
                            _rebrowseTimer.Change(_browseDelay, Timeout.InfiniteTimeSpan);
                        }
                    }
                    _logger.LogInformation("Browser process exited.");
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Browser process exited due to unexpected exception.");
                }
            }

            /// <summary>
            /// Browse address space
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task BrowseAddressSpaceAsync(OpcUaSession session, CancellationToken ct)
            {
                var browseDescriptionCollection = CreateBrowseDescriptionCollection(
                    (ObjectIds.RootFolder, new RelativePath()).YieldReturn());

                // Browse
                var foundReferences = new Dictionary<ReferenceDescription, (NodeId, RelativePath)>(
                    Compare.Using<ReferenceDescription>(Utils.IsEqual));
                var foundNodes = new Dictionary<NodeId, (RelativePath, Node)>();
                try
                {
                    var searchDepth = 0;
                    var maxNodesPerBrowse = session.OperationLimits.MaxNodesPerBrowse;
                    while (browseDescriptionCollection.Count != 0 && searchDepth < kMaxSearchDepth)
                    {
                        searchDepth++;

                        bool repeatBrowse;
                        var allBrowseResults = new List<(NodeId, RelativePath, BrowseResult)>();
                        var unprocessedOperations = new BrowseDescriptionCollection();
                        BrowseResultCollection? browseResultCollection = null;
                        do
                        {
                            var browseCollection = maxNodesPerBrowse == 0
                                ? browseDescriptionCollection
                                : browseDescriptionCollection.Take((int)maxNodesPerBrowse).ToArray();
                            repeatBrowse = false;
                            try
                            {
                                var browseResponse = await session.BrowseAsync(null, null,
                                    kMaxReferencesPerNode, browseCollection, ct).ConfigureAwait(false);
                                browseResultCollection = browseResponse.Results;
                                ClientBase.ValidateResponse(browseResultCollection, browseCollection);
                                ClientBase.ValidateDiagnosticInfos(
                                    browseResponse.DiagnosticInfos, browseCollection);

                                // seperate unprocessed nodes for later
                                for (var index = 0; index < browseResultCollection.Count; index++)
                                {
                                    var browseResult = browseResultCollection[index];

                                    // check for error.
                                    var statusCode = browseResult.StatusCode;
                                    if (StatusCode.IsBad(statusCode))
                                    {
                                        //
                                        // this error indicates that the server does not have enough
                                        // simultaneously active continuation points. This request will
                                        // need to be re-sent after the other operations have been
                                        // completed and their continuation points released.
                                        //
                                        if (statusCode == StatusCodes.BadNoContinuationPoints)
                                        {
                                            unprocessedOperations.Add(browseCollection[index]);
                                            continue;
                                        }
                                    }
                                    // save results.
                                    allBrowseResults.Add((browseCollection[index].NodeId,
                                        (RelativePath)browseCollection[index].Handle, browseResult));
                                }
                            }
                            catch (ServiceResultException sre) when
                                (sre.StatusCode == StatusCodes.BadEncodingLimitsExceeded ||
                                 sre.StatusCode == StatusCodes.BadResponseTooLarge)
                            {
                                // try to address by overriding operation limit
                                maxNodesPerBrowse = maxNodesPerBrowse == 0 ?
                                    (uint)browseCollection.Count / 2 : maxNodesPerBrowse / 2;
                                repeatBrowse = true;
                            }
                        }
                        while (repeatBrowse);

                        // Browse next
                        Debug.Assert(browseResultCollection != null);
                        var (nodeIds, continuationPoints) = PrepareBrowseNext(
                            new NodeIdCollection(browseDescriptionCollection
                                .Take(browseResultCollection.Count).Select(r => r.NodeId)),
                            browseResultCollection);
                        while (continuationPoints.Count != 0)
                        {
                            var browseNextResult = await session.BrowseNextAsync(null, false,
                                continuationPoints, ct).ConfigureAwait(false);
                            var browseNextResultCollection = browseNextResult.Results;
                            ClientBase.ValidateResponse(browseNextResultCollection, continuationPoints);
                            ClientBase.ValidateDiagnosticInfos(
                                browseNextResult.DiagnosticInfos, continuationPoints);

                            allBrowseResults.AddRange(browseNextResultCollection
                                .Select((r, i) => (browseDescriptionCollection[i].NodeId,
                                    (RelativePath)browseDescriptionCollection[i].Handle, r)));
                            (nodeIds, continuationPoints) = PrepareBrowseNext(nodeIds, browseNextResultCollection);
                        }

                        if (maxNodesPerBrowse == 0)
                        {
                            browseDescriptionCollection.Clear();
                        }
                        else
                        {
                            browseDescriptionCollection = browseDescriptionCollection
                                .Skip(browseResultCollection.Count)
                                .ToArray();
                        }

                        static (NodeIdCollection, ByteStringCollection) PrepareBrowseNext(
                            NodeIdCollection browseSourceCollection, BrowseResultCollection results)
                        {
                            var continuationPoints = new ByteStringCollection();
                            var nodeIdCollection = new NodeIdCollection();
                            for (var i = 0; i < results.Count; i++)
                            {
                                var browseResult = results[i];
                                if (browseResult.ContinuationPoint != null)
                                {
                                    nodeIdCollection.Add(browseSourceCollection[i]);
                                    continuationPoints.Add(browseResult.ContinuationPoint);
                                }
                            }
                            return (nodeIdCollection, continuationPoints);
                        }

                        // Build browse request for next level
                        var browseTable = new List<(NodeId, RelativePath)>();
                        foreach (var (source, path, browseResult) in allBrowseResults)
                        {
                            var nodesToRead = new List<NodeId>();
                            foreach (var reference in browseResult.References)
                            {
                                if (foundReferences.TryAdd(reference, (source, path)))
                                {
                                    if (!_knownReferences.Remove(reference))
                                    {
                                        // Send new reference
                                        _referencesAdded++;
                                        OnReferenceChange?.Invoke(session, CreateChange(source, path, null,
                                            reference));
                                    }
                                    var targetNodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                                    var targetPath = new RelativePath
                                    {
                                        Elements = new RelativePathElementCollection(path.Elements
                                            .Append(new RelativePathElement
                                            {
                                                TargetName = reference.BrowseName,
                                                IsInverse = false,
                                                IncludeSubtypes = false,
                                                ReferenceTypeId = reference.ReferenceTypeId
                                            }))
                                    };
                                    browseTable.Add((targetNodeId, targetPath));
                                    await ReadNodeAsync(session, targetNodeId, targetPath,
                                        foundNodes, ct).ConfigureAwait(false);
                                }
                            }
                        }
                        browseDescriptionCollection.AddRange(CreateBrowseDescriptionCollection(browseTable));
                        // add unprocessed nodes if any
                        browseDescriptionCollection.AddRange(unprocessedOperations);
                    }

                    _referencesRemoved += _knownReferences.Count;
                    foreach (var (removedReference, (nodeId, path)) in _knownReferences)
                    {
                        OnReferenceChange?.Invoke(session, CreateChange(nodeId, path, removedReference,
                            null));
                    }
                    _knownReferences.Clear();

                    _nodesRemoved += _knownNodes.Count;
                    foreach (var (removedNodeId, (path, removedNode)) in _knownNodes)
                    {
                        OnNodeChange?.Invoke(session, CreateChange(removedNodeId, path, removedNode,
                            null));
                    }
                    _knownNodes.Clear();
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    HandleException(foundReferences, foundNodes, ex);
                    throw;
                }
                finally
                {
                    _knownReferences = foundReferences;
                    _knownNodes = foundNodes;
                }

                static BrowseDescriptionCollection CreateBrowseDescriptionCollection(
                    IEnumerable<(NodeId NodeId, RelativePath Position)> items)
                {
                    return new BrowseDescriptionCollection(items.Select(
                        item => new BrowseDescription
                        {
                            Handle = item.Position,
                            BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                            IncludeSubtypes = true,
                            NodeId = item.NodeId,
                            NodeClassMask = 0,
                            ResultMask = (uint)BrowseResultMask.All
                        }));
                }

                void HandleException(Dictionary<ReferenceDescription, (NodeId, RelativePath)> foundReferences,
                    Dictionary<NodeId, (RelativePath, Node)> foundNodes, Exception ex)
                {
                    _logger.LogDebug(ex, "Stopping browse due to error.");

                    // Reset stream by resetting the sequence number to 0
                    _sequenceNumber = 0u;

                    //
                    // In case of exception we could not process the entire address space
                    // We add the remainder of the remaining existing references and nodes
                    // back to the currently known nodes and references and sort those out
                    // next time around.
                    //
                    foreach (var removedReference in _knownReferences)
                    {
                        // Re-add
                        foundReferences.AddOrUpdate(removedReference.Key, removedReference.Value);
                    }
                    _knownReferences.Clear();

                    foreach (var removedNode in _knownNodes)
                    {
                        // Re-add
                        foundNodes.AddOrUpdate(removedNode.Key, removedNode.Value);
                    }
                    _knownNodes.Clear();
                }
            }

            /// <summary>
            /// Read node and send add or change notification
            /// </summary>
            /// <param name="session"></param>
            /// <param name="targetNodeId"></param>
            /// <param name="targetPath"></param>
            /// <param name="foundNodes"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async ValueTask ReadNodeAsync(OpcUaSession session, NodeId targetNodeId,
                RelativePath targetPath, Dictionary<NodeId, (RelativePath Path, Node Node)> foundNodes,
                CancellationToken ct)
            {
                try
                {
                    var node = await session.ReadNodeAsync(targetNodeId, ct).ConfigureAwait(false);
                    if (NodeId.IsNull(node.NodeId))
                    {
                        return;
                    }
                    if (_knownNodes.Remove(node.NodeId, out var existing) &&
                        !Utils.IsEqual(existing.Item2, node))
                    {
                        // send updated node
                        _nodesChanged++;
                        OnNodeChange?.Invoke(session, CreateChange(targetNodeId, existing.Item1,
                            existing.Item2, node));
                    }

                    if (foundNodes.TryAdd(node.NodeId, (targetPath, node)) && existing.Item1 == null)
                    {
                        // Send added node
                        _nodesAdded++;
                        OnNodeChange?.Invoke(session, CreateChange(targetNodeId, targetPath,
                            null, node));
                    }
                }
                catch (Exception) when (session.Connected)
                {
                    // TODO: Notify error here, but we are anyway sending a removal...
                    _errors++;
                }
            }

            /// <summary>
            /// Helper to create a change structure
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="source"></param>
            /// <param name="path"></param>
            /// <param name="existing"></param>
            /// <param name="New"></param>
            /// <returns></returns>
            private Change<T> CreateChange<T>(NodeId source, RelativePath path, T? existing, T? New) where T : class
            {
                return new(source, path, existing, New, Interlocked.Increment(ref _sequenceNumber),
                    _client._timeProvider.GetUtcNow());
            }

            /// <summary>
            /// Register
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="rebrowsePeriod"></param>
            /// <param name="subscriptionId"></param>
            /// <returns></returns>
            public static Browser Register(OpcUaClient outer, TimeSpan rebrowsePeriod, string subscriptionId)
            {
                lock (outer._browsers)
                {
                    if (!outer._browsers.TryGetValue((subscriptionId, rebrowsePeriod), out var browser))
                    {
                        browser = new Browser(outer, subscriptionId, rebrowsePeriod);
                        outer._browsers.Add((subscriptionId, rebrowsePeriod), browser);
                    }
                    browser.AddRef();
                    return browser;
                }
            }

            /// <summary>
            /// Take a reference on this browser
            /// </summary>
            private void AddRef()
            {
                _refCount++;
                _channel.Writer.TryWrite(false); // Ensure we start a rebrowse in 10
            }

            /// <summary>
            /// Release browser and remove from browser list
            /// </summary>
            /// <returns></returns>
            private bool Release()
            {
                var cleanup = false;
                lock (_client._browsers)
                {
                    if (--_refCount == 0 && _client._browsers.Remove((_subscriptionId, _browseDelay)))
                    {
                        cleanup = true;
                    }
                }
                return cleanup;
            }

            const int kMaxSearchDepth = 128;
            const int kMaxReferencesPerNode = 1000;

            private bool _disposed;
            private uint _sequenceNumber;
            private int _refCount;
            private int _referencesAdded;
            private int _referencesRemoved;
            private int _nodesAdded;
            private int _nodesChanged;
            private int _nodesRemoved;
            private int _errors;
            private Dictionary<NodeId, (RelativePath, Node)> _knownNodes = new();
            private Dictionary<ReferenceDescription, (NodeId, RelativePath)> _knownReferences =
                new(Compare.Using<ReferenceDescription>(Utils.IsEqual));
            private readonly string _subscriptionId;
            private readonly Task _browser;
            private readonly OpcUaClient _client;
            private readonly ILogger _logger;
            private readonly Channel<bool> _channel;
            private readonly ITimer _rebrowseTimer;
            private readonly CancellationTokenSource _cts = new();
            private readonly TimeSpan _browseDelay;
        }
    }
}
