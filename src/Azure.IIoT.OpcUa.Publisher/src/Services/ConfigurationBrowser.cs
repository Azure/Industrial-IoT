// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
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
    /// Configuration browser
    /// </summary>
    internal sealed class ConfigurationBrowser : AsyncEnumerableBrowser<ServiceResponse<PublishedNodesEntryModel>>
    {
        /// <inheritdoc/>
        public ConfigurationBrowser(PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
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
            _logger.HandleError(node, errorInfo);
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
                    _logger.DroppedDuplicateVariables();
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
                ConfigurationServices.ValidateNodes(entry.Result.OpcNodes);
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
        internal record class NodeToExpand
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
        internal class ObjectToExpand
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

    /// <summary>
    /// Source-generated logging extensions for ConfigurationBrowser
    /// </summary>
    internal static partial class ConfigurationBrowserLogging
    {
        private const int EventClass = 100;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Error expanding node {Node}: {Error}")]
        public static partial void HandleError(this ILogger logger,
            ConfigurationBrowser.NodeToExpand node, ServiceResultModel error);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "Dropped duplicate variables found.")]
        public static partial void DroppedDuplicateVariables(this ILogger logger);
    }
}
