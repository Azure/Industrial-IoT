// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration services uses the address space and services of a connected server to
    /// configure the publisher. The configuration services allow interactive expansion of
    /// published nodes.
    /// </summary>
    public sealed class ConfigurationServices : IConfigurationServices,
        IAssetConfiguration<Stream>, IAssetConfiguration<byte[]>, IDisposable
    {
        /// <summary>
        /// Create configuration services
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="client"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        public ConfigurationServices(IPublishedNodesServices configuration,
            IOpcUaClientManager<ConnectionModel> client, IOptions<PublisherOptions> options,
            ILogger<ConfigurationServices> logger, TimeProvider? timeProvider = null)
        {
            _configuration = configuration;
            _client = client;
            _options = options;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> ExpandAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.OpcNodes);
            ValidateNodes(entry.OpcNodes);

            var browser = new ConfigBrowser(entry, request, _options, null,
                _logger, _timeProvider);
            return _client.ExecuteAsync(entry.ToConnectionModel(), browser, request.Header, ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.OpcNodes);
            ValidateNodes(entry.OpcNodes);

            var browser = new ConfigBrowser(entry, request, _options, _configuration,
                _logger, _timeProvider);
            return _client.ExecuteAsync(entry.ToConnectionModel(), browser, request.Header, ct);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<byte[]> request, CancellationToken ct)
        {
            var stream = new MemoryStream(request.Configuration);
            await using var _ = stream.ConfigureAwait(false);
            var requestWithStream = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream,
                Entry = request.Entry,
                Header = request.Header,
                WaitTime = request.WaitTime
            };
            return await CreateOrUpdateAssetAsync(requestWithStream,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> GetAllAssetsAsync(
            PublishedNodesEntryModel entry, RequestHeaderModel? header, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return CoreAsync(ct);

            async IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> CoreAsync(
                [EnumeratorCancellation] CancellationToken ct)
            {
                // Expand the wot node one level and expand each level
                var expansion = new PublishedNodeExpansionModel
                {
                    ExcludeRootIfInstanceNode = true,
                    MaxDepth = 1, // There is one asset level underneath the root connection node
                    Header = header
                };
                var browser = new ConfigBrowser(entry with
                {
                    // Named object in the address space of the server.
                    OpcNodes = [new() { Id = AssetsEx.Root }]
                }, expansion, _options, null, _logger, _timeProvider, true);

                // Browse and swap the data set writer id and data set name to make an asset entry.
                await foreach (var result in _client.ExecuteAsync(entry.ToConnectionModel(),
                    browser, header, ct).ConfigureAwait(false))
                {
                    yield return result with
                    {
                        Result = result.Result == null ? null : result.Result with
                        {
                            DataSetWriterGroup = entry.DataSetWriterGroup,
                            DataSetWriterId = result.Result.DataSetName,
                            DataSetName = result.Result.DataSetWriterId?.TrimStart('/')
                        }
                    };
                }
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<Stream> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Configuration);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetName); // Asset name

            using var trace = _activitySource.StartActivity("CreateOrUpdateAsset");

            var entry = request.Entry;
            var connection = entry.ToConnectionModel();
            ServiceResultModel? errorInfo;
            (entry, errorInfo) = await _client.ExecuteAsync(connection, async context =>
            {
                // 1) Create asset and get asset file object
                var (nodeId, errorInfo) = await context.Session.CreateAssetAsync(
                    request.Header.ToRequestHeader(_timeProvider),
                    request.Entry.DataSetName, context.Ct).ConfigureAwait(false);
                if (errorInfo != null || nodeId is null || NodeId.IsNull(nodeId))
                {
                    // TOOD errorInfo?.StatusCode ==
                    //  Opc.Ua.StatusCodes.BadBrowseNameDuplicated
                    errorInfo ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadNodeIdInvalid,
                        ErrorMessage = "Failed to create asset."
                    };
                    return (entry with { DataSetWriterId = null }, errorInfo);
                }
                var assetId = nodeId.AsString(context.Session.MessageContext,
                    NamespaceFormat.Expanded);

                entry = entry with
                {
                    DataSetWriterId = assetId,
                    OpcNodes =
                    [
                        new ()
                        {
                            Id = assetId,
                            DataSetFieldId = entry.DataSetName
                        }
                    ]
                };

                // Find the created asset file
                (nodeId, errorInfo) = await context.Session.GetAssetFileAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId!,
                    context.Ct).ConfigureAwait(false);
                if (errorInfo != null || nodeId is null || NodeId.IsNull(nodeId))
                {
                    errorInfo ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadNotFound,
                        ErrorMessage = "Asset did not have a file component."
                    };
                    return (entry, errorInfo);
                }

                // 2) upload asset file
                var bufferSize = await context.Session.GetBufferSizeAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId,
                    context.Ct).ConfigureAwait(false);
                var (fileHandle, openError) = await context.Session.OpenAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId, 0x2 | 0x4,
                    context.Ct).ConfigureAwait(false);
                if (openError != null || !fileHandle.HasValue || NodeId.IsNull(nodeId))
                {
                    openError ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadUserAccessDenied,
                        ErrorMessage = "Asset file could not be opened for write."
                    };
                    return (entry, openError);
                }
                while (true)
                {
                    // Copy buffers to server
                    var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        var read = await request.Configuration.ReadAsync(
                            buffer.AsMemory(), context.Ct).ConfigureAwait(false);
                        if (read == 0)
                        {
                            break;
                        }
                        errorInfo = await context.Session.WriteAsync(
                            request.Header.ToRequestHeader(_timeProvider), nodeId,
                            fileHandle.Value, buffer.AsMemory()[..read],
                            context.Ct).ConfigureAwait(false);
                        if (errorInfo != null)
                        {
                            return (entry, errorInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        return (entry, ex.ToServiceResultModel());
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                errorInfo = await context.Session.CloseAndUpdateAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId,
                    fileHandle.Value, context.Ct).ConfigureAwait(false);
                return (entry, errorInfo);
            }, request.Header, ct).ConfigureAwait(false);

            // From now on we need to revert by deleting the asset
            if (errorInfo != null && errorInfo.StatusCode != 0)
            {
                if (entry.DataSetWriterId != null)
                {
                    // Delete the asset for good house keeping sake
                    await DeleteAssetAsync(request.Header, entry, ct).ConfigureAwait(false);
                }
                return new ServiceResponse<PublishedNodesEntryModel>
                {
                    Result = entry,
                    ErrorInfo = errorInfo
                };
            }
            try
            {
                if (request.WaitTime.HasValue)
                {
                    //
                    // The asset is uploaded the nodes are being created, potentially
                    // the session is disconnected now and the server is restarting.
                    // We wait a bit here to ensure all of this has happened correctly.
                    //
                    await _timeProvider.Delay(request.WaitTime.Value, ct).ConfigureAwait(false);
                }

                // 3) Collect all created tags under the asset
                var browser = new ConfigBrowser(entry, new PublishedNodeExpansionModel
                {
                    CreateSingleWriter = true,
                    MaxDepth = 0,
                    Header = request.Header
                }, _options, _configuration, _logger, _timeProvider);

                var results = new List<ServiceResponse<PublishedNodesEntryModel>>();
                await foreach (var configurationResult in _client.ExecuteAsync(
                    entry.ToConnectionModel(), browser, request.Header,
                    ct).ConfigureAwait(false))
                {
                    results.Add(configurationResult);
                }
                // We only expect a single writer here, else it is a failure.
                if (results.Count != 1 ||
                    (results[0].ErrorInfo != null && results[0].ErrorInfo!.StatusCode != 0))
                {
                    // Could not create tags - delete the asset
                    errorInfo = results.Select(r => r.ErrorInfo)
                        .FirstOrDefault(r => r != null);
                    errorInfo ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadUnexpectedError,
                        ErrorMessage = "Failed to find any tags for the asset."
                    };
                    await DeleteAssetAsync(request.Header, entry, ct).ConfigureAwait(false);
                    return new ServiceResponse<PublishedNodesEntryModel>
                    {
                        Result = entry,
                        ErrorInfo = errorInfo
                    };
                }
                return results[0];
            }
            catch (Exception ex)
            {
                await DeleteAssetAsync(request.Header, entry, ct).ConfigureAwait(false);
                return new ServiceResponse<PublishedNodesEntryModel>
                {
                    Result = entry,
                    ErrorInfo = ex.ToServiceResultModel()
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> DeleteAssetAsync(
            PublishedNodeDeleteAssetRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterId);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);

            using var trace = _activitySource.StartActivity("DeleteAsset");

            // First remove the entry
            try
            {
                await _configuration.RemoveDataSetWriterEntryAsync(
                    request.Entry.DataSetWriterGroup, request.Entry.DataSetWriterId,
                    ct: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!request.Force)
                {
                    return ex.ToServiceResultModel();
                }
                _logger.LogInformation(ex, "Discard error because force was set.");
            }
            var errorInfo = await DeleteAssetAsync(request.Header, request.Entry,
                ct).ConfigureAwait(false);
            return errorInfo ?? new ServiceResultModel();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <summary>
        /// Delete the asset
        /// </summary>
        /// <param name="header"></param>
        /// <param name="entry"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ServiceResultModel?> DeleteAssetAsync(RequestHeaderModel? header,
            PublishedNodesEntryModel entry, CancellationToken ct)
        {
            return await _client.ExecuteAsync(entry.ToConnectionModel(), async context =>
            {
                var assetId = entry.DataSetWriterId.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(assetId))
                {
                    return new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadNodeIdInvalid,
                        ErrorMessage = "Invalid node id and browse path in file system object"
                    };
                }
                return await context.Session.DeleteAssetAsync(header.ToRequestHeader(_timeProvider),
                    assetId, ct).ConfigureAwait(false);
            }, header, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Validate nodes are valid
        /// </summary>
        /// <param name="opcNodes"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        private static IList<OpcNodeModel> ValidateNodes(IList<OpcNodeModel> opcNodes)
        {
            var set = new HashSet<string>();
            foreach (var node in opcNodes)
            {
                if (!node.TryGetId(out var id))
                {
                    throw new BadRequestException("Node must contain a node ID");
                }
                node.DataSetFieldId ??= id;
                set.Add(node.DataSetFieldId);
                if (node.OpcPublishingInterval != null ||
                    node.OpcPublishingIntervalTimespan != null)
                {
                    throw new BadRequestException(
                        "Publishing interval not allowed on node level. " +
                        "Must be set at writer level.");
                }
            }
            if (set.Count != opcNodes.Count)
            {
                throw new BadRequestException("Field ids must be present and unique.");
            }
            return opcNodes;
        }

        /// <summary>
        /// Configuration browser
        /// </summary>
        internal sealed class ConfigBrowser : AsyncEnumerableBrowser<ServiceResponse<PublishedNodesEntryModel>>
        {
            /// <inheritdoc/>
            public ConfigBrowser(PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
                IOptions<PublisherOptions> options, IPublishedNodesServices? configuration, ILogger logger,
                TimeProvider? timeProvider = null, bool allowNoResolution = false)
                : base(request.Header, options, timeProvider)
            {
                _entry = entry;
                _request = request;
                _configuration = configuration;
                _logger = logger;
                _allowNoResolution = allowNoResolution;
            }

            /// <inheritdoc/>
            public override void Reset()
            {
                base.Reset();

                _nodeIndex = -1;
                _expanded.Clear();

                Push(BeginAsync);
            }

            /// <inheritdoc/>
            protected override void OnReset()
            {
                // We handle our own restarts
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleError(
                ServiceCallContext context, ServiceResultModel errorInfo)
            {
                var node = _currentObject != null ? _currentObject.OriginalNode : CurrentNode;
                node.AddErrorInfo(errorInfo);
                _logger.LogError("Error expanding node {Node}: {Error}", node, errorInfo);
                return [];
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleMatching(
                ServiceCallContext context, IReadOnlyList<BrowseFrame> matching)
            {
                if (_currentObject != null)
                {
                    // collect matching variables under the current object instance
                    if (_currentObject.AddVariables(matching))
                    {
                        _logger.LogDebug("Dropped duplicate variables found.");
                    }
                }
                else
                {
                    // collect matching object instances
                    CurrentNode.AddObjectsOrVariables(matching);
                }
                return [];
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleCompletion(
                ServiceCallContext context)
            {
                Push(CompleteAsync);
                return [];
            }

            /// <summary>
            /// Complete the browse operation and resolve objects
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<ServiceResponse<PublishedNodesEntryModel>>> CompleteAsync(
                ServiceCallContext context)
            {
                var results = new List<ServiceResponse<PublishedNodesEntryModel>>();
                var currentObject = _currentObject;
                if (currentObject != null)
                {
                    // Process current object
                    await ProcessAsync(currentObject, context, results).ConfigureAwait(false);

                    if (TryMoveToNextObject())
                    {
                        // Kicked off the next expansion
                        return results;
                    }
                    Debug.Assert(_currentObject == null);
                }
                else if (CurrentNode.Variables.ContainsVariables)
                {
                    // Completing a browse for variables
                    await ProcessAsync(CurrentNode.Variables, context, results).ConfigureAwait(false);
                }
                else if (!CurrentNode.ContainsObjects)
                {
                    // Completing a browse for objects
                    if (!CurrentNode.HasErrors && !_allowNoResolution)
                    {
                        CurrentNode.AddErrorInfo(StatusCodes.BadNotFound, "No objects resolved.");
                    }
                }
                if (!TryMoveToNextNode())
                {
                    // Complete
                    results.AddRange(await EndAsync(context).ConfigureAwait(false));
                }
                return results;

                async Task ProcessAsync(ObjectToExpand currentObject, ServiceCallContext context,
                    List<ServiceResponse<PublishedNodesEntryModel>> results)
                {
                    if (currentObject.ContainsVariables &&
                        !_request.CreateSingleWriter && !currentObject.OriginalNode.HasErrors)
                    {
                        // Create a new writer entry for the object
                        var result = await SaveEntryAsync(new ServiceResponse<PublishedNodesEntryModel>
                        {
                            Result = _entry with
                            {
                                DataSetName = currentObject.CreateDataSetName(
                                    context.Session.MessageContext),
                                DataSetWriterId = currentObject.CreateWriterId(),
                                OpcNodes = currentObject
                                    .GetOpcNodeModels(
                                        currentObject.OriginalNode.NodeFromConfiguration,
                                        context.Session.MessageContext)
                                    .ToList()
                            }
                        }, context.Ct).ConfigureAwait(false);

                        currentObject.EntriesAlreadyReturned = true;
                        if (!_request.DiscardErrors || result.ErrorInfo == null)
                        {
                            // Add good entry to return _now_
                            results.Add(result);
                        }
                    }
                }
            }

            /// <summary>
            /// Start by resolving nodes and starting the browse operation
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<ServiceResponse<PublishedNodesEntryModel>>> BeginAsync(
                ServiceCallContext context)
            {
                if (_entry.OpcNodes?.Count > 0)
                {
                    // TODO: Could be done in one request for better efficiency
                    foreach (var node in _entry.OpcNodes)
                    {
                        var nodeId = await context.Session.ResolveNodeIdAsync(_request.Header,
                            node.Id, node.BrowsePath, nameof(node.BrowsePath), TimeProvider,
                            context.Ct).ConfigureAwait(false);

                        var readValueIds = new ReadValueIdCollection
                        {
                            new ReadValueId
                            {
                                NodeId = nodeId,
                                AttributeId = Attributes.NodeClass
                            },
                            new ReadValueId
                            {
                                NodeId = nodeId,
                                AttributeId = Attributes.BrowseName
                            },
                            new ReadValueId
                            {
                                NodeId = nodeId,
                                AttributeId = Attributes.DisplayName
                            },
                            new ReadValueId
                            {
                                NodeId = nodeId,
                                AttributeId = Attributes.EventNotifier
                            }
                        };
                        var response = await context.Session.Services.ReadAsync(
                            _request.Header.ToRequestHeader(TimeProvider), 0,
                            Opc.Ua.TimestampsToReturn.Neither, readValueIds,
                            context.Ct).ConfigureAwait(false);

                        var readResults = response.Validate(response.Results,
                            s => s.StatusCode, response.DiagnosticInfos, readValueIds);

                        var errorInfo = readResults.ErrorInfo ??
                            readResults[0].ErrorInfo;
                        var nodeClass = errorInfo != null ? Opc.Ua.NodeClass.Unspecified :
                            readResults[0].Result.GetValueOrDefaultEx<Opc.Ua.NodeClass>();
                        var browseName = errorInfo != null ? null :
                            readResults[1].Result.GetValueOrDefaultEx<QualifiedName>();
                        var displayName = errorInfo != null ? null :
                            readResults[2].Result.GetValueOrDefaultEx<LocalizedText>();
                        var eventNotifier = errorInfo != null ? (byte)0 :
                            readResults[3].Result.GetValueOrDefaultEx<byte>();

                        ExpandedNodeId? typeDefinitionId = null;
                        if (errorInfo == null)
                        {
                            switch (nodeClass)
                            {
                                case Opc.Ua.NodeClass.ObjectType:
                                case Opc.Ua.NodeClass.VariableType:
                                    typeDefinitionId = nodeId;
                                    break;
                                case Opc.Ua.NodeClass.Object:
                                    var (results, errorInfo2) = await context.Session.FindAsync(
                                        _request.Header.ToRequestHeader(TimeProvider),
                                        nodeId.YieldReturn(), ReferenceTypeIds.HasTypeDefinition,
                                        nodeClassMask: (uint)Opc.Ua.NodeClass.ObjectType,
                                        ct: context.Ct).ConfigureAwait(false);
                                    errorInfo = errorInfo2;
                                    if (errorInfo != null)
                                    {
                                        break;
                                    }
                                    Debug.Assert(results.Count == 1);
                                    typeDefinitionId = results[0].Node;
                                    break;
                            }
                        }
                        _expanded.Add(new NodeToExpand(node, nodeId, nodeClass,
                            browseName, displayName, eventNotifier, typeDefinitionId, errorInfo));
                    }

                    if (!TryMoveToNextNode())
                    {
                        // Complete
                        return await EndAsync(context).ConfigureAwait(false);
                    }
                }
                return [];
            }

            /// <summary>
            /// Return remaining entries
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<ServiceResponse<PublishedNodesEntryModel>>> EndAsync(
                ServiceCallContext context)
            {
                var results = new List<ServiceResponse<PublishedNodesEntryModel>>();
                var ids = new HashSet<string?>();
                var goodNodes = _expanded
                    .Where(e => !e.HasErrors)
                    .SelectMany(r => r.GetAllOpcNodeModels(context.Session.MessageContext, ids))
                    .ToList();
                if (goodNodes.Count > 0)
                {
                    var result = await SaveEntryAsync(new ServiceResponse<PublishedNodesEntryModel>
                    {
                        Result = _entry with { OpcNodes = goodNodes }
                    }, context.Ct).ConfigureAwait(false);
                    if (!_request.DiscardErrors || result.ErrorInfo == null)
                    {
                        // Add good entry
                        results.Add(result);
                    }
                }
                if (!_request.DiscardErrors)
                {
                    var badNodes = _expanded
                        .Where(e => e.HasErrors)
                        .SelectMany(e => e.ErrorInfos
                            .Select(error => (error, e
                                .GetAllOpcNodeModels(context.Session.MessageContext, ids, true)
                                .ToList())))
                        .GroupBy(e => e.error)
                        .SelectMany(r => r.Select(r => r))
                        .ToList();
                    foreach (var entry in badNodes)
                    {
                        // Return bad entries
                        results.Add(new ServiceResponse<PublishedNodesEntryModel>
                        {
                            ErrorInfo = entry.error,
                            Result = _entry with { OpcNodes = entry.Item2 }
                        });
                    }
                }
                _nodeIndex = -1;
                _expanded.Clear();
                return results;
            }

            /// <summary>
            /// Try move to next node
            /// </summary>
            /// <returns></returns>
            private bool TryMoveToNextNode()
            {
                Debug.Assert(_currentObject == null);
                _nodeIndex++;
                for (; _nodeIndex < _expanded.Count; _nodeIndex++)
                {
                    switch (CurrentNode.NodeClass)
                    {
                        case (uint)Opc.Ua.NodeClass.Object:
                            // Resolve all objects under this object
                            Debug.Assert(!NodeId.IsNull(CurrentNode.NodeId));
                            if (!_request.ExcludeRootIfInstanceNode)
                            {
                                // Add root
                                CurrentNode.AddObjectsOrVariables(
                                    new BrowseFrame(CurrentNode.NodeId!).YieldReturn());

                                if (_request.MaxDepth == 0)
                                {
                                    // We have the object - browse it now
                                    return TryMoveToNextObject();
                                }
                            }
                            var depth = _request.MaxDepth == 0 ? 1 : _request.MaxDepth;
                            Restart(CurrentNode.NodeId, maxDepth: depth,
                                referenceTypeId: ReferenceTypeIds.HierarchicalReferences);
                            return true;
                        case (uint)Opc.Ua.NodeClass.VariableType:
                        case (uint)Opc.Ua.NodeClass.ObjectType:
                            // Resolve all objects of this type
                            Debug.Assert(!NodeId.IsNull(CurrentNode.NodeId));
                            var instanceClass =
                                CurrentNode.NodeClass == (uint)Opc.Ua.NodeClass.ObjectType ?
                                    Opc.Ua.NodeClass.Object : Opc.Ua.NodeClass.Variable;
                            var stopWhenFound = instanceClass == Opc.Ua.NodeClass.Variable ||
                                _request.FlattenTypeInstance;
                            Restart(ObjectIds.ObjectsFolder, maxDepth: _request.MaxDepth,
                                typeDefinitionId: CurrentNode.NodeId,
                                stopWhenFound: stopWhenFound,
                                referenceTypeId: ReferenceTypeIds.HierarchicalReferences,
                                matchClass: instanceClass);
                            return true;
                        case (uint)Opc.Ua.NodeClass.Variable:
                            if (!_request.ExcludeRootIfInstanceNode)
                            {
                                // Add root
                                CurrentNode.AddObjectsOrVariables(
                                    new BrowseFrame(CurrentNode.NodeId!).YieldReturn());

                                if (_request.MaxLevelsToExpand == 0)
                                {
                                    // Done - already a variable - stays in the original entry
                                    break;
                                }
                            }
                            // Now we expand the variable here
                            Restart(CurrentNode.NodeId,
                                _request.MaxLevelsToExpand == 0 ? 1 : _request.MaxLevelsToExpand,
                                referenceTypeId: ReferenceTypeIds.Aggregates,
                                nodeClass: Opc.Ua.NodeClass.Variable);
                            return true;
                        case (uint)Opc.Ua.NodeClass.Unspecified:
                            // There should already be an error here
                            if (CurrentNode.HasErrors)
                            {
                                break;
                            }
                            goto default;
                        default:
                            CurrentNode.AddErrorInfo(StatusCodes.BadNotSupported,
                                $"Node class {CurrentNode.NodeClass} not supported.");
                            break;
                    }
                }
                return TryMoveToNextObject();
            }

            /// <summary>
            /// Find next object to expand
            /// </summary>
            /// <returns></returns>
            private bool TryMoveToNextObject()
            {
                foreach (var node in _expanded)
                {
                    if (node.TryGetNextObject(out _currentObject))
                    {
                        Debug.Assert(_currentObject != null);
                        var nodeClass = Opc.Ua.NodeClass.Variable;
                        var maxDepth = _request.MaxLevelsToExpand == 0 ? (uint?)null :
                            _request.MaxLevelsToExpand;
                        if (_currentObject.OriginalNode.NodeClass == (uint)Opc.Ua.NodeClass.ObjectType
                            && _request.FlattenTypeInstance)
                        {
                            nodeClass |= Opc.Ua.NodeClass.Object;
                            maxDepth = null;
                        }
                        Restart(_currentObject.ObjectFromBrowse.NodeId, maxDepth,
                            referenceTypeId: ReferenceTypeIds.Aggregates,
                            nodeClass: nodeClass, matchClass: Opc.Ua.NodeClass.Variable);
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Save entry if update is enabled
            /// </summary>
            /// <param name="entry"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async ValueTask<ServiceResponse<PublishedNodesEntryModel>> SaveEntryAsync(
                ServiceResponse<PublishedNodesEntryModel> entry, CancellationToken ct)
            {
                Debug.Assert(entry.Result != null);
                Debug.Assert(entry.Result.OpcNodes != null);
                Debug.Assert(entry.ErrorInfo == null);
                try
                {
                    ValidateNodes(entry.Result.OpcNodes);
                    if (_configuration != null)
                    {
                        await _configuration.CreateOrUpdateDataSetWriterEntryAsync(entry.Result,
                            ct).ConfigureAwait(false);
                    }
                    return entry;
                }
                catch (Exception ex)
                {
                    return entry with { ErrorInfo = ex.ToServiceResultModel() };
                }
            }

            /// <summary>
            /// Get current node to expand
            /// </summary>
            private NodeToExpand CurrentNode
            {
                get
                {
                    Debug.Assert(_nodeIndex < _expanded.Count);
                    return _expanded[_nodeIndex];
                }
            }

            /// <summary>
            /// Node that should be expanded
            /// </summary>
            private class NodeToExpand
            {
                public IEnumerable<ServiceResultModel> ErrorInfos => _errorInfos;

                public bool HasErrors => _errorInfos.Count > 0;

                public bool ContainsObjects => _objects.Count > 0;

                public ObjectToExpand Variables { get; }

                /// <summary>
                /// Original node from configuration
                /// </summary>
                public OpcNodeModel NodeFromConfiguration { get; }

                /// <summary>
                /// Node id that should be expanded
                /// </summary>
                public NodeId? NodeId { get; }

                /// <summary>
                /// Node class of the node
                /// </summary>
                public uint NodeClass { get; }

                /// <summary>
                /// Event Notifier of the node
                /// </summary>
                public byte EventNotifier { get; }

                /// <summary>
                /// Create node to expand
                /// </summary>
                /// <param name="nodeFromConfiguration"></param>
                /// <param name="nodeId"></param>
                /// <param name="nodeClass"></param>
                /// <param name="browseName"></param>
                /// <param name="displayName"></param>
                /// <param name="eventNotifier"></param>
                /// <param name="typeDefinitionId"></param>
                /// <param name="errorInfo"></param>
                public NodeToExpand(OpcNodeModel nodeFromConfiguration, NodeId? nodeId,
                    Opc.Ua.NodeClass nodeClass, QualifiedName? browseName, LocalizedText? displayName,
                    byte eventNotifier, ExpandedNodeId? typeDefinitionId, ServiceResultModel? errorInfo)
                {
                    NodeFromConfiguration = nodeFromConfiguration;
                    NodeId = nodeId;
                    NodeClass = (uint)nodeClass;
                    EventNotifier = eventNotifier;

                    if (errorInfo != null)
                    {
                        AddErrorInfo(errorInfo);
                    }

                    // Hold variables resolved from a variable or variable type
                    Variables = new ObjectToExpand(new BrowseFrame(
                        nodeId ?? NodeId.Null, browseName ?? "Variables",
                        displayName?.Text ?? "Variables", typeDefinitionId, nodeClass), this);
                }

                /// <summary>
                /// Opc node model configurations over all objects
                /// </summary>
                /// <param name="context"></param>
                /// <param name="ids"></param>
                /// <param name="error"></param>
                /// <returns></returns>
                public IEnumerable<OpcNodeModel> GetAllOpcNodeModels(IServiceMessageContext context,
                    HashSet<string?>? ids = null, bool error = false)
                {
                    switch (NodeClass)
                    {
                        case (uint)Opc.Ua.NodeClass.VariableType:
                        case (uint)Opc.Ua.NodeClass.Variable:
                            if (Variables.EntriesAlreadyReturned)
                            {
                                break;
                            }
                            var variables = Variables.GetOpcNodeModels(NodeFromConfiguration,
                                    context, ids, true);
                            if ((!error && NodeClass == (uint)Opc.Ua.NodeClass.VariableType) ||
                                ids?.Contains(NodeFromConfiguration.DataSetFieldId) == true)
                            {
                                // Only variables, not the root variable
                                return variables;
                            }
                            return variables.Prepend(NodeFromConfiguration);
                        case (uint)Opc.Ua.NodeClass.Object:
                        case (uint)Opc.Ua.NodeClass.ObjectType:
                            var objects = _objects
                                .Where(o => !o.EntriesAlreadyReturned)
                                .SelectMany(o => o.GetOpcNodeModels(
                                    NodeFromConfiguration, context, ids, true));
                            if (!error)
                            {
                                return objects;
                            }
                            return objects.Prepend(NodeFromConfiguration);
                    }
                    return error ? [NodeFromConfiguration] : Array.Empty<OpcNodeModel>();
                }

                /// <summary>
                /// Add objects or variables depending on the node class that is expanded
                /// </summary>
                /// <param name="frames"></param>
                public void AddObjectsOrVariables(IEnumerable<BrowseFrame> frames)
                {
                    switch (NodeClass)
                    {
                        case (uint)Opc.Ua.NodeClass.VariableType:
                        case (uint)Opc.Ua.NodeClass.Variable:
                            Variables.AddVariables(frames
                                .Where(f => !NodeId.IsNull(f.NodeId) && _knownIds.Add(f.NodeId)));
                            break;
                        default:
                            _objects.AddRange(frames
                                .Where(f => !NodeId.IsNull(f.NodeId) && _knownIds.Add(f.NodeId))
                                .Select(f => new ObjectToExpand(f, this)));
                            break;
                    }
                }

                /// <summary>
                /// Add error info
                /// </summary>
                /// <param name="statusCode"></param>
                /// <param name="message"></param>
                public void AddErrorInfo(uint statusCode, string message)
                {
                    _errorInfos.Add(new ServiceResultModel
                    {
                        ErrorMessage = message,
                        StatusCode = statusCode
                    });
                }

                /// <summary>
                /// Add error info
                /// </summary>
                /// <param name="errorInfo"></param>
                public void AddErrorInfo(ServiceResultModel? errorInfo)
                {
                    if (errorInfo != null)
                    {
                        _errorInfos.Add(errorInfo);
                    }
                }

                /// <summary>
                /// Move to next object
                /// </summary>
                /// <param name="obj"></param>
                /// <returns></returns>
                public bool TryGetNextObject(out ObjectToExpand? obj)
                {
                    if (_objectIndex < _objects.Count)
                    {
                        obj = _objects[_objectIndex];
                        _objectIndex++;
                        return true;
                    }
                    obj = null;
                    return false;
                }

                private readonly List<ServiceResultModel> _errorInfos = [];
                private readonly List<ObjectToExpand> _objects = [];
                private readonly HashSet<NodeId> _knownIds = [];
                private int _objectIndex;
            }

            /// <summary>
            /// The object to expand
            /// </summary>
            private class ObjectToExpand
            {
                public BrowseFrame ObjectFromBrowse { get; }
                public NodeToExpand OriginalNode { get; }

                /// <summary>
                /// Create object to expand
                /// </summary>
                /// <param name="objectFromBrowse"></param>
                /// <param name="originalNode"></param>
                public ObjectToExpand(BrowseFrame objectFromBrowse,
                    NodeToExpand originalNode)
                {
                    ObjectFromBrowse = objectFromBrowse;
                    OriginalNode = originalNode;
                }

                public bool EntriesAlreadyReturned { get; internal set; }

                public bool ContainsVariables => _variables.Count > 0;

                /// <summary>
                /// Add variables
                /// </summary>
                /// <param name="frames"></param>
                /// <returns></returns>
                public bool AddVariables(IEnumerable<BrowseFrame> frames)
                {
                    var duplicates = false;
                    foreach (var frame in frames)
                    {
                        duplicates |= !_variables.Add(frame);
                    }
                    return duplicates;
                }

                /// <summary>
                /// Add events
                /// </summary>
                /// <param name="frames"></param>
                /// <returns></returns>
                public bool AddEvents(IEnumerable<BrowseFrame> frames)
                {
                    var duplicates = false;
                    foreach (var frame in frames)
                    {
                        duplicates |= !_events.Add(frame);
                    }
                    return duplicates;
                }

                /// <summary>
                /// Get node models
                /// </summary>
                /// <param name="template"></param>
                /// <param name="context"></param>
                /// <param name="ids"></param>
                /// <param name="createLongIds"></param>
                /// <returns></returns>
                public IEnumerable<OpcNodeModel> GetOpcNodeModels(OpcNodeModel template,
                    IServiceMessageContext context, HashSet<string?>? ids = null,
                    bool createLongIds = false)
                {
                    ids ??= [];
                    if (EntriesAlreadyReturned)
                    {
                        return [];
                    }
                    return _variables.Select(frame => template with
                    {
                        Id = frame.NodeId.AsString(context, NamespaceFormat.Expanded),
                        AttributeId = null, // Defaults to variable
                        DataSetFieldId = CreateUniqueId(frame),
                    });

                    string CreateUniqueId(BrowseFrame frame)
                    {
                        var id = template.DataSetFieldId ?? string.Empty;
                        id = createLongIds ?
                            $"{id}{ObjectFromBrowse.BrowsePath}{frame.BrowsePath}" :
                            $"{id}{frame.BrowsePath}";
                        var uniqueId = id;
                        for (var index = 1; !ids.Add(uniqueId); index++)
                        {
                            uniqueId = $"{id}_{index}";
                        }
                        return uniqueId;
                    }
                }

                /// <summary>
                /// Create writer id for the object
                /// </summary>
                /// <returns></returns>
                public string CreateWriterId()
                {
                    var sb = new StringBuilder();
                    if (OriginalNode.NodeFromConfiguration.DataSetFieldId != null)
                    {
                        sb = sb.Append(OriginalNode.NodeFromConfiguration.DataSetFieldId);
                    }
                    return sb.Append(ObjectFromBrowse.BrowsePath).ToString();
                }

                /// <summary>
                /// Create data set name for the object
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                public string CreateDataSetName(IServiceMessageContext context)
                {
                    var result = ObjectFromBrowse.NodeId.AsString(context,
                        NamespaceFormat.Expanded)!;
                    Debug.Assert(result != null);
                    return result;
                }

                private readonly HashSet<BrowseFrame> _variables = [];
                private readonly HashSet<BrowseFrame> _events = [];
            }

            private int _nodeIndex = -1;
            private ObjectToExpand? _currentObject;
            private readonly List<NodeToExpand> _expanded = [];
            private readonly PublishedNodesEntryModel _entry;
            private readonly PublishedNodeExpansionModel _request;
            private readonly IPublishedNodesServices? _configuration;
            private readonly ILogger _logger;
            private readonly bool _allowNoResolution;
        }

        private readonly IPublishedNodesServices _configuration;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly ILogger<ConfigurationServices> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
