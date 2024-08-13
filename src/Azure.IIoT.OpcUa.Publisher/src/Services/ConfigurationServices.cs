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
        public ConfigurationServices(IPublisherConfiguration configuration,
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
            PublishedNodeExpansionRequestModel request, bool noUpdate, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.OpcNodes);
            ValidateNodes(request.Entry.OpcNodes);

            var browser = new ConfigBrowser(request, _options, noUpdate ? null : _configuration,
                _timeProvider);
            return _client.ExecuteAsync(request.Entry.ToConnectionModel(), browser,
                request.Header, ct);
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
        internal sealed class ConfigBrowser : ObjectBrowser<ServiceResponse<PublishedNodesEntryModel>>
        {
            /// <inheritdoc/>
            public ConfigBrowser(PublishedNodeExpansionRequestModel request,
                IOptions<PublisherOptions> options, IPublisherConfiguration? configuration,
                TimeProvider? timeProvider = null) : base(request.Header, options, null, null, false,
                      request.LevelsToExpand ?? int.MaxValue, request.NoSubtypes, timeProvider)
            {
                _request = request;
                _configuration = configuration;
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
            protected override void Restart()
            {
                // We handle our own
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleError(
                ServiceCallContext context, ServiceResultModel errorInfo)
            {
                _expanded[_nodeIndex].ErrorInfos.Add(errorInfo);
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleMatching(
                ServiceCallContext context, IReadOnlyList<ReferenceDescription> matching)
            {
                // Add items
                _expanded[_nodeIndex].Objects.AddRange(matching.Select(r => new ExpandedObject(r)));
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleCompletion(
                ServiceCallContext context)
            {
                // The browse operation completed and we have all objects that we found
                Push(context => CompleteAsync(context));
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
                var goodNodes = _expanded
                    .Where(e => e.ErrorInfos.Count == 0)
                    .SelectMany(r => r.Remaining)
                    .ToList();
                if (goodNodes.Count > 0)
                {
                    var entry = new ServiceResponse<PublishedNodesEntryModel>
                    {
                        Result = _request.Entry with
                        {
                            OpcNodes = goodNodes
                        }
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
                        .Where(e => e.ErrorInfos.Count > 0)
                        .SelectMany(e => e.ErrorInfos
                            .Select(error => (error, e.Remaining.ToList())))
                        .GroupBy(e => e.error)
                        .SelectMany(r => r.Select(r => r))
                        .ToList();
                    foreach (var entry in badNodes)
                    {
                        // Return bad entries
                        results.Add(new ServiceResponse<PublishedNodesEntryModel>
                        {
                            ErrorInfo = entry.error,
                            Result = _request.Entry with
                            {
                                OpcNodes = entry.Item2
                            }
                        });
                    }
                }
                _expanded.Clear();
                return results;
            }

            /// <summary>
            /// Complete the browse operation and resolve objects
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            private async ValueTask<IEnumerable<ServiceResponse<PublishedNodesEntryModel>>> CompleteAsync(
                ServiceCallContext context)
            {
                Debug.Assert(_nodeIndex < _expanded.Count);
                if (_expanded[_nodeIndex].Objects.Count == 0)
                {
                    if (_expanded[_nodeIndex].ErrorInfos.Count == 0)
                    {
                        _expanded[_nodeIndex].ErrorInfos.Add(new ServiceResultModel
                        {
                            ErrorMessage = "No objects resolved.",
                            StatusCode = StatusCodes.BadNotFound
                        });
                    }
                    // Try browse more
                    if (!TryMoveToNextNode())
                    {
                        // Complete
                        return await EndAsync(context).ConfigureAwait(false);
                    }
                    return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
                }

                // Now resolve all variables under the objects and create new entries
                var entries = new List<ServiceResponse<PublishedNodesEntryModel>>();
                foreach (var obj in _expanded[_nodeIndex].Objects)
                {
                    var results = await context.Session.FindAsync(
                        _request.Header.ToRequestHeader(TimeProvider),
                        obj.ObjectToExpand.NodeId
                            .ToNodeId(context.Session.MessageContext.NamespaceUris)
                            .YieldReturn(),
                        ReferenceTypeIds.HasComponent, true,
                        nodeClassMask: (uint)Opc.Ua.NodeClass.Variable, // Only return variables
                        ct: context.Ct).ConfigureAwait(false);

                    if (results.Item2 != null)
                    {
                        _expanded[_nodeIndex].ErrorInfos.Add(results.Item2);
                        continue;
                    }

                    var objId = _request.Header.AsString(obj.ObjectToExpand.NodeId,
                        context.Session.MessageContext, Options);
                    foreach (var result in results.Item1)
                    {
                        if (result.ErrorInfo != null)
                        {
                            _expanded[_nodeIndex].ErrorInfos.Add(result.ErrorInfo);
                            continue;
                        }
                        if (NodeId.IsNull(result.Node) ||
                            result.NodeClass != Opc.Ua.NodeClass.Variable)
                        {
                            // TODO: Should we add an error here also?
                            continue;
                        }
                        var node = _expanded[_nodeIndex].NodeToExpand;
                        var name = _request.Header.AsString(result.Name,
                            context.Session.MessageContext, Options);
                        obj.Nodes.Add(node with
                        {
                            Id = _request.Header.AsString(result.Node,
                                context.Session.MessageContext, Options),
                            AttributeId = null,
                            DataSetFieldId = objId + "/" + name + "/" + obj.Nodes.Count
                        });
                    }

                    if (_expanded[_nodeIndex].ErrorInfos.Count == 0 && obj.Nodes.Count > 0
                        && !_request.CreateSingleWriter)
                    {
                        // Create a new writer entry for the object
                        var entry = new ServiceResponse<PublishedNodesEntryModel>
                        {
                            Result = _request.Entry with
                            {
                                DataSetName = objId,
                                DataSetWriterId = CreateSingleWriterId(objId),
                                OpcNodes = obj.Nodes.ToList()
                            }
                        };
                        await SaveEntryAsync(entry, context.Ct).ConfigureAwait(false);

                        if (!_request.DiscardErrors || entry.ErrorInfo == null)
                        {
                            // Add good entry to return _now_
                            entries.Add(entry);
                            obj.Nodes.Clear();
                            obj.EntriesAlreadyReturned = true;
                        }

                        string CreateSingleWriterId(string objId)
                        {
                            var sb = new StringBuilder();
                            if (_request.Entry.DataSetWriterId != null)
                            {
                                sb = sb
                                    .Append(_request.Entry.DataSetWriterId)
                                    .Append('|');
                            }
                            if (_expanded[_nodeIndex].NodeToExpand.DataSetFieldId != null)
                            {
                                sb = sb
                                    .Append(_expanded[_nodeIndex].NodeToExpand.DataSetFieldId)
                                    .Append('|');
                            }
                            return sb.Append(objId).ToString();
                        }
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
                if (_request.Entry.OpcNodes?.Count > 0)
                {
                    // TODO: Could be done in one request for better efficiency
                    foreach (var node in _request.Entry.OpcNodes)
                    {
                        var nodeId = await context.Session.ResolveNodeIdAsync(_request.Header,
                            node.Id, node.BrowsePath, nameof(node.BrowsePath), TimeProvider,
                            context.Ct).ConfigureAwait(false);
                        _expanded.Add(new ExpandedNode(node, nodeId));
                    }

                    // Resolve node classes
                    var results = await context.Session.ReadAttributeAsync<int>(
                        _request.Header.ToRequestHeader(TimeProvider),
                        _expanded.Select(r => r.NodeId ?? NodeId.Null),
                        (uint)NodeAttribute.NodeClass, context.Ct).ConfigureAwait(false);
                    foreach (var result in results.Zip(_expanded))
                    {
                        if (result.First.Item2 != null)
                        {
                            result.Second.ErrorInfos.Add(result.First.Item2);
                        }
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
            /// Try move to next node
            /// </summary>
            /// <returns></returns>
            private bool TryMoveToNextNode()
            {
                _nodeIndex++;
                for (; _nodeIndex < _expanded.Count; _nodeIndex++)
                {
                    switch (_expanded[_nodeIndex].NodeClass)
                    {
                        case (uint)Opc.Ua.NodeClass.Object:
                            // Resolve all objects under this object
                            Debug.Assert(!NodeId.IsNull(_expanded[_nodeIndex].NodeId));
                            if (!_request.ExcludeRootObject)
                            {
                                _expanded[_nodeIndex].Objects.Add(new ExpandedObject(
                                    new ReferenceDescription
                                    {
                                        NodeClass = Opc.Ua.NodeClass.Object,
                                        NodeId = _expanded[_nodeIndex].NodeId,
                                        BrowseName = QualifiedName.Null,
                                        DisplayName = LocalizedText.Null,
                                        TypeDefinition = NodeId.Null // TODO: Could retrieve type
                                    }));
                            }
                            Restart(_request.LevelsToExpand, _expanded[_nodeIndex].NodeId!);
                            return true;
                        case (uint)Opc.Ua.NodeClass.ObjectType:
                            // Resolve all objects of this type
                            Debug.Assert(!NodeId.IsNull(_expanded[_nodeIndex].NodeId));
                            Restart(_request.LevelsToExpand, ObjectIds.ObjectsFolder,
                                _expanded[_nodeIndex].NodeId); // Find all objects of the type
                            return true;
                        case (uint)Opc.Ua.NodeClass.Variable:
                            // Done - already a variable - stays in the original entry
                            break;
                        default:
                            _expanded[_nodeIndex].ErrorInfos.Add(new ServiceResultModel
                            {
                                ErrorMessage =
                                    $"Node class {_expanded[_nodeIndex].NodeClass} not supported.",
                                StatusCode = StatusCodes.BadNotSupported
                            });
                            break;
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
                if (_configuration == null)
                {
                    return;
                }
                Debug.Assert(entry.Result != null);
                Debug.Assert(entry.ErrorInfo == null);
                try
                {
                    await _configuration.CreateOrUpdateDataSetWriterEntryAsync(entry.Result,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    entry.ErrorInfo = ex.ToServiceResultModel();
                }
            }

            private record class ExpandedNode(OpcNodeModel NodeToExpand, NodeId? NodeId)
            {
                public uint NodeClass { get; internal set; }
                public List<ExpandedObject> Objects { get; } = new();
                public List<ServiceResultModel> ErrorInfos { get; } = new();
                public IEnumerable<OpcNodeModel> Remaining
                {
                    get
                    {
                        switch (NodeClass)
                        {
                            case (uint)Opc.Ua.NodeClass.Variable:
                                return new[] { NodeToExpand };
                            case (uint)Opc.Ua.NodeClass.Object:
                            case (uint)Opc.Ua.NodeClass.ObjectType:
                                return Objects
                                     .Where(o => !o.EntriesAlreadyReturned)
                                     .SelectMany(o => o.Nodes);
                            default:
                                return Enumerable.Empty<OpcNodeModel>();
                        }
                    }
                }
            }

            private record class ExpandedObject(ReferenceDescription ObjectToExpand)
            {
                public List<OpcNodeModel> Nodes { get; } = new();
                public bool EntriesAlreadyReturned { get; internal set; }
            }

            private int _nodeIndex = -1;
            private readonly List<ExpandedNode> _expanded = new();
            private readonly PublishedNodeExpansionRequestModel _request;
            private readonly IPublisherConfiguration? _configuration;
        }

        private readonly IPublisherConfiguration _configuration;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly ILogger<ConfigurationServices> _logger;
        private readonly TimeProvider? _timeProvider;
    }
}
