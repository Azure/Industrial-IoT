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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration services uses the address space and services of a connected server to
    /// configure the publisher. The configuration services allow interactive expansion of
    /// published nodes.
    /// </summary>
    public sealed class ConfigurationServices : IConfigurationServices
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
            _timeProvider = timeProvider;
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
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.OpcNodes);
            ValidateNodes(entry.OpcNodes);

            var browser = new ConfigBrowser(entry, request, _options, _configuration,
                _logger, _timeProvider);
            return _client.ExecuteAsync(entry.ToConnectionModel(), browser, request.Header, ct);
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
#if DEBUG
                if (set.Contains(node.DataSetFieldId))
                {
                    Debug.Fail($"{node.DataSetFieldId} already exists");
                }
#endif
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
                TimeProvider? timeProvider = null) : base(request.Header, options, timeProvider)
            {
                _entry = entry;
                _request = request;
                _configuration = configuration;
                _logger = logger;
            }

            /// <inheritdoc/>
            public override void Reset()
            {
                base.Reset();

                _nodeIndex = -1;
                _expanded.Clear();

                Push(context => BeginAsync(context));
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
                CurrentNode.AddErrorInfo(errorInfo);
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleMatching(
                ServiceCallContext context, IReadOnlyList<BrowseFrame> matching)
            {
                if (_currentObject != null)
                {
                    // collect matching variables under the current object instance
                    _currentObject.AddVariables(matching);
                }
                else
                {
                    // collect matching object instances
                    CurrentNode.AddObjects(matching);
                }
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleCompletion(
                ServiceCallContext context)
            {
                Push(context => CompleteAsync(context));
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <summary>
            /// Complete the browse operation and resolve objects
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<ServiceResponse<PublishedNodesEntryModel>>> CompleteAsync(
                ServiceCallContext context)
            {
                var entries = new List<ServiceResponse<PublishedNodesEntryModel>>();
                if (_currentObject != null)
                {
                    if (_currentObject.ContainsVariables &&
                        !_request.CreateSingleWriter && !_currentObject.OriginalNode.HasErrors)
                    {
                        // Create a new writer entry for the object
                        var entry = new ServiceResponse<PublishedNodesEntryModel>
                        {
                            Result = _entry with
                            {
                                DataSetName = _currentObject.CreateDataSetName(
                                    context.Session.MessageContext),
                                DataSetWriterId = _currentObject.CreateWriterId(
                                    context.Session.MessageContext),
                                OpcNodes = _currentObject
                                    .GetOpcNodeModels(
                                        _currentObject.OriginalNode.NodeFromConfiguration,
                                        context.Session.MessageContext)
                                    .ToList()
                            }
                        };

                        await SaveEntryAsync(entry, context.Ct).ConfigureAwait(false);
                        if (!_request.DiscardErrors || entry.ErrorInfo == null)
                        {
                            // Add good entry to return _now_
                            entries.Add(entry);
                            _currentObject.EntriesAlreadyReturned = true;
                        }
                    }

                    if (TryMoveToNextObject())
                    {
                        // Kicked off the next variable expansion
                        return entries;
                    }
                    Debug.Assert(_currentObject == null);
                }

                // Completing a browse for objects
                else if (!CurrentNode.ContainsObjects)
                {
                    if (!CurrentNode.HasErrors)
                    {
                        CurrentNode.AddErrorInfo(StatusCodes.BadNotFound, "No objects resolved.");
                    }
                }

                if (!TryMoveToNextNode())
                {
                    // Complete
                    entries.AddRange(await EndAsync(context).ConfigureAwait(false));
                }
                return entries;
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
                        _expanded.Add(new NodeToExpand(node, nodeId));
                    }

                    // Resolve node classes
                    var results = await context.Session.ReadAttributeAsync<int>(
                        _request.Header.ToRequestHeader(TimeProvider),
                        _expanded.Select(r => r.NodeId ?? NodeId.Null),
                        (uint)NodeAttribute.NodeClass, context.Ct).ConfigureAwait(false);
                    foreach (var result in results.Zip(_expanded))
                    {
                        result.Second.AddErrorInfo(result.First.Item2);
                        result.Second.NodeClass = (uint)result.First.Item1;
                    }
                    if (!TryMoveToNextNode())
                    {
                        // Complete
                        return await EndAsync(context).ConfigureAwait(false);
                    }
                }
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <summary>
            /// Return remaining entries
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
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
                    var entry = new ServiceResponse<PublishedNodesEntryModel>
                    {
                        Result = _entry with { OpcNodes = goodNodes }
                    };
                    await SaveEntryAsync(entry, context.Ct).ConfigureAwait(false);
                    if (!_request.DiscardErrors || entry.ErrorInfo == null)
                    {
                        // Add good entry
                        results.Add(entry);
                    }
                }
                if (!_request.DiscardErrors)
                {
                    var badNodes = _expanded
                        .Where(e => e.HasErrors)
                        .SelectMany(e => e.ErrorInfos
                            .Select(error => (error, e
                                .GetAllOpcNodeModels(context.Session.MessageContext, ids)
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
                            if (!_request.ExcludeRootObject)
                            {
                                CurrentNode.AddObjects(
                                    new BrowseFrame(CurrentNode.NodeId!, null, null).YieldReturn());

                                if (_request.MaxDepth == 0)
                                {
                                    // We have the object - browse it now
                                    return TryMoveToNextObject();
                                }
                            }
                            var depth = _request.MaxDepth == 0 ? 1 : _request.MaxDepth;
                            var refTypeId = _request.StopAtFirstFoundInstance ?
                               ReferenceTypeIds.Organizes : ReferenceTypeIds.HierarchicalReferences;
                            Restart(CurrentNode.NodeId, maxDepth: depth, referenceTypeId: refTypeId);
                            return true;
                        case (uint)Opc.Ua.NodeClass.VariableType:
                        case (uint)Opc.Ua.NodeClass.ObjectType:
                            // Resolve all objects of this type
                            Debug.Assert(!NodeId.IsNull(CurrentNode.NodeId));
                            var instanceClass =
                                CurrentNode.NodeClass == (uint)Opc.Ua.NodeClass.ObjectType ?
                                    Opc.Ua.NodeClass.Object : Opc.Ua.NodeClass.Variable;

                            // If stop at first found we only need to use organizes references
                            var referenceTypeId =
                                _request.StopAtFirstFoundInstance &&
                                    instanceClass == Opc.Ua.NodeClass.Object ?
                                ReferenceTypeIds.Organizes : ReferenceTypeIds.HierarchicalReferences;

                            Restart(ObjectIds.ObjectsFolder, maxDepth: _request.MaxDepth,
                                typeDefinitionId: CurrentNode.NodeId,
                                stopWhenFound: _request.StopAtFirstFoundInstance,
                                referenceTypeId: referenceTypeId, nodeClass: instanceClass);
                            return true;
                        case (uint)Opc.Ua.NodeClass.Variable:
                            // Done - already a variable - stays in the original entry
                            break;
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
                        Restart(_currentObject.ObjectFromBrowse.NodeId,
                            _request.MaxLevelsToExpand == 0 ? null : _request.MaxLevelsToExpand,
                            referenceTypeId: ReferenceTypeIds.Aggregates,
                            nodeClass: Opc.Ua.NodeClass.Variable);
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
            private async ValueTask SaveEntryAsync(ServiceResponse<PublishedNodesEntryModel> entry,
                CancellationToken ct)
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
                }
                catch (Exception ex)
                {
                    entry.ErrorInfo = ex.ToServiceResultModel();
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
            /// <param name="NodeFromConfiguration"></param>
            /// <param name="NodeId"></param>
            private record class NodeToExpand(OpcNodeModel NodeFromConfiguration, NodeId? NodeId)
            {
                public uint NodeClass { get; internal set; }

                public IEnumerable<ServiceResultModel> ErrorInfos => _errorInfos;

                public bool HasErrors => _errorInfos.Count > 0;

                public bool ContainsObjects => _objects.Count > 0;

                /// <summary>
                /// Opc node model configurations over all objects
                /// </summary>
                /// <param name="context"></param>
                /// <param name="ids"></param>
                /// <returns></returns>
                public IEnumerable<OpcNodeModel> GetAllOpcNodeModels(IServiceMessageContext context,
                    HashSet<string?>? ids = null)
                {
                    switch (NodeClass)
                    {
                        case (uint)Opc.Ua.NodeClass.Variable:
                            if (ids?.Contains(NodeFromConfiguration.DataSetFieldId) == true)
                            {
                                // TODO: Add error info
                                return Enumerable.Empty<OpcNodeModel>();
                            }
                            return new[] { NodeFromConfiguration };
                        case (uint)Opc.Ua.NodeClass.Object:
                        case (uint)Opc.Ua.NodeClass.ObjectType:
                        case (uint)Opc.Ua.NodeClass.VariableType:
                            return _objects
                                .Where(o => !o.EntriesAlreadyReturned)
                                .SelectMany(o => o.GetOpcNodeModels(
                                    NodeFromConfiguration, context, ids, true));
                        default:
                            return Enumerable.Empty<OpcNodeModel>();
                    }
                }

                /// <summary>
                /// Add objects
                /// </summary>
                /// <param name="frames"></param>
                public void AddObjects(IEnumerable<BrowseFrame> frames)
                {
                    _objects.AddRange(frames
                        .Where(f => !NodeId.IsNull(f.NodeId) && _knownObjects.Add(f.NodeId))
                        .Select(f => new ObjectToExpand(f, this)));
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

                private readonly List<ServiceResultModel> _errorInfos = new();
                private readonly List<ObjectToExpand> _objects = new();
                private readonly HashSet<NodeId> _knownObjects = new();
                private int _objectIndex;
            }

            /// <summary>
            /// The object to expand
            /// </summary>
            /// <param name="ObjectFromBrowse"></param>
            /// <param name="OriginalNode"></param>
            private record class ObjectToExpand(BrowseFrame ObjectFromBrowse, NodeToExpand OriginalNode)
            {
                public bool EntriesAlreadyReturned { get; internal set; }

                public bool ContainsVariables => _variables.Count > 0;

                /// <summary>
                /// Add variables
                /// </summary>
                /// <param name="frames"></param>
                public void AddVariables(IEnumerable<BrowseFrame> frames)
                {
                    foreach (var frame in frames)
                    {
                        var added = _variables.Add(frame);
#if DEBUG
                        Debug.Assert(added, $"Variable {frame} already exists");
#endif
                    }
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
                    ids ??= new HashSet<string?>();
                    if (EntriesAlreadyReturned)
                    {
                        return Enumerable.Empty<OpcNodeModel>();
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
                /// <param name="context"></param>
                /// <returns></returns>
                public string CreateWriterId(IServiceMessageContext context)
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

                private readonly HashSet<BrowseFrame> _variables = new();
            }

            private int _nodeIndex = -1;
            private ObjectToExpand? _currentObject;
            private readonly List<NodeToExpand> _expanded = new();
            private readonly PublishedNodesEntryModel _entry;
            private readonly PublishedNodeExpansionModel _request;
            private readonly IPublishedNodesServices? _configuration;
            private readonly ILogger _logger;
        }

        private readonly IPublishedNodesServices _configuration;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly ILogger<ConfigurationServices> _logger;
        private readonly TimeProvider? _timeProvider;
    }
}
