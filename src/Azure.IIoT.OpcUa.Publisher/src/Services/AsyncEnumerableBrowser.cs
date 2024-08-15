// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Async enumerable browsing operation base class. Used in configuration
    /// and file system browse operations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class AsyncEnumerableBrowser<T> : AsyncEnumerableEnumerableStack<T>
        where T : class
    {
        protected TimeProvider TimeProvider { get; }
        protected RequestHeaderModel? Header { get; }
        protected IOptions<PublisherOptions> Options { get; }

        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name="header"></param>
        /// <param name="options"></param>
        /// <param name="timeProvider"></param>
        /// <param name="root"></param>
        /// <param name="typeDefinitionId"></param>
        /// <param name="includeTypeDefinitionSubtypes"></param>
        /// <param name="stopWhenFound"></param>
        /// <param name="maxDepth"></param>
        /// <param name="nodeClass"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeReferenceTypeSubtypes"></param>
        /// <param name="matchClass"></param>
        protected AsyncEnumerableBrowser(RequestHeaderModel? header,
            IOptions<PublisherOptions> options, TimeProvider? timeProvider = null,
            NodeId? root = null, NodeId? typeDefinitionId = null,
            bool includeTypeDefinitionSubtypes = true, bool stopWhenFound = false,
            uint? maxDepth = null, Opc.Ua.NodeClass nodeClass = Opc.Ua.NodeClass.Object,
            NodeId? referenceTypeId = null, bool includeReferenceTypeSubtypes = true,
            Opc.Ua.NodeClass? matchClass = null)
        {
            Header = header;
            Options = options;
            TimeProvider = timeProvider ?? TimeProvider.System;

            _root = null!;
            _referenceTypeId = null!;

            Initialize(maxDepth, root, nodeClass, typeDefinitionId,
                includeTypeDefinitionSubtypes, referenceTypeId,
                includeReferenceTypeSubtypes, stopWhenFound, matchClass);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            OnReset();
        }

        /// <summary>
        /// Override to disable starting browsing
        /// </summary>
        protected virtual void OnReset()
        {
            Start();
        }

        /// <summary>
        /// Restart browsing with different configuration
        /// </summary>
        /// <param name="root"></param>
        /// <param name="maxDepth"></param>
        /// <param name="typeDefinitionId"></param>
        /// <param name="includeTypeDefinitionSubtypes"></param>
        /// <param name="stopWhenFound"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeReferenceTypeSubtypes"></param>
        /// <param name="nodeClass"></param>
        /// <param name="matchClass"></param>
        protected void Restart(NodeId? root = null, uint? maxDepth = null,
            NodeId? typeDefinitionId = null, bool includeTypeDefinitionSubtypes = true,
            bool stopWhenFound = false,
            NodeId? referenceTypeId = null, bool includeReferenceTypeSubtypes = true,
            Opc.Ua.NodeClass nodeClass = Opc.Ua.NodeClass.Object,
            Opc.Ua.NodeClass? matchClass = null)
        {
            Initialize(maxDepth, root, nodeClass, typeDefinitionId,
                includeTypeDefinitionSubtypes, referenceTypeId,
                includeReferenceTypeSubtypes, stopWhenFound, matchClass);
            Start();
        }

        /// <summary>
        /// Handle error
        /// </summary>
        /// <param name="context"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        protected abstract IEnumerable<T> HandleError(
            ServiceCallContext context, ServiceResultModel errorInfo);

        /// <summary>
        /// handle matches
        /// </summary>
        /// <param name="context"></param>
        /// <param name="matching"></param>
        /// <returns></returns>
        protected abstract IEnumerable<T> HandleMatching(
            ServiceCallContext context, IReadOnlyList<BrowseFrame> matching);

        /// <summary>
        /// Handle browse completion
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual IEnumerable<T> HandleCompletion(ServiceCallContext context)
        {
            return Enumerable.Empty<T>();
        }

        /// <summary>
        /// Browse references
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async ValueTask<IEnumerable<T>> BrowseAsync(ServiceCallContext context)
        {
            using var trace = _activitySource.StartActivity("Browse");

            var frame = Pop();
            if (frame == null)
            {
                return HandleCompletion(context);
            }

            var browseDescriptions = new BrowseDescriptionCollection
            {
                new BrowseDescription
                {
                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                    NodeClassMask = (uint)_nodeClass | (uint)_matchClass,
                    NodeId = frame.NodeId,
                    ReferenceTypeId = _referenceTypeId,
                    IncludeSubtypes = _includeReferenceTypeSubtypes,
                    ResultMask = (uint)BrowseResultMask.All
                }
            };

            // Browse and read children
            var response = await context.Session.Services.BrowseAsync(
                Header.ToRequestHeader(TimeProvider), null, 0,
                browseDescriptions, context.Ct).ConfigureAwait(false);

            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, browseDescriptions);
            if (results.ErrorInfo != null)
            {
                return HandleError(context, results.ErrorInfo);
            }
            var refs = MatchReferences(frame, context, results[0].Result.References,
                results[0].ErrorInfo);
            var continuation = results[0].Result.ContinuationPoint ?? Array.Empty<byte>();
            if (continuation.Length > 0)
            {
                Push(context => BrowseNextAsync(context, continuation, frame));
            }
            else
            {
                Push(context => BrowseAsync(context));
            }
            return refs;
        }

        /// <summary>
        /// Browse remainder of references
        /// </summary>
        /// <param name="context"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async ValueTask<IEnumerable<T>> BrowseNextAsync(ServiceCallContext context,
            byte[] continuationPoint, BrowseFrame frame)
        {
            using var trace = _activitySource.StartActivity("BrowseNext");

            var continuationPoints = new ByteStringCollection { continuationPoint };
            var response = await context.Session.Services.BrowseNextAsync(
                Header.ToRequestHeader(TimeProvider), false, continuationPoints,
                context.Ct).ConfigureAwait(false);

            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, continuationPoints);
            if (results.ErrorInfo != null)
            {
                return HandleError(context, results.ErrorInfo);
            }

            var refs = MatchReferences(frame, context, results[0].Result.References,
                results[0].ErrorInfo);

            var continuation = results[0].Result.ContinuationPoint ?? Array.Empty<byte>();
            if (continuation.Length > 0)
            {
                Push(session => BrowseNextAsync(session, continuation, frame));
            }
            else
            {
                Push(context => BrowseAsync(context));
            }
            return refs;
        }

        /// <summary>
        /// Collect references
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="context"></param>
        /// <param name="refs"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        private IEnumerable<T> MatchReferences(BrowseFrame frame, ServiceCallContext context,
            ReferenceDescriptionCollection refs, ServiceResultModel? errorInfo)
        {
            if (errorInfo != null)
            {
                return HandleError(context, errorInfo);
            }

            var matching = refs
                .Where(reference => reference.NodeClass == _matchClass
                    && (reference.NodeId?.ServerIndex ?? 1u) == 0)
                .Where(reference => _typeDefinitionId == null ||
                    reference.TypeDefinition == _typeDefinitionId || (_includeTypeDefinitionSubtypes
                        && context.Session.TypeTree.IsTypeOf(reference.TypeDefinition, _typeDefinitionId)))
                .Select(reference => new BrowseFrame((NodeId)reference.NodeId,
                    reference.BrowseName, reference.DisplayName?.Text, frame))
                .ToList();

            if (_stopWhenFound && matching.Count != 0)
            {
                // Only add what we did not match to browser deeper
                var stop = matching.Select(r => r.NodeId).ToHashSet();
                foreach (var reference in refs)
                {
                    if (!stop.Contains((NodeId)reference.NodeId))
                    {
                        Push(reference.NodeId, reference.BrowseName,
                            reference.DisplayName?.Text, frame);
                    }
                }
            }
            else
            {
                // Browse deeper in if possible
                foreach (var reference in refs)
                {
                    Push(reference.NodeId, reference.BrowseName,
                        reference.DisplayName?.Text, frame);
                }
            }

            if (matching.Count == 0)
            {
                return Enumerable.Empty<T>();
            }

            // Pass matching on
            return HandleMatching(context, matching);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        private void Start()
        {
            // Initialize
            _visited.Clear();
            _browseStack.Push(new BrowseFrame(_root, null, null));
            Push(context => BrowseAsync(context));
        }

        /// <summary>
        /// Helper to push nodes onto the browse stack
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="browseName"></param>
        /// <param name="displayName"></param>
        /// <param name="parent"></param>
        private void Push(ExpandedNodeId nodeId, QualifiedName? browseName,
            string? displayName, BrowseFrame? parent)
        {
            if ((nodeId?.ServerIndex ?? 1u) != 0)
            {
                return;
            }
            var local = (NodeId)nodeId;
            if (!NodeId.IsNull(local) && !_visited.Contains(local))
            {
                var frame = new BrowseFrame(local, browseName, displayName, parent);
                if (_maxDepth.HasValue && frame.Depth >= _maxDepth.Value)
                {
                    return;
                }
                _browseStack.Push(frame);
            }
        }

        /// <summary>
        /// Helper to pop nodes from the browse stack
        /// </summary>
        /// <returns></returns>
        private BrowseFrame? Pop()
        {
            while (_browseStack.TryPop(out var frame))
            {
                if (!NodeId.IsNull(frame.NodeId) && !_visited.Contains(frame.NodeId))
                {
                    return frame;
                }
            }
            return null;
        }

        /// <summary>
        /// Tracks a reference to a node
        /// </summary>
        /// <param name="NodeId"></param>
        /// <param name="BrowseName"></param>
        /// <param name="DisplayName"></param>
        /// <param name="Parent"></param>
        protected record class BrowseFrame(NodeId NodeId, QualifiedName? BrowseName,
            string? DisplayName, BrowseFrame? Parent = null)
        {
            /// <summary>
            /// Current depth of this frame
            /// </summary>
            public uint Depth
            {
                get
                {
                    var depth = 0u;
                    for (var parent = Parent; parent != null; parent = parent.Parent)
                    {
                        depth++;
                    }
                    return depth;
                }
            }

            /// <summary>
            /// Browse path to the node
            /// </summary>
            public string BrowsePath
            {
                get
                {
                    var path = BrowseName;
                    for (var parent = Parent; parent?.BrowseName != null; parent = parent.Parent)
                    {
                        path = $"{parent.BrowseName}/{path}";
                    }
                    return "/" + (path ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Restart browsing with different configuration
        /// </summary>
        /// <param name="maxDepth"></param>
        /// <param name="root"></param>
        /// <param name="nodeClass"></param>
        /// <param name="typeDefinitionId"></param>
        /// <param name="includeTypeDefinitionSubtypes"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeReferenceTypeSubtypes"></param>
        /// <param name="stopWhenFound"></param>
        /// <param name="matchClass"></param>
        private void Initialize(uint? maxDepth, NodeId? root, Opc.Ua.NodeClass nodeClass,
            NodeId? typeDefinitionId, bool includeTypeDefinitionSubtypes,
            NodeId? referenceTypeId, bool includeReferenceTypeSubtypes,
            bool stopWhenFound, Opc.Ua.NodeClass? matchClass)
        {
            _stopWhenFound = stopWhenFound;
            _nodeClass = nodeClass;
            _matchClass = matchClass ?? nodeClass;
            _maxDepth = maxDepth;
            _root = root ?? ObjectIds.ObjectsFolder;

            _typeDefinitionId =
                typeDefinitionId;
            _includeTypeDefinitionSubtypes =
                includeTypeDefinitionSubtypes;
            _referenceTypeId =
                referenceTypeId ?? ReferenceTypeIds.HierarchicalReferences;
            _includeReferenceTypeSubtypes =
                includeReferenceTypeSubtypes;
        }

        private bool _stopWhenFound;
        private Opc.Ua.NodeClass _nodeClass;
        private Opc.Ua.NodeClass _matchClass;
        private uint? _maxDepth;
        private NodeId _root;
        private NodeId _referenceTypeId;
        private bool _includeReferenceTypeSubtypes;
        private NodeId? _typeDefinitionId;
        private bool _includeTypeDefinitionSubtypes;
        private readonly Stack<BrowseFrame> _browseStack = new();
        private readonly HashSet<NodeId> _visited = new();
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
