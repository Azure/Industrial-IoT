// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Serializers;
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
            ServiceCallContext context, IReadOnlyList<BrowseFrame> matching,
            List<ReferenceDescription> references)
        {
            uint originalNodeClass;
            if (_currentObject == null)
            {
                // collect matching object instances
                originalNodeClass = CurrentNode.NodeClass;
                CurrentNode.AddObjectsOrVariables(matching);
            }
            else
            {
                originalNodeClass = _currentObject.OriginalNode.NodeClass;
                // collect matching variables under the current object instance
                var nodes = matching
                    .Where(m => m.NodeClass is Opc.Ua.NodeClass.Variable or Opc.Ua.NodeClass.Method);
                var objects = matching
                    .Where(m => m.NodeClass == Opc.Ua.NodeClass.Object);
                if (_currentObject.AddNodes(nodes))
                {
                    _logger.DroppedDuplicateItems();
                }
                // Add components of the current object - these will be expanded
                // when we move to next object.
                _currentObject.OriginalNode.AddObjectsOrVariables(objects);
                if (originalNodeClass == (uint)Opc.Ua.NodeClass.ObjectType && !_request.FlattenTypeInstance)
                {
                    // Only expand variables of current object in match loop
                    references.RemoveAll(m => m.NodeClass == Opc.Ua.NodeClass.Object);
                }
            }
            if ((originalNodeClass == (uint)Opc.Ua.NodeClass.ObjectType && _request.FlattenTypeInstance) ||
                originalNodeClass == (uint)Opc.Ua.NodeClass.VariableType)
            {
                // Browse deeper for other variables of the type
                var stop = matching.Select(r => r.NodeId).ToHashSet();
                references.RemoveAll(r => stop.Contains((NodeId)r.NodeId));
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
                // Completing the browse operation for variables of variables
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
                await currentObject.CompleteAsync(_request.Header.ToRequestHeader(TimeProvider),
                    context).ConfigureAwait(false);

                if (!_request.CreateSingleWriter &&
                    (currentObject.ContainsVariables || currentObject.ContainsMethods || currentObject.ContainsEvents) &&
                    !currentObject.OriginalNode.HasErrors)
                {
                    // Create a new writer entry for the object
                    var root = currentObject.ObjectFromBrowse.RootFrame;
                    var result = await SaveEntryAsync(new ServiceResponse<PublishedNodesEntryModel>
                    {
                        Result = _entry with
                        {
                            DataSetWriterId = currentObject.CreateWriterId(), // Unique
                            DataSetWriterGroup = _entry.DataSetWriterGroup ?? root?.BrowseName?.Name,
                            // Name of the dataset with DataSetWriterGroup as root
                            DataSetName = currentObject.CreateDataSetName(root),
                            DataSetRootNodeId = currentObject.ObjectFromBrowse.NodeId?.AsString(
                                context.Session.MessageContext, NamespaceFormat.Expanded),
                            // Type of the dataset
                            DataSetType = currentObject.ObjectFromBrowse.TypeDefinitionId?.AsString(
                                context.Session.MessageContext, NamespaceFormat.ExpandedWithNamespace0),
                            // Type of the writer group
                            WriterGroupType = root?.TypeDefinitionId?.AsString(
                                context.Session.MessageContext, NamespaceFormat.ExpandedWithNamespace0),
                            WriterGroupRootNodeId = root?.NodeId?.AsString(
                                context.Session.MessageContext, NamespaceFormat.Expanded),
                            OpcNodes = currentObject
                                .GetOpcNodeModels(
                                    currentObject.OriginalNode.NodeFromConfiguration,
                                    context.Session.MessageContext, createLongIds: false)
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
                    try
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
                            browseName, displayName, typeDefinitionId, errorInfo));
                    }
                    catch (Exception e)
                    {
                        _expanded.Add(new NodeToExpand(node, NodeId.Null, Opc.Ua.NodeClass.Unspecified,
                            null, null, null, e.ToServiceResultModel()));
                    }
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
                        Restart(
                            CurrentNode.NodeId == null ? null : new BrowseFrame(CurrentNode.NodeId),
                            maxDepth: depth, referenceTypeId: ReferenceTypeIds.HierarchicalReferences);
                        return true;
                    case (uint)Opc.Ua.NodeClass.VariableType:
                    case (uint)Opc.Ua.NodeClass.ObjectType:
                        // Resolve all objects of this type
                        Debug.Assert(!NodeId.IsNull(CurrentNode.NodeId));
                        var instanceClass =
                            CurrentNode.NodeClass == (uint)Opc.Ua.NodeClass.ObjectType ?
                                Opc.Ua.NodeClass.Object : Opc.Ua.NodeClass.Variable;
                        Restart(null, maxDepth: _request.MaxDepth,
                            typeDefinitionId: CurrentNode.NodeId,
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
                        Restart(CurrentNode.NodeId == null ? null : new BrowseFrame(CurrentNode.NodeId),
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
                    //
                    // Now we are at the level where we expand the variables and optionally methods of
                    // the object. This will match variables and variables in variables (properties).
                    // as well as methods if included.
                    //
                    var nodeClass = Opc.Ua.NodeClass.Variable;
                    var matchClass = Opc.Ua.NodeClass.Variable;
                    var maxDepth = _request.MaxLevelsToExpand != 0 ? _request.MaxLevelsToExpand : (uint?)null;
                    if (_request.IncludeMethods)
                    {
                        // Include methods in the match
                        matchClass |= Opc.Ua.NodeClass.Method;
                    }

                    //
                    // If the original node class was object type we also search for sub components
                    // of the object found (other aggregates). We match variables if we flatten
                    // and objects and variables when we want to create individual entries per object
                    //
                    if (_currentObject.OriginalNode.NodeClass == (uint)Opc.Ua.NodeClass.ObjectType)
                    {
                        nodeClass |= Opc.Ua.NodeClass.Object;
                        if (!_request.FlattenTypeInstance)
                        {
                            // Match not just variables but also objects and expand them
                            matchClass |= Opc.Ua.NodeClass.Object;
                        }
                        // maxDepth = null;
                    }
                    Restart(_currentObject.ObjectFromBrowse, maxDepth,
                        referenceTypeId: ReferenceTypeIds.Aggregates, nodeClass: nodeClass,
                        matchClass: matchClass);
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
            /// <param name="typeDefinitionId"></param>
            /// <param name="errorInfo"></param>
            public NodeToExpand(OpcNodeModel nodeFromConfiguration, NodeId? nodeId,
                Opc.Ua.NodeClass nodeClass, QualifiedName? browseName, LocalizedText? displayName,
                ExpandedNodeId? typeDefinitionId, ServiceResultModel? errorInfo)
            {
                NodeFromConfiguration = nodeFromConfiguration;
                NodeId = nodeId;
                NodeClass = (uint)nodeClass;

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
                        Variables.AddNodes(frames);
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

            public bool ContainsMethods => _methods.Count > 0;

            public bool ContainsEvents => _eventTypesGenerated.Count > 0;

            /// <summary>
            /// Add variables
            /// </summary>
            /// <param name="frames"></param>
            /// <returns></returns>
            public bool AddNodes(IEnumerable<BrowseFrame> frames)
            {
                var duplicates = false;
                foreach (var frame in frames)
                {
                    if (NodeId.IsNull(frame.NodeId))
                    {
                        continue;
                    }
                    if (!_knownIds.Add(frame.NodeId))
                    {
                        duplicates |= true;
                        continue;
                    }
                    if (frame.NodeClass == Opc.Ua.NodeClass.Method)
                    {
                        // Add methods
                        duplicates |= !_methods.Add(frame);
                    }
                    else
                    {
                        if (frame.Parent?.NodeId != null)
                        {
                            // Collect input and output arguments for later use
                            if (frame.BrowseName == BrowseNames.InputArguments)
                            {
                                _input.AddOrUpdate(frame.Parent.NodeId,
                                    new MethodArgument(frame));
                                break;
                            }
                            else if (frame.BrowseName == BrowseNames.OutputArguments)
                            {
                                _output.AddOrUpdate(frame.Parent.NodeId,
                                    new MethodArgument(frame));
                                break;
                            }
                        }
                        // Add variable
                        duplicates |= !_variables.Add(frame);
                    }
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

                // Get variable nodes
                var nodeModels = _variables.Select(variableFrame => template with
                {
                    // Use absolute nodes
                    Id = variableFrame.NodeId.AsString(context, NamespaceFormat.Expanded),
                    AttributeId = NodeAttribute.Value,

                    DataSetFieldId = CreateUniqueIdFromFrame(variableFrame.BrowsePath),
                    DisplayName = variableFrame.BrowseName?.Name ?? variableFrame.DisplayName,

                    // TODO - use browse paths instead:
                    // Id = ObjectFromBrowse.NodeId.AsString(context, NamespaceFormat.Expanded)
                    // BrowsePath = frame.BrowsePath.ToRelativePath(out var prefix).AsString(prefix),

                    TypeDefinitionId = variableFrame.TypeDefinitionId?.AsString(context,
                        NamespaceFormat.ExpandedWithNamespace0)
                });

                // Add methods if any
                if (_eventTypesGenerated.Count != 0)
                {
                    nodeModels = nodeModels.Concat(_eventTypesGenerated
                        .SelectMany(eventSource => eventSource.Value.Select(eventType => template with
                        {
                            Id = _eventNotifier.AsString(context, NamespaceFormat.Expanded),
                            AttributeId = NodeAttribute.EventNotifier,

                            // DataSetClassFieldId = CreateUniqueIdFromFrame(_eventNotifierBrowsePath,
                            //     evt.BrowseName),
                            DataSetFieldId = CreateUniqueIdFromFrame(eventSource.Key.BrowsePath,
                                eventType.BrowseName),
                            DisplayName = eventType.BrowseName?.Name ?? eventType.DisplayName.Text,

                            // TODO: Set up the event filter to filter the source node and event type
                            // EventFilter = new EventFilterModel(),

                            // TODO - use browse paths instead:
                            // Id = ObjectFromBrowse.NodeId.AsString(context, NamespaceFormat.Expanded)
                            // BrowsePath = frame.BrowsePath.ToRelativePath(out var prefix).AsString(prefix),

                            TypeDefinitionId = eventType.NodeId.AsString(context,
                                NamespaceFormat.ExpandedWithNamespace0)
                        })));
                }

                // Add methods if any
                if (_methods.Count != 0)
                {
                    nodeModels = nodeModels.Concat(_methods.Select(methodFrame => template with
                    {
                        Id = methodFrame.NodeId.AsString(context, NamespaceFormat.Expanded),
                        DataSetFieldId = CreateUniqueIdFromFrame(methodFrame.BrowsePath),
                        DisplayName = methodFrame.BrowseName?.Name ?? methodFrame.DisplayName,
                        // TODO - use browse paths instead:
                        // Id = ObjectFromBrowse.NodeId.AsString(context, NamespaceFormat.Expanded)
                        // BrowsePath = frame.BrowsePath.ToRelativePath(out var prefix).AsString(prefix),

                        MethodMetadata = new MethodMetadataModel
                        {
                            InputArguments = _input.TryGetValue(methodFrame.NodeId, out var input)
                                ? input.Arguments : [],
                            OutputArguments = _output.TryGetValue(methodFrame.NodeId, out var output)
                                ? output.Arguments : [],
                            ObjectId = methodFrame.Parent?.NodeId?.AsString(context, NamespaceFormat.Expanded)
                        },
                        TypeDefinitionId = methodFrame.TypeDefinitionId?.AsString(context,
                            NamespaceFormat.ExpandedWithNamespace0)
                    }));
                }

                string CreateUniqueIdFromFrame(string? browsePath, QualifiedName? extra = null)
                {
                    var id = template.DataSetFieldId ?? string.Empty;
                    id = createLongIds ?
                        $"{id}{ObjectFromBrowse.BrowsePath}{browsePath ?? string.Empty}" :
                        $"{id}{browsePath ?? string.Empty}";
                    if (extra != null)
                    {
                        id = $"{id}/{extra.Name}";
                    }
                    var uniqueId = id;
                    for (var index = 1; !ids.Add(uniqueId); index++)
                    {
                        uniqueId = $"{id}_{index}";
                    }
                    return uniqueId;
                }
                return nodeModels;
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
            /// Create data set name for the object that is rooted in
            /// the writer group structurally. We use . seperator to
            /// create names that can be reused in topics and paths.
            /// </summary>
            /// <param name="root"></param>
            /// <returns></returns>
            public string CreateDataSetName(BrowseFrame? root)
            {
                var cur = ObjectFromBrowse;
                if (cur.BrowseName?.Name == null || cur == root)
                {
                    return "Default";
                }

                var result = cur.BrowseName.Name;
                cur = cur.Parent;
                while (cur != null && cur != root)
                {
                    Debug.Assert(cur.BrowseName?.Name != null);
                    result = cur.BrowseName.Name + "." + result;
                    cur = cur.Parent;
                }
                return result;
            }

            /// <summary>
            /// Complete object
            /// </summary>
            /// <param name="requestHeader"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            internal async Task CompleteAsync(RequestHeader requestHeader, ServiceCallContext context)
            {
                //
                // Find events anything in this set generates, then find the event notifier for them
                // The SourceNode of GeneratesEvent and AlwaysGeneratesEvent is limited to ObjectType,
                // VariableType AND Method InstanceDeclaration Nodes on ObjectTypes (not objects!).
                //
                try
                {
                    var browseDescriptions = _variables
                        .Concat(_methods)
                        .Append(ObjectFromBrowse)
                        .Where(t => !NodeId.IsNull(t.TypeDefinitionId))
                        .Select(t => new BrowseDescription
                        {
                            Handle = t,
                            NodeId = ExpandedNodeId.ToNodeId(
                                t.TypeDefinitionId, context.Session.MessageContext.NamespaceUris)!,
                            ReferenceTypeId = ReferenceTypeIds.GeneratesEvent,
                            IncludeSubtypes = true,
                            BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                            NodeClassMask = (uint)Opc.Ua.NodeClass.ObjectType,
                            ResultMask = (uint)BrowseResultMask.All
                        })
                        .ToArray();

                    _eventTypesGenerated.Clear();
                    if (browseDescriptions.Length != 0)
                    {
                        await foreach (var result in context.Session.BrowseAsync(requestHeader, null,
                            browseDescriptions, context.Ct).ConfigureAwait(false))
                        {
                            if (result.ErrorInfo != null)
                            {
                                OriginalNode.AddErrorInfo(result.ErrorInfo);
                                continue;
                            }
                            Debug.Assert(result.References != null);
                            Debug.Assert(result.Description != null);
                            if (result.References.Count == 0)
                            {
                                // No events generated
                                continue;
                            }
                            var originalFrame = (BrowseFrame)result.Description.Handle;
                            if (!_eventTypesGenerated.TryGetValue(originalFrame, out var eventList))
                            {
                                eventList = [];
                                _eventTypesGenerated.Add(originalFrame, eventList);
                            }
                            eventList.AddRange(result.References);
                        }
                    }
                    if (_eventTypesGenerated.Count > 0)
                    {
                        // Find the event notifier. This should use HasEventSource if possible, but we just
                        // try and find it in the objects to the root here
                        var readValueIds = ObjectFromBrowse.AllFramesToRoot
                            .Where(f => f.NodeClass == Opc.Ua.NodeClass.Object && !NodeId.IsNull(f.NodeId))
                            .Select(f => new ReadValueId
                            {
                                Handle = f,
                                NodeId = f.NodeId,
                                AttributeId = Attributes.EventNotifier
                            })
                            .ToArray();
                        _eventNotifier = ObjectIds.Server;
                        _eventNotifierBrowsePath = null;
                        if (readValueIds.Length != 0)
                        {
                            var response = await context.Session.Services.ReadAsync(requestHeader, 0,
                                Opc.Ua.TimestampsToReturn.Neither, readValueIds, context.Ct).ConfigureAwait(false);
                            var readResults = response.Validate(response.Results, s => s.StatusCode,
                                response.DiagnosticInfos, readValueIds);
                            if (readResults.ErrorInfo != null)
                            {
                                OriginalNode.AddErrorInfo(readResults.ErrorInfo);
                            }
                            else
                            {
                                var result = readResults.FirstOrDefault(IsSubscribeToEvents);
                                if (result != null)
                                {
                                    _eventNotifier = result.Request.NodeId;
                                    _eventNotifierBrowsePath = ((BrowseFrame)result.Request.Handle).BrowsePath;
                                }
                            }
                        }

                        static bool IsSubscribeToEvents(ServiceResponse<ReadValueId, DataValue>.Operation operation)
                        {
                            if (operation.ErrorInfo != null)
                            {
                                return false;
                            }
                            var eventNotifier = operation.Result.GetValue<byte>(0);
                            if ((eventNotifier & EventNotifiers.SubscribeToEvents) != 0)
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OriginalNode.AddErrorInfo(ex.ToServiceResultModel());
                }

                if (ContainsMethods)
                {
                    foreach (var input in _input.Values)
                    {
                        await input.ExpandAsync(context, requestHeader).ConfigureAwait(false);
                        if (input.ErrorInfo != null)
                        {
                            // If input argument cannot be resolved, we do not
                            // return it as part of the metadata.
                            _input.Remove(input.BrowseFrame.NodeId);
                            OriginalNode.AddErrorInfo(input.ErrorInfo);
                        }
                    }
                    foreach (var output in _output.Values)
                    {
                        await output.ExpandAsync(context, requestHeader).ConfigureAwait(false);
                        if (output.ErrorInfo != null)
                        {
                            // If output argument cannot be resolved, we do not
                            // return it as part of the metadata.
                            _output.Remove(output.BrowseFrame.NodeId);
                            OriginalNode.AddErrorInfo(output.ErrorInfo);
                        }
                    }
                }
            }

            private readonly HashSet<NodeId> _knownIds = [];
            private readonly HashSet<BrowseFrame> _methods = [];
            private readonly Dictionary<NodeId, MethodArgument> _input = [];
            private readonly Dictionary<NodeId, MethodArgument> _output = [];
            private readonly HashSet<BrowseFrame> _variables = [];
            private readonly Dictionary<BrowseFrame, List<ReferenceDescription>> _eventTypesGenerated = [];
            private NodeId _eventNotifier = ObjectIds.Server;
            private string? _eventNotifierBrowsePath;
        }

        /// <summary>
        /// Respresents a method argument.
        /// </summary>
        /// <param name="BrowseFrame"></param>
        internal record MethodArgument(BrowseFrame BrowseFrame)
        {
            /// <summary>
            /// Error info if not resolvable
            /// </summary>
            public ServiceResultModel? ErrorInfo { get; private set; } = kDefaultError;

            /// <summary>
            /// The arguments in order
            /// </summary>
            public List<MethodMetadataArgumentModel> Arguments { get; } = [];

            /// <summary>
            /// Resolve the argument
            /// </summary>
            /// <param name="context"></param>
            /// <param name="header"></param>
            /// <returns></returns>
            public async ValueTask ExpandAsync(ServiceCallContext context, RequestHeader header)
            {
                var (value, errorInfo) = await context.Session.ReadValueAsync(header, BrowseFrame.NodeId,
                    context.Ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    ErrorInfo = errorInfo;
                    return;
                }
                ErrorInfo = null; // Mark as processed
                if (value?.Value is not ExtensionObject[] argumentsList)
                {
                    return;
                }
                Arguments.Clear();
                foreach (var argument in argumentsList.Select(a => (Argument)a.Body))
                {
                    var (dataTypeIdNode, errorInfo2) = await context.Session.ReadNodeAsync(
                        header, argument.DataType, null, false, false, NamespaceFormat.Expanded,
                        false, context.Ct).ConfigureAwait(false);
                    var arg = new MethodMetadataArgumentModel
                    {
                        Name = argument.Name,
                        DefaultValue = argument.Value == null ? VariantValue.Null :
                            context.Session.Codec.Encode(new Variant(argument.Value), out var type),
                        ValueRank = argument.ValueRank == ValueRanks.Scalar ?
                            null : (global::Azure.IIoT.OpcUa.Publisher.Models.NodeValueRank)argument.ValueRank,
                        ArrayDimensions = argument.ArrayDimensions?.Count > 0 ?
                            argument.ArrayDimensions?.ToArray() : null,
                        Description = string.IsNullOrEmpty(argument?.Description.Text) ?
                            null : argument.Description.Text,
                        ErrorInfo = errorInfo2,
                        Type = dataTypeIdNode with
                        {
                            // Compress by removing non relevant fields
                            WriteMask = null,
                            UserAccessLevel = null,
                            UserWriteMask = null,
                            AccessLevel = null,
                            Children = null,
                            SourcePicoseconds = null,
                            ServerPicoseconds = null,
                            ServerTimestamp = null,
                            SourceTimestamp = null,
                        }
                    };
                    Arguments.Add(arg);
                }
            }

            private static readonly ServiceResultModel kDefaultError =
                ((ServiceResult)StatusCodes.BadInternalError).ToServiceResultModel();
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
            Message = "Dropped duplicate variables or methods found.")]
        public static partial void DroppedDuplicateItems(this ILogger logger);
    }
}
