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
    /// Object browsing operation - returns all objects of a certain object type
    /// Configure the browser with the object type to browse and whether to
    /// consider all types derived from the object type or just the object type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class ObjectBrowser<T> : AsyncEnumerableEnumerableStack<T>
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
        /// <param name="rootFolder"></param>
        /// <param name="objectType"></param>
        /// <param name="stopWhenFound"></param>
        /// <param name="noSubtypes"></param>
        /// <param name="timeProvider"></param>
        protected ObjectBrowser(RequestHeaderModel? header, IOptions<PublisherOptions> options,
            NodeId? rootFolder = null, NodeId? objectType = null, bool stopWhenFound = false,
            bool noSubtypes = false, TimeProvider? timeProvider = null)
        {
            Header = header;
            _rootFolder = rootFolder ?? ObjectIds.ObjectsFolder;
            _objectType = objectType;
            _stopWhenFound = stopWhenFound;
            _noSubtypes = noSubtypes;
            TimeProvider = timeProvider ?? TimeProvider.System;
            Options = options;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            Restart();
        }

        /// <summary>
        /// Override to disable starting browsing
        /// </summary>
        protected virtual void Restart()
        {
            // Initialize
            _browseStack.Push(_rootFolder);
            Push(context => BrowseAsync(context));
        }

        /// <summary>
        /// Set new root folder
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="objectType"></param>
        /// <param name="stopWhenFound"></param>
        protected void Restart(NodeId rootFolder, NodeId? objectType = null,
            bool stopWhenFound = false)
        {
            _rootFolder = rootFolder;
            _objectType = objectType;
            _stopWhenFound = stopWhenFound;

            _browseStack.Push(_rootFolder);
            Push(context => BrowseAsync(context));
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
            ServiceCallContext context, IReadOnlyList<ReferenceDescription> matching);

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

            var nodeId = PopNode();
            if (nodeId == null)
            {
                return HandleCompletion(context);
            }

            var browseDescriptions = new BrowseDescriptionCollection
            {
                new BrowseDescription
                {
                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)Opc.Ua.NodeClass.Object,
                    NodeId = nodeId,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
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
            var refs = MatchReferences(context, results[0].Result.References, results[0].ErrorInfo);
            var continuation = results[0].Result.ContinuationPoint ?? Array.Empty<byte>();
            if (continuation.Length > 0)
            {
                Push(context => BrowseNextAsync(context, continuation));
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
        /// <returns></returns>
        private async ValueTask<IEnumerable<T>> BrowseNextAsync(ServiceCallContext context,
            byte[] continuationPoint)
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

            var refs = MatchReferences(context, results[0].Result.References, results[0].ErrorInfo);

            var continuation = results[0].Result.ContinuationPoint ?? Array.Empty<byte>();
            if (continuation.Length > 0)
            {
                Push(session => BrowseNextAsync(session, continuation));
            }
            else
            {
                Push(context => BrowseAsync(context));
            }
            return refs;
        }

        /// <summary>
        /// Helper to push nodes onto the browse stack
        /// </summary>
        /// <param name="nodeId"></param>
        private void PushNode(ExpandedNodeId nodeId)
        {
            if ((nodeId?.ServerIndex ?? 1u) != 0)
            {
                return;
            }
            var local = (NodeId)nodeId;
            if (!NodeId.IsNull(local) && !_visited.Contains(local))
            {
                _browseStack.Push(local);
            }
        }

        /// <summary>
        /// Helper to pop nodes from the browse stack
        /// </summary>
        /// <returns></returns>
        private NodeId? PopNode()
        {
            while (_browseStack.TryPop(out var nodeId))
            {
                if (!NodeId.IsNull(nodeId) && !_visited.Contains(nodeId))
                {
                    return nodeId;
                }
            }
            return null;
        }

        /// <summary>
        /// Collect references
        /// </summary>
        /// <param name="context"></param>
        /// <param name="refs"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        private IEnumerable<T> MatchReferences(ServiceCallContext context,
            ReferenceDescriptionCollection refs, ServiceResultModel? errorInfo)
        {
            if (errorInfo != null)
            {
                return HandleError(context, errorInfo);
            }

            var matching = refs
                .Where(reference => reference.NodeClass == Opc.Ua.NodeClass.Object)
                .Where(reference => _objectType == null ||
                    reference.TypeDefinition == _objectType || (!_noSubtypes
                        && context.Session.TypeTree.IsTypeOf(reference.TypeDefinition, _objectType)))
                .ToList();

            if (_stopWhenFound && matching.Count != 0)
            {
                // Only add what we did not match to browser deeper
                var stop = matching.Select(r => r.NodeId).ToHashSet();
                foreach (var reference in refs)
                {
                    if (!stop.Contains(reference.NodeId))
                    {
                        PushNode(reference.NodeId);
                    }
                }
            }
            else
            {
                // Browse deeper in if possible
                foreach (var reference in refs)
                {
                    PushNode(reference.NodeId);
                }
            }

            if (matching.Count == 0)
            {
                return Enumerable.Empty<T>();
            }

            // Pass matching on
            return HandleMatching(context, matching);
        }

        private bool _stopWhenFound;
        private NodeId _rootFolder;
        private NodeId? _objectType;
        private readonly Stack<NodeId> _browseStack = new();
        private readonly HashSet<NodeId> _visited = new();
        private readonly bool _noSubtypes;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
