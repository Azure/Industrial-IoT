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
        /// <param name="maxDepth"></param>
        /// <param name="nodeClass"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeReferenceTypeSubtypes"></param>
        /// <param name="matchClass"></param>
        protected AsyncEnumerableBrowser(RequestHeaderModel? header,
            IOptions<PublisherOptions> options, TimeProvider? timeProvider = null,
            BrowseFrame? root = null, NodeId? typeDefinitionId = null,
            bool includeTypeDefinitionSubtypes = true,
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
                includeReferenceTypeSubtypes, matchClass);
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
        /// <param name="referenceTypeId"></param>
        /// <param name="includeReferenceTypeSubtypes"></param>
        /// <param name="nodeClass"></param>
        /// <param name="matchClass"></param>
        protected void Restart(BrowseFrame? root = null, uint? maxDepth = null,
            NodeId? typeDefinitionId = null, bool includeTypeDefinitionSubtypes = true,
            NodeId? referenceTypeId = null, bool includeReferenceTypeSubtypes = true,
            Opc.Ua.NodeClass nodeClass = Opc.Ua.NodeClass.Object,
            Opc.Ua.NodeClass? matchClass = null)
        {
            Initialize(maxDepth, root, nodeClass, typeDefinitionId,
                includeTypeDefinitionSubtypes, referenceTypeId,
                includeReferenceTypeSubtypes, matchClass);
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
        /// <param name="references"></param>
        /// <returns></returns>
        protected abstract IEnumerable<T> HandleMatching(
            ServiceCallContext context, IReadOnlyList<BrowseFrame> matching,
            List<ReferenceDescription> references);

        /// <summary>
        /// Handle browse completion
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual IEnumerable<T> HandleCompletion(ServiceCallContext context)
        {
            return [];
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
            var continuation = results[0].Result.ContinuationPoint ?? [];
            if (continuation.Length > 0)
            {
                Push(context => BrowseNextAsync(context, continuation, frame));
            }
            else
            {
                Push(BrowseAsync);
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

            var continuation = results[0].Result.ContinuationPoint ?? [];
            if (continuation.Length > 0)
            {
                Push(session => BrowseNextAsync(session, continuation, frame));
            }
            else
            {
                Push(BrowseAsync);
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
                .Where(reference => ((int)reference.NodeClass & (int)_matchClass) != 0
                    && (reference.NodeId?.ServerIndex ?? 1u) == 0
                    && MatchTypeDefinitionId(context.Session, reference.TypeDefinition))
                .Select(reference => new BrowseFrame((NodeId)reference.NodeId,
                    reference.BrowseName, reference.DisplayName?.Text,
                    reference.TypeDefinition, reference.NodeClass, frame,
                    IsChildOf(context.Session, reference, frame)))
                .ToList();

            var results = matching.Count != 0 ? HandleMatching(context, matching, refs) : [];

            // Browse deeper
            foreach (var reference in refs)
            {
                Push(reference.NodeId, reference.BrowseName,
                    reference.DisplayName?.Text, reference.TypeDefinition,
                    reference.NodeClass, frame,
                    IsChildOf(context.Session, reference, frame));
            }
            return results;

            bool? IsChildOf(IOpcUaSession session, ReferenceDescription reference,
                BrowseFrame parent)
            {
                var referenceTypeId = reference.ReferenceTypeId;
                var parentTypeDefinitionId = parent.TypeDefinitionId;

                if (NodeId.IsNull(parentTypeDefinitionId))
                {
                    return null;
                }
                if (session.LruNodeCache.IsTypeOf(referenceTypeId, ReferenceTypeIds.HasComponent) ||
                    session.LruNodeCache.IsTypeOf(referenceTypeId, ReferenceTypeIds.HasProperty))
                {
                    var parentIsFolder = session.LruNodeCache.IsTypeOf(parentTypeDefinitionId,
                        ObjectTypeIds.FolderType);
#if DEBUG
                    Debug.WriteLine(parent.BrowseName
                        + "(" + (parentIsFolder ? "Folder" : "Component") + ")--"
                        + referenceTypeId + "-->" + reference.BrowseName);
#endif
                    return !parentIsFolder;
                }
                return false;
            }

            // Helper to match type definition to desired type definition id
            bool MatchTypeDefinitionId(IOpcUaSession session, ExpandedNodeId typeDefinition)
            {
                if (typeDefinition == _typeDefinitionId || _typeDefinitionId == null)
                {
                    return true;
                }
                if (_includeTypeDefinitionSubtypes && !NodeId.IsNull(typeDefinition))
                {
                    var typeDefinitionId = ExpandedNodeId.ToNodeId(typeDefinition,
                        session.MessageContext.NamespaceUris);
                    return session.LruNodeCache.IsTypeOf(typeDefinitionId, _typeDefinitionId);
                }
                return false;
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        private void Start()
        {
            // Initialize
            _visited.Clear();
            _browseStack.Push(_root);
            Push(BrowseAsync);
        }

        /// <summary>
        /// Helper to push nodes onto the browse stack
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="browseName"></param>
        /// <param name="displayName"></param>
        /// <param name="typeDefinition"></param>
        /// <param name="nodeClass"></param>
        /// <param name="parent"></param>
        /// <param name="isChildOfParent"></param>
        private void Push(ExpandedNodeId nodeId, QualifiedName? browseName, string? displayName,
            ExpandedNodeId typeDefinition, Opc.Ua.NodeClass nodeClass, BrowseFrame? parent,
            bool? isChildOfParent)
        {
            if ((nodeId?.ServerIndex ?? 1u) != 0)
            {
                return;
            }
            var local = (NodeId)nodeId;
            if (!NodeId.IsNull(local) && !_visited.Contains(local))
            {
                var frame = new BrowseFrame(local, browseName, displayName,
                    typeDefinition, nodeClass, parent, isChildOfParent);
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
        /// <param name="TypeDefinitionId"></param>
        /// <param name="NodeClass"></param>
        /// <param name="Parent"></param>
        /// <param name="IsChildOfParent"></param>
        protected internal record class BrowseFrame(NodeId NodeId, QualifiedName? BrowseName = null,
            string? DisplayName = null, ExpandedNodeId? TypeDefinitionId = null,
            Opc.Ua.NodeClass? NodeClass = null, BrowseFrame? Parent = null,
            bool? IsChildOfParent = null)
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
            /// Get the root frame that is not a child of anything
            /// </summary>
            public BrowseFrame? RootFrame
            {
                get
                {
                    //
                    // child, not child, child, child, not child (*), not child, not child.
                    // (*) <- this is what we want to return here
                    //
                    BrowseFrame? found = null;
                    for (var parent = this; parent != null; parent = parent.Parent)
                    {
                        if (parent.IsChildOfParent != true)
                        {
                            found ??= parent; // Set if not already set
                            continue;
                        }
                        found = null;
                    }
                    return found;
                }
            }

            /// <summary>
            /// Browse path to the node from root
            /// </summary>
            public string BrowsePath
            {
                get
                {
                    var path = BrowseName?.ToString() ?? string.Empty;
                    for (var parent = Parent; parent?.BrowseName != null; parent = parent.Parent)
                    {
                        path = $"{parent.BrowseName}/{path}";
                    }
                    return "/" + path;
                }
            }

            /// <summary>
            /// All browse frames to the root
            /// </summary>
            public IEnumerable<BrowseFrame> AllFramesToRoot
            {
                get
                {
                    for (var frame = this; frame != null; frame = frame.Parent)
                    {
                        yield return frame;
                    }
                }
            }

            /// <summary>
            /// Create a name for the object that is rooted in a root browse frame,
            /// e.g. the writer group, structurally. We use . seperator to create
            /// names that can be reused in topics and paths.
            /// </summary>
            /// <param name="root"></param>
            /// <returns></returns>
            public string BrowseNameFromRootFrame(BrowseFrame? root)
            {
                var cur = this;
                if (cur.BrowseName?.Name == null || cur == root)
                {
                    return "Default";
                }

                var result = cur.BrowseName.Name;
                cur = cur.Parent;
                while (cur != root)
                {
                    if (cur?.BrowseName?.Name == null)
                    {
                        // not rooted in root because we hit a null parent,
                        // return cur browsename
                        return BrowseName!.Name;
                    }
                    result = cur.BrowseName.Name + "." + result;
                    cur = cur.Parent;
                }
                return result;
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
        /// <param name="matchClass"></param>
        private void Initialize(uint? maxDepth, BrowseFrame? root, Opc.Ua.NodeClass nodeClass,
            NodeId? typeDefinitionId, bool includeTypeDefinitionSubtypes,
            NodeId? referenceTypeId, bool includeReferenceTypeSubtypes,
            Opc.Ua.NodeClass? matchClass)
        {
            _nodeClass = nodeClass;
            _matchClass = matchClass ?? nodeClass;
            _maxDepth = maxDepth;
            _root = root ?? new BrowseFrame(ObjectIds.ObjectsFolder);

            _typeDefinitionId =
                typeDefinitionId;
            _includeTypeDefinitionSubtypes =
                includeTypeDefinitionSubtypes;
            _referenceTypeId =
                referenceTypeId ?? ReferenceTypeIds.HierarchicalReferences;
            _includeReferenceTypeSubtypes =
                includeReferenceTypeSubtypes;
        }

        private Opc.Ua.NodeClass _nodeClass;
        private Opc.Ua.NodeClass _matchClass;
        private uint? _maxDepth;
        private BrowseFrame _root;
        private NodeId _referenceTypeId;
        private bool _includeReferenceTypeSubtypes;
        private NodeId? _typeDefinitionId;
        private bool _includeTypeDefinitionSubtypes;
        private readonly Stack<BrowseFrame> _browseStack = new();
        private readonly HashSet<NodeId> _visited = [];
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
