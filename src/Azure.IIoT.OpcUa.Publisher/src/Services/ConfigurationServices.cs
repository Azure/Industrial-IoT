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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration services uses the address space and services of a
    /// connected server to configure the publisher. The configuration
    /// services allow interactive expansion of published nodes.
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
            var browser = new ConfigBrowser(request, _options,
                noUpdate ? null : _configuration, _timeProvider);
            return _client.ExecuteAsync(request.Entry.ToConnectionModel(),
                browser, request.Header, ct);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        internal sealed class ConfigBrowser : ObjectBrowser<ServiceResponse<PublishedNodesEntryModel>>
        {
            /// <inheritdoc/>
            public ConfigBrowser(PublishedNodeExpansionRequestModel request,
                IOptions<PublisherOptions> options, IPublisherConfiguration? configuration,
                TimeProvider? timeProvider = null)
                : base(request.Header, options, null, null, false,
                      request.LevelsToExpand ?? int.MaxValue, timeProvider)
            {
                _request = request;
                _configuration = configuration;
            }

            /// <inheritdoc/>
            public override void Reset()
            {
                Push(context => BeginAsync(context));
                // First resolve the root folder and then browse - the reset is called again after begin
                base.Reset();
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleError(
                ServiceCallContext context, ServiceResultModel errorInfo)
            {
                _resolved[_nodeIndex].ErrorInfo.Add(errorInfo);
                return Enumerable.Empty<ServiceResponse<PublishedNodesEntryModel>>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<PublishedNodesEntryModel>> HandleMatching(
                ServiceCallContext context, IReadOnlyList<ReferenceDescription> matching)
            {
                // Add items
                _resolved[_nodeIndex].Objects.AddRange(matching);
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
            private async Task<IEnumerable<ServiceResponse<PublishedNodesEntryModel>>> EndAsync(
                ServiceCallContext context)
            {
                var remaining = _resolved.Where(r => !r.EntryAlreadyReturned).ToList();
                var results = new List<ServiceResponse<PublishedNodesEntryModel>>();
                if (remaining.Count > 0)
                {
                    var goodEntry = _request.Entry with
                    {
                        OpcNodes = remaining
                            .Where(r => r.ErrorInfo.Count == 0)
                            .Where(r => r.NodeClass == (uint)Opc.Ua.NodeClass.Variable)
                            .Select(r => r.opcNode).ToList()
                    };
                    if (goodEntry.OpcNodes.Count > 1)
                    {
                        if (_configuration != null)
                        {
                            await _configuration.CreateOrUpdateDataSetWriterEntryAsync(goodEntry,
                                context.Ct).ConfigureAwait(false);
                        }

                        // Add good entry
                        results.Add(new ServiceResponse<PublishedNodesEntryModel>
                        {
                            Result = goodEntry
                        });
                    }
                    foreach (var entry in remaining.Where(r => r.ErrorInfo.Count > 0))
                    {
                        // Return bad entries
                        results.Add(new ServiceResponse<PublishedNodesEntryModel>
                        {
                            ErrorInfo = entry.ErrorInfo.FirstOrDefault(), // TODO
                            Result = _request.Entry with
                            {
                                OpcNodes = new List<OpcNodeModel> { entry.opcNode }
                            }
                        });
                    }
                }
                _resolved.Clear();
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
                if (_resolved[_nodeIndex].Objects.Count == 0)
                {
                    if (_resolved[_nodeIndex].ErrorInfo.Count == 0)
                    {
                        _resolved[_nodeIndex].ErrorInfo.Add(new ServiceResultModel
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
                // Now resolve all variables under the object
                var entries = new List<ServiceResponse<PublishedNodesEntryModel>>();
                foreach (var obj in _resolved[_nodeIndex].Objects)
                {
                    var results = await context.Session.FindAsync(
                        _request.Header.ToRequestHeader(TimeProvider),
                        obj.NodeId.ToNodeId(context.Session.MessageContext.NamespaceUris)
                            .YieldReturn(),
                        ReferenceTypeIds.HasComponent, ct: context.Ct).ConfigureAwait(false);

                    if (results.Item2 != null)
                    {
                        _resolved[_nodeIndex].ErrorInfo.Add(results.Item2);
                    }
                    else
                    {
                        var nodes = new List<OpcNodeModel>();
                        var index = 0;
                        foreach (var result in results.Item1)
                        {
                            if (result.ErrorInfo != null)
                            {
                                _resolved[_nodeIndex].ErrorInfo.Add(result.ErrorInfo);
                                continue; // Stop
                            }
                            if (NodeId.IsNull(result.Node) ||
                                result.NodeClass != Opc.Ua.NodeClass.Variable)
                            {
                                // Not a variable
                                continue;
                            }
                            var node = _resolved[_nodeIndex].opcNode;
                            nodes.Add(node with
                            {
                                Id = _request.Header.AsString(result.Node,
                                    context.Session.MessageContext, Options),
                                DataSetFieldId = node.DataSetFieldId + "/" + index++
                            });
                        }

                        if (_resolved[_nodeIndex].ErrorInfo.Count == 0)
                        {
                            var entry = new ServiceResponse<PublishedNodesEntryModel>
                            {
                                Result = _request.Entry with
                                {
                                    OpcNodes = nodes
                                }
                            };
                            entries.Add(entry);
                            if (_configuration != null)
                            {
                                // Update configuration
                                await _configuration.CreateOrUpdateDataSetWriterEntryAsync(entry.Result,
                                    context.Ct).ConfigureAwait(false);
                            }
                            _resolved[_nodeIndex].EntryAlreadyReturned = true;
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
                if (_request.Entry.OpcNodes != null)
                {
                    foreach (var node in _request.Entry.OpcNodes)
                    {
                        var nodeId = await context.Session.ResolveNodeIdAsync(_request.Header,
                            node.Id, node.BrowsePath, nameof(node.BrowsePath), TimeProvider,
                            context.Ct).ConfigureAwait(false);
                        _resolved.Add(new Node(node, nodeId));
                    }
                    // Resolve node classes
                    var results = await context.Session.ReadAttributeAsync<uint>(
                        _request.Header.ToRequestHeader(TimeProvider),
                        _resolved.Select(r => r.nodeId ?? NodeId.Null),
                        (uint)NodeAttribute.NodeClass, context.Ct).ConfigureAwait(false);
                    foreach (var result in results.Zip(_resolved))
                    {
                        if (result.First.Item2 != null)
                        {
                            result.Second.ErrorInfo.Add(result.First.Item2);
                        }
                        result.Second.NodeClass = result.First.Item1;
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
                // Now browse the objects
                for (; _nodeIndex < _resolved.Count; _nodeIndex++)
                {
                    if (_resolved[_nodeIndex].NodeClass == (uint)Opc.Ua.NodeClass.Object)
                    {
                        Restart(_request.LevelsToExpand, _resolved[_nodeIndex].nodeId!);
                        return true;
                    }
                    else if (_resolved[_nodeIndex].NodeClass == (uint)Opc.Ua.NodeClass.ObjectType)
                    {
                        Restart(_request.LevelsToExpand, ObjectIds.ObjectsFolder,
                            _resolved[_nodeIndex].nodeId); // Find all objects of the type
                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }
                return false;
            }

            private record class Node(OpcNodeModel opcNode, NodeId? nodeId)
            {
                public uint NodeClass { get; set; }
                public List<ReferenceDescription> Objects { get; } = new ();
                public List<ServiceResultModel> ErrorInfo { get; } = new ();
                public bool EntryAlreadyReturned { get; internal set; }
            }

            private int _nodeIndex;
            private readonly List<Node> _resolved = new();
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
