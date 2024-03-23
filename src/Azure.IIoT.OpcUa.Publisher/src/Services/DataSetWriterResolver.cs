// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Avro.Generic;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly.Extensions.Serializers;

    /// <summary>
    /// <para>
    /// Data set resolver resolves items in a data set against the
    /// server and updates the internal information.
    /// </para>
    /// <para>
    /// We move the lookup of relative paths and display name here
    /// to have it available for later matching of incoming
    /// messages.
    /// </para>
    /// <para>
    /// Good: We can also resolve nodes to subscribe to recursively
    /// here as well.
    /// Bad: if it fails?  In subscription (stack) we retry, I guess
    /// we have to retry at the writer group level as well then?
    /// We should not move this all to subscription or else we
    /// cannot handle
    /// writes until we have the subscription applied - that feels
    /// too late.
    /// </para>
    /// </summary>
    internal class DataSetWriterResolver
    {
        /// <summary>
        /// Needs update
        /// </summary>
        /// <returns></returns>
        public bool NeedsUpdate => _items.Any(v => v.NeedsUpdate);

        /// <summary>
        /// Get writers in the data set resolver
        /// </summary>
        public IEnumerable<DataSetWriterModel> DataSetWriters
            => _items.Select(i => i.DataSetWriter).Distinct();

        /// <summary>
        /// Create resolver
        /// </summary>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        public DataSetWriterResolver(IEnumerable<DataSetWriterModel> writers,
            ILogger<DataSetWriterResolver> logger)
        {
            _items = writers
                .SelectMany(w => PublishedDataSetItem.Create(this, w))
                .ToList();
            _logger = logger;
        }

        /// <summary>
        /// Create resolver
        /// </summary>
        /// <param name="writers"></param>
        /// <param name="oldWriters"></param>
        /// <param name="logger"></param>
        public DataSetWriterResolver(IEnumerable<DataSetWriterModel> writers,
            IDictionary<string, DataSetWriterModel> oldWriters,
            ILogger<DataSetWriterResolver> logger)
        {
            _items = writers
                .SelectMany(w => PublishedDataSetItem.Merge(this, w, oldWriters))
                .ToList();
            _logger = logger;
        }

        /// <summary>
        /// Update the configuration using session. This is only necessary if
        /// <see cref="NeedsUpdate()"/> returns
        /// true.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask ResolveAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            // 1. Resolve nodes
            await ResolveBrowsePathAsync(session, ct).ConfigureAwait(false);

            // 2. Resolve components
            await ResolveComponentsAsync(session, ct).ConfigureAwait(false);

            // 3. Resolve display names
            await ResolveFieldNamesAsync(session, ct).ConfigureAwait(false);

            // 4. Resolve queue names
            await ResolveQueueNamesAsync(session, ct).ConfigureAwait(false);

            // 5. Special resolve anything else
            await ResolveRemainingsAsync(session, ct).ConfigureAwait(false);

            // 6. Resolve metadata
            await ResolveMetaDataAsync(session,  ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Split into new set of writers
        /// </summary>
        /// <param name="maxItemsPerDataSet"></param>
        /// <returns></returns>
        public IEnumerable<DataSetWriterModel> Split(int maxItemsPerDataSet)
        {
            // We do not filter so we can report errors through stack subscription
            foreach (var writers in _items.GroupBy(w => w.DataSetWriter,
                Compare.Using<DataSetWriterModel>((x, y) => x?.Id == y?.Id)))
            {
                // TODO: slice events and generated data sets to fit as many items
                // into a split writer.

                var writerId = -1;
                foreach (var writer in PublishedDataSetItem.VariableItem
                        .Split(writers.Key, writers, maxItemsPerDataSet)
                    .Concat(PublishedDataSetItem.EventItem
                        .Split(writers.Key, writers))
                    .Concat(PublishedDataSetItem.ObjectItem
                        .Split(writers.Key, writers, maxItemsPerDataSet)))
                {
                    writerId++;
                    yield return writer with
                    {
                        Id = writerId == 0 ? writer.Id : writer.Id + "_" + writerId
                    };
                }
            }
        }

        /// <summary>
        /// Create topic builder
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="dataSetWriter"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TopicBuilder CreateTopicBuilder(WriterGroupModel writerGroup,
            DataSetWriterModel dataSetWriter, PublisherOptions options)
        {
            // TODO: Decide how we resolve topic templates. Should this happen from
            // top to bottom?  Which topic template is ultimately used?  Does it matter?

            var dataSetClassId = dataSetWriter.DataSet?.DataSetMetaData?.DataSetClassId
                ?? Guid.Empty;
            var escWriterName = TopicFilter.Escape(
                dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName);
            var escWriterGroup = TopicFilter.Escape(
                writerGroup.Name ?? Constants.DefaultWriterGroupName);

            var variables = new Dictionary<string, string>
            {
                [PublisherConfig.DataSetWriterIdVariableName] = dataSetWriter.Id,
                [PublisherConfig.DataSetWriterVariableName] = escWriterName,
                [PublisherConfig.DataSetWriterNameVariableName] = escWriterName,
                [PublisherConfig.DataSetClassIdVariableName] = dataSetClassId.ToString(),
                [PublisherConfig.WriterGroupIdVariableName] = writerGroup.Id,
                [PublisherConfig.DataSetWriterGroupVariableName] = escWriterGroup,
                [PublisherConfig.WriterGroupVariableName] = escWriterGroup
                // ...
            };

            return new TopicBuilder(options, writerGroup.MessageType,
                new TopicTemplatesOptions
                {
                    Telemetry = dataSetWriter.Publishing?.QueueName
                        ?? writerGroup.Publishing?.QueueName,
                    DataSetMetaData = dataSetWriter.MetaData?.QueueName
                }, variables);
        }

        /// <summary>
        /// Resolve browse names
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ResolveBrowsePathAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
            foreach (var resolvers in _items
                .Where(v => v.BrowsePath?.Count > 0)
                .Batch(limits.GetMaxNodesPerTranslatePathsToNodeIds()))
            {
                var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    new RequestHeader(), new BrowsePathCollection(resolvers
                        .Select(a => new BrowsePath
                        {
                            StartingNode = a!.NodeId.ToNodeId(
                                session.MessageContext),
                            RelativePath = a.BrowsePath.ToRelativePath(
                                session.MessageContext)
                        })), ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, s => s.StatusCode,
                    response.DiagnosticInfos, resolvers);
                if (results.ErrorInfo != null)
                {
                    // Could not do anything...
                    _logger.LogDebug(
                        "Failed to resolve browse path in due to {ErrorInfo}...",
                        results.ErrorInfo);

                    foreach (var item in resolvers)
                    {
                        item.ErrorInfo = results.ErrorInfo;
                    }
                    continue;
                }
                foreach (var result in results)
                {
                    if (result.ErrorInfo == null && result.Result.Targets.Count > 0)
                    {
                        if (result.Result.Targets.Count > 1)
                        {
                            _logger.LogInformation(
                                "Ambiguous browse path for {NodeId} - using first.",
                                result.Request!.NodeId);
                        }

                        result.Request.ErrorInfo = null;
                        result.Request.BrowsePath = null;
                        result.Request.NodeId = result.Result.Targets[0].TargetId.AsString(
                            session.MessageContext, result.Request.NamespaceFormat);
                    }
                    else
                    {
                        result.Request.ErrorInfo = result.ErrorInfo;

                        _logger.LogDebug(
                            "Failed resolve browse path of {NodeId} due to '{ServiceResult}'",
                            result.Request!.NodeId, result.ErrorInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Expand items
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ResolveComponentsAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
            foreach (var itembatches in _items
                .Where(v => v.Flags.HasFlag(PublishedNodeExpansion.Expand))
                .GroupBy(v => v.Flags.HasFlag(PublishedNodeExpansion.Recursive) ? 128 : 1)
                .Select(v => (v.Key, v.Select(v => v)))
                .Batch(limits.GetMaxNodesPerBrowse()))
            {
                foreach (var (depth, itemsToBrowse) in itembatches)
                {
                    // Different depths browsing of objects and variables
                    var results = session.BrowseAsync(new RequestHeader(), null,
                        itemsToBrowse
                            .Select(item => new BrowseDescription
                            {
                                Handle = item,
                                BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                                IncludeSubtypes = true,
                                NodeClassMask =
                                        (uint)Opc.Ua.NodeClass.Object
                                    | (uint)Opc.Ua.NodeClass.Variable,
                                NodeId = item.NodeId.ToNodeId(session.MessageContext),
                                ResultMask = (int)BrowseResultMask.All
                            })
                            .ToArray(), depth, ct);

                    // Add found items
                    var newItems = new Dictionary<PublishedDataSetItem,
                        SortedSet<PublishedDataSetVariableModel>>();
                    await foreach (var result in results.ConfigureAwait(false))
                    {
                        var item = result.Description?.Handle as PublishedDataSetItem;
                        if (result.ErrorInfo != null)
                        {
                            if (item == null)
                            {
                                foreach (var o in itemsToBrowse)
                                {
                                    o.ErrorInfo = result.ErrorInfo;
                                }
                                _logger.LogDebug(
                                     "Failed to resolve any components due to {ErrorInfo}...",
                                     result.ErrorInfo);
                                return;
                            }
                            item.ErrorInfo = result.ErrorInfo;
                            continue;
                        }

                        Debug.Assert(item != null);
                        Debug.Assert(result.References != null);
                        foreach (var reference in result.References)
                        {
                            var nodeId = reference.NodeId
                                .AsString(session.MessageContext, item.NamespaceFormat);
                            if (nodeId == null)
                            {
                                // Should not happen
                                continue;
                            }
                            // Do not add properties, those go into the metadata
                            if (reference.NodeClass == Opc.Ua.NodeClass.Variable &&
                                reference.ReferenceTypeId != ReferenceTypeIds.HasProperty)
                            {
                                if (!newItems.TryGetValue(item, out var variables))
                                {
                                    variables = new SortedSet<PublishedDataSetVariableModel>(
                                        Comparer<PublishedDataSetVariableModel>.Create((a, b)
                                        => StringComparer.Ordinal.Compare(
                                            a?.DataSetFieldName, b?.DataSetFieldName)));
                                    newItems.Add(item, variables);
                                }

                                var name = reference.DisplayName.Text
                                        ?? reference.BrowseName.AsString(
                                            session.MessageContext, item.NamespaceFormat);

                                // Add new variable
                                variables.Add(item.CreateVariable(nodeId, name));
                            }
                        }
                    }
                    foreach (var (item, variables) in newItems)
                    {
                        _logger.LogInformation("Expanded item {Id} to {Count} Variables",
                            item.Id, variables.Count);

                        item.Expand(this, new PublishedDataItemsModel
                        {
                            PublishedData = variables.ToList()
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Resolve field names
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ResolveFieldNamesAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            // Get limits to batch requests during resolve
            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);

            foreach (var displayNameUpdates in _items
                .Where(p => p.ResolvedName == null)
                .Batch(limits.GetMaxNodesPerRead()))
            {
                var response = await session.Services.ReadAsync(new RequestHeader(),
                    0, Opc.Ua.TimestampsToReturn.Neither, new ReadValueIdCollection(
                    displayNameUpdates.Select(a => new ReadValueId
                    {
                        NodeId = a!.NodeId.ToNodeId(session.MessageContext),
                        AttributeId = (uint)NodeAttribute.DisplayName
                    })), ct).ConfigureAwait(false);
                var results = response.Validate(response.Results,
                    s => s.StatusCode, response.DiagnosticInfos, displayNameUpdates);

                if (results.ErrorInfo != null)
                {
                    _logger.LogDebug(
                        "Failed to resolve display name due to {ErrorInfo}...",
                        results.ErrorInfo);

                    foreach (var item in displayNameUpdates)
                    {
                        item.ErrorInfo = results.ErrorInfo;
                    }
                    continue;
                }
                foreach (var result in results)
                {
                    if (result.Result.Value is not null)
                    {
                        result.Request!.ErrorInfo = null;
                        result.Request!.ResolvedName =
                            (result.Result.Value as LocalizedText)?.ToString();
                        // metadataChanged = true;
                    }
                    else
                    {
                        result.Request.ErrorInfo = result.ErrorInfo;

                        _logger.LogDebug("Failed to read display name for {NodeId} " +
                            "due to '{ServiceResult}'",
                            result.Request!.NodeId, result.ErrorInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Resolve queue names
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ResolveQueueNamesAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            var limits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);

            foreach (var getPathsBatch in _items
                .Where(i => i.NeedsQueueNameResolving)
                .Batch(1000))
            {
                var getPath = getPathsBatch.ToList();
                var paths = await session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                    getPath.Select(n => n.NodeId
                        .ToNodeId(session.MessageContext)),
                    ct).ConfigureAwait(false);

                for (var index = 0; index < paths.Count; index++)
                {
                    if (paths[index].ErrorInfo != null)
                    {
                        getPath[index].ErrorInfo = paths[index].ErrorInfo;

                        _logger.LogDebug(
                            "Failed to get root path for {NodeId} due to '{ServiceResult}'",
                            getPath[index]!.NodeId, paths[index].ErrorInfo);
                    }
                    else
                    {
                        getPath[index].ErrorInfo = null;
                        getPath[index].Publishing = (getPath[index].Publishing ??
                            new PublishingQueueSettingsModel()) with
                        {
                            QueueName = ToQueueName(getPath[index].DataSetWriter,
                                    paths[index].Path, getPath[index].Routing)
                        };
                    }
                }
            }

            static string ToQueueName(DataSetWriterModel dataSetWriter,
                RelativePath subPath, DataSetRoutingMode routingMode)
            {
                Debug.Assert(dataSetWriter.Publishing != null);
                var sb = new StringBuilder().Append(dataSetWriter.Publishing.QueueName);
                foreach (var path in subPath.Elements)
                {
                    sb.Append('/');
                    if (path.TargetName.NamespaceIndex != 0 &&
                        routingMode == DataSetRoutingMode.UseBrowseNamesWithNamespaceIndex)
                    {
                        sb.Append(path.TargetName.NamespaceIndex).Append(':');
                    }
                    sb.Append(TopicFilter.Escape(path.TargetName.Name));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Resolve metadata
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ResolveMetaDataAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            _logger.LogDebug("Loading Metadata ...");

            var sw = Stopwatch.StartNew();
            var typeSystem = await session.GetComplexTypeSystemAsync(
                ct).ConfigureAwait(false);

            foreach (var item in _items.Where(i => i.MetaDataNeedsRefresh))
            {
                await item.ResolveMetaDataAsync(session,
                    typeSystem, ct).ConfigureAwait(false);
            }
            _logger.LogInformation("Loaded Metadata in {Duration}.",
                sw.Elapsed);
        }

        /// <summary>
        /// Resolve all remmaining
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ResolveRemainingsAsync(IOpcUaSession session,
            CancellationToken ct)
        {
            foreach (var item in _items
                .Where(i => i.ErrorInfo == null))
            {
                await item.ResolveAsync(session, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Data set item adapts data set entries using a common interface
        /// </summary>
        internal abstract class PublishedDataSetItem
        {
            [Flags]
            public enum ItemTypes
            {
                Variables = 1,
                Events = 2,
                Objects = 4,
                ExtensionField = 8,
                All = Variables | Events | ExtensionField | Objects
            }

            /// <summary>
            /// Access the node id field
            /// </summary>
            public abstract string? NodeId { get; set; }

            /// <summary>
            /// Access the browse path
            /// </summary>
            public abstract IReadOnlyList<string>? BrowsePath { get; set; }

            /// <summary>
            /// Access the resolved field name
            /// </summary>
            public abstract string? ResolvedName { get; set; }

            /// <summary>
            /// Write resolver error
            /// </summary>
            public abstract ServiceResultModel? ErrorInfo { get; set; }

            /// <summary>
            /// Resolved queue name
            /// </summary>
            public abstract PublishingQueueSettingsModel? Publishing { get; set; }

            /// <summary>
            /// Flags
            /// </summary>
            public abstract PublishedNodeExpansion Flags { get; set; }

            /// <summary>
            /// Expand writer with new items
            /// </summary>
            /// <param name="resolver"></param>
            /// <param name="value"></param>
            /// <exception cref="NotSupportedException"></exception>
            public virtual void Expand(DataSetWriterResolver resolver,
                PublishedDataItemsModel value)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Writer
            /// </summary>
            public DataSetWriterModel DataSetWriter { get; }

            /// <summary>
            /// Reset meta data
            /// </summary>
            public virtual bool MetaDataNeedsRefresh
            {
                get => !MetaDataDisabled || NeedMetaDataRefresh;
                set => NeedMetaDataRefresh = value;
            }

            private bool NeedMetaDataRefresh { get; set; }

            /// <summary>
            /// Get unique identifier
            /// </summary>
            public abstract string? Id { get; }

            /// <summary>
            /// Whether the item needs updating
            /// </summary>
            public virtual bool NeedsUpdate =>
                ErrorInfo != null ||
                BrowsePath?.Count > 0 ||
                ResolvedName == null ||
                Flags.HasFlag(PublishedNodeExpansion.Expand) ||
                NeedsQueueNameResolving ||
                MetaDataNeedsRefresh
                ;

            protected bool MetaDataDisabled
                => DataSetWriter.DataSet?.DataSetMetaData == null;
            public virtual bool NeedsQueueNameResolving
                => Publishing?.QueueName == null
                && Routing != DataSetRoutingMode.None;
            public DataSetRoutingMode Routing
                => DataSetWriter.DataSet?.Routing
                ?? DataSetRoutingMode.None;
            /// <summary>
            /// Format to use to encode namespaces, index is not allowed
            /// </summary>
            public NamespaceFormat NamespaceFormat
                => NamespaceFormatInternal == NamespaceFormat.Index
                ? NamespaceFormat.Expanded
                : NamespaceFormatInternal;
            private NamespaceFormat NamespaceFormatInternal
                => DataSetWriter.MessageSettings?.NamespaceFormat
                ?? NamespaceFormat.Expanded;

            /// <summary>
            /// Create data set item
            /// </summary>
            /// <param name="resolver"></param>
            /// <param name="dataSetWriter"></param>
            protected PublishedDataSetItem(DataSetWriterResolver resolver,
                DataSetWriterModel dataSetWriter)
            {
                _resolver = resolver;
                DataSetWriter = dataSetWriter;
            }

            /// <summary>
            /// Merge an existing item's state into this one
            /// </summary>
            /// <param name="existing"></param>
            public virtual bool Merge(PublishedDataSetItem existing)
            {
                if (existing.Id != Id)
                {
                    // Cannot merge
                    return false;
                }

                ErrorInfo = existing.ErrorInfo;

                if (NodeId != existing.NodeId)
                {
                    NodeId = existing.NodeId;
                }

                if (BrowsePath != existing.BrowsePath)
                {
                    BrowsePath = existing.BrowsePath;
                }

                if (Publishing?.QueueName == null)
                {
                    Publishing = existing.Publishing;
                }

                if (string.IsNullOrEmpty(ResolvedName))
                {
                    ResolvedName ??= existing.ResolvedName;
                }
                return true;
            }

            /// <summary>
            /// Create variable
            /// </summary>
            /// <param name="nodeId"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            /// <exception cref="NotSupportedException"></exception>
            public virtual PublishedDataSetVariableModel CreateVariable(
                string nodeId, string name)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Merge items
            /// </summary>
            /// <param name="resolver"></param>
            /// <param name="writer"></param>
            /// <param name="oldWriters"></param>
            /// <returns></returns>
            public static IEnumerable<PublishedDataSetItem> Merge(DataSetWriterResolver resolver,
                DataSetWriterModel writer, IDictionary<string, DataSetWriterModel> oldWriters)
            {
                if (!oldWriters.TryGetValue(writer.Id, out var existing))
                {
                    foreach (var item in Create(resolver, writer))
                    {
                        yield return item;
                    }
                    yield break;
                }

                // Create lookup to reference the old state
                var lookup = Create(resolver, existing)
                    .Where(d => d.Id != null)
                    .DistinctBy(d => d.Id)
                    .ToDictionary(d => d.Id!, d => d);
                foreach (var item in Create(resolver, writer))
                {
                    if (item.Id != null && lookup.TryGetValue(item.Id, out var existingItem))
                    {
                        item.Merge(existingItem);
                    }
                    yield return item;
                }
            }

            /// <summary>
            /// Create items over a writer
            /// </summary>
            /// <param name="resolver"></param>
            /// <param name="writer"></param>
            /// <param name="items"></param>
            /// <returns></returns>
            public static IEnumerable<PublishedDataSetItem> Create(DataSetWriterResolver resolver,
                DataSetWriterModel writer, ItemTypes items = ItemTypes.All)
            {
                if (items.HasFlag(ItemTypes.Variables))
                {
                    var publishedData =
                        writer.DataSet?.DataSetSource?.PublishedVariables?.PublishedData;
                    if (publishedData != null)
                    {
                        foreach (var item in publishedData)
                        {
                            yield return new VariableItem(resolver, writer, item);
                        }
                    }
                    var publishedObjects =
                        writer.DataSet?.DataSetSource?.PublishedObjects?.PublishedData;
                    if (publishedObjects != null)
                    {
                        foreach (var item in publishedObjects
                            .Where(o => o.PublishedVariables != null)
                            .SelectMany(o => o.PublishedVariables!.PublishedData))
                        {
                            yield return new VariableItem(resolver, writer, item);
                        }
                    }

                    // Return triggered variables?
                }
                if (items.HasFlag(ItemTypes.Events))
                {
                    var publishedEvents =
                        writer.DataSet?.DataSetSource?.PublishedEvents?.PublishedData;
                    if (publishedEvents != null)
                    {
                        foreach (var item in publishedEvents)
                        {
                            yield return new EventItem(resolver, writer, item);
                        }
                    }
                }
                if (items.HasFlag(ItemTypes.Objects))
                {
                    var publishedObjects =
                        writer.DataSet?.DataSetSource?.PublishedObjects?.PublishedData;
                    if (publishedObjects != null)
                    {
                        foreach (var item in publishedObjects)
                        {
                            yield return new ObjectItem(resolver, writer, item);
                        }
                    }
                }
                if (items.HasFlag(ItemTypes.ExtensionField))
                {
                    var extensionFields =
                        writer.DataSet?.ExtensionFields;
                    if (extensionFields != null)
                    {
                        foreach (var item in extensionFields)
                        {
                            yield return new ExtensionField(resolver, writer, item);
                        }
                    }
                }
            }

            /// <summary>
            /// Resolve anything else
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public virtual ValueTask ResolveAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                return ValueTask.CompletedTask;
            }

            /// <summary>
            /// Get metadata
            /// </summary>
            /// <param name="session"></param>
            /// <param name="typeSystem"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public abstract ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, CancellationToken ct);

            /// <summary>
            /// Extension field
            /// </summary>
            private class ExtensionField : PublishedDataSetItem
            {
                /// <inheritdoc/>
                public override string? NodeId { get; set; }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath { get; set; }

                /// <inheritdoc/>
                public override string? ResolvedName { get; set; }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing { get; set; }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo { get; set; }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags { get; set; }

                /// <inheritdoc/>
                public override bool MetaDataNeedsRefresh
                {
                    get => base.MetaDataNeedsRefresh || _extension.MetaData == null;
                    set => base.MetaDataNeedsRefresh = value;
                }

                /// <inheritdoc/>
                public override string? Id
                    => _extension.Id;

                /// <inheritdoc/>
                public override bool NeedsUpdate => MetaDataNeedsRefresh;

                /// <inheritdoc/>
                public ExtensionField(DataSetWriterResolver resolver, DataSetWriterModel writer,
                    ExtensionFieldModel extension) : base(resolver, writer)
                {
                    _extension = extension;

                    if (_extension.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(ExtensionField))
                            .Append(_extension.DataSetFieldName)
                            .Append(_extension.DataSetClassFieldId)
                            // .Append(_extension.Triggering)
                            ;
                        _extension.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(PublishedDataSetItem existing)
                {
                    if (!base.Merge(existing) || existing is not ExtensionField other)
                    {
                        return false;
                    }

                    _extension.MetaData = other._extension.MetaData;
                    return true;
                }

                /// <inheritdoc/>
                public override async ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    if (_extension.MetaData != null)
                    {
                        return;
                    }
                    _extension.MetaData = await ResolveMetaDataAsync(session, typeSystem,
                        new VariableNode
                        {
                            DataType = GetBuiltInType(_extension.Value)
                        }, 0u, ct).ConfigureAwait(false);

                    static NodeId GetBuiltInType(VariantValue value)
                    {
                        return new NodeId((uint)(value.GetTypeCode() switch
                        {
                            TypeCode.Boolean => BuiltInType.Boolean,
                            TypeCode.SByte => BuiltInType.SByte,
                            TypeCode.Byte => BuiltInType.Byte,
                            TypeCode.Int16 => BuiltInType.Int16,
                            TypeCode.UInt16 => BuiltInType.UInt16,
                            TypeCode.Int32 => BuiltInType.Int32,
                            TypeCode.UInt32 => BuiltInType.UInt32,
                            TypeCode.Int64 => BuiltInType.Int64,
                            TypeCode.UInt64 => BuiltInType.UInt64,
                            TypeCode.Double => BuiltInType.Double,
                            TypeCode.String => BuiltInType.String,
                            TypeCode.DateTime => BuiltInType.DateTime,
                            TypeCode.Empty => BuiltInType.Null,
                            TypeCode.Object => BuiltInType.Variant,
                            TypeCode.Char => BuiltInType.String,
                            TypeCode.Single => BuiltInType.Float,
                            TypeCode.Decimal => BuiltInType.Variant,
                            _ => BuiltInType.Variant
                        }));
                    }
                }

                private readonly ExtensionFieldModel _extension;
            }

            /// <summary>
            /// Variable item
            /// </summary>
            public sealed class VariableItem : PublishedDataSetItem
            {
                /// <inheritdoc/>
                public override string? NodeId
                {
                    get => _variable.PublishedVariableNodeId;
                    set
                    {
                        if (_variable.PublishedVariableNodeId != value)
                        {
                            MetaDataNeedsRefresh = true;
                            _variable.PublishedVariableNodeId = value;
                            if (_variable.ReadDisplayNameFromNode != true &&
                                _variable.DataSetFieldName == null)
                            {
                                ResolvedName = value;
                            }
                        }
                    }
                }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath
                {
                    get => _variable.BrowsePath;
                    set => _variable.BrowsePath = value;
                }

                /// <inheritdoc/>
                public override string? ResolvedName
                {
                    get => _variable.DataSetFieldName;
                    set
                    {
                        if (_variable.DataSetFieldName != value)
                        {
                            MetaDataNeedsRefresh = true;
                            _variable.DataSetFieldName = value;
                        }
                    }
                }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags { get; set; }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing
                {
                    get => _variable.Publishing;
                    set => _variable.Publishing = value;
                }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo
                {
                    get => _variable.State;
                    set => _variable.State = ErrorInfo;
                }
                /// <inheritdoc/>
                public override string? Id => _variable.Id;

                /// <inheritdoc/>
                public override bool MetaDataNeedsRefresh
                {
                    get => base.MetaDataNeedsRefresh || _variable.MetaData == null ;
                    set => base.MetaDataNeedsRefresh = value;
                }

                /// <inheritdoc/>
                public VariableItem(DataSetWriterResolver resolver,
                    DataSetWriterModel writer, PublishedDataSetVariableModel variable)
                    : base(resolver, writer)
                {
                    _variable = variable;

                    if (_variable.ReadDisplayNameFromNode != true &&
                        _variable.DataSetFieldName == null)
                    {
                        ResolvedName = _variable.Id ?? NodeId;
                    }
                    if (_variable.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(VariableItem))
                            .Append(_variable.DataSetFieldName)
                            .Append(_variable.PublishedVariableNodeId)
                            .Append(_variable.ReadDisplayNameFromNode == true)
                            .Append(_variable.DataSetClassFieldId)
                            // .Append(_variable.Triggering)
                            .Append(_variable.Publishing?.QueueName ?? string.Empty)
                            ;
                        _variable.BrowsePath?.ForEach(b => sb.Append(b));
                        _variable.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(PublishedDataSetItem existing)
                {
                    if (!base.Merge(existing) || existing is not VariableItem other)
                    {
                        return false;
                    }

                    // Merge state
                    _variable.MetaData = other._variable.MetaData;
                    return true;
                }

                /// <inheritdoc/>
                public static IEnumerable<DataSetWriterModel> Split(DataSetWriterModel writer,
                    IEnumerable<PublishedDataSetItem> items, int maxItemsPerWriter)
                {
                    foreach (var variables in items
                        .OfType<VariableItem>()
                        .Batch(maxItemsPerWriter))
                    {
                        var copy = Copy(writer);
                        Debug.Assert(copy.DataSet?.DataSetSource != null);
                        copy.DataSet.DataSetSource.PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = variables
                                .Select((f, i) => f._variable with { FieldIndex = i })
                                .ToList()
                        };
                        var offset = copy.DataSet.DataSetSource.PublishedVariables.PublishedData.Count;
                        copy.DataSet.ExtensionFields = copy.DataSet.ExtensionFields?
                            // No need to clone more members of the field
                            .Select((f, i) => f with { FieldIndex = i + offset })
                            .ToList();
                        yield return copy;
                    }
                }

                /// <inheritdoc/>
                public override async ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    Debug.Assert(_variable.Id != null);
                    var nodeId = _variable.PublishedVariableNodeId.ToNodeId(session.MessageContext);
                    if (Opc.Ua.NodeId.IsNull(nodeId))
                    {
                        _variable.State = kItemInvalid;
                        return;
                    }
                    try
                    {
                        var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                        var fields = new FieldMetaDataCollection();
                        var node = await session.NodeCache.FetchNodeAsync(nodeId,
                            ct).ConfigureAwait(false);
                        if (node is VariableNode variable)
                        {
                            _variable.MetaData = await ResolveMetaDataAsync(session,
                                typeSystem, variable, (_variable.MetaData?.MinorVersion ?? 0) + 1,
                                ct).ConfigureAwait(false);
                            MetaDataNeedsRefresh = false;
                        }
                    }
                    catch (ServiceResultException sre)
                    {
                        _variable.State = sre.ToServiceResultModel();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogDebug("{Item}: Failed to get meta data for field {Field} " +
                            "with node {NodeId} with message {Message}.", this, _variable.Id,
                            nodeId, ex.Message);
                    }
                }
                private readonly PublishedDataSetVariableModel _variable;
            }

            /// <summary>
            /// Variable item
            /// </summary>
            public sealed class ObjectItem : PublishedDataSetItem
            {
                /// <inheritdoc/>
                public override string? NodeId
                {
                    get => _object.PublishedNodeId;
                    set
                    {
                        if (_object.PublishedNodeId != value)
                        {
                            _object.PublishedNodeId = value;
                        }
                    }
                }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath
                {
                    get => _object.BrowsePath;
                    set => _object.BrowsePath = value;
                }

                /// <inheritdoc/>
                public override string? ResolvedName
                {
                    get => _object.Name;
                    set
                    {
                        if (_object.Name != value)
                        {
                            _object.Name = value;
                        }
                    }
                }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags
                {
                    get => _object.Flags;
                    set => _object.Flags = value;
                }

                /// <inheritdoc/>
                public override void Expand(DataSetWriterResolver resolver,
                    PublishedDataItemsModel value)
                {
                    if (value != null)
                    {
                        _object.PublishedVariables = value;

                        foreach (var item in value.PublishedData)
                        {
                            resolver._items.Add(new VariableItem(resolver, DataSetWriter, item));
                        }
                        Flags &= ~PublishedNodeExpansion.Expand;
                    }
                }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing { get; set; }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo
                {
                    get => _object.State;
                    set => _object.State = ErrorInfo;
                }
                /// <inheritdoc/>
                public override string? Id => _object.Id;

                /// <inheritdoc/>
                public override bool NeedsQueueNameResolving => false;

                /// <inheritdoc/>
                public ObjectItem(DataSetWriterResolver resolver,
                    DataSetWriterModel writer, PublishedObjectModel obj)
                    : base(resolver, writer)
                {
                    _object = obj;

                    if ((_object.PublishedVariables?.PublishedData.Count ?? 0) > 0 &&
                        ErrorInfo == null)
                    {
                        _object.Flags &= ~PublishedNodeExpansion.Expand;
                    }
                    else
                    {
                        _object.Flags |= PublishedNodeExpansion.Expand;
                    }

                    if (_object.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(VariableItem))
                        //    .Append(_object.DisplayName)
                            .Append(_object.PublishedNodeId)
                            // .Append(_object.DataSetClassId)
                            // .Append(_variable.Triggering)
                            .Append(_object.Template.Publishing?.QueueName ?? string.Empty)
                            ;
                        _object.BrowsePath?.ForEach(b => sb.Append(b));

                        _object.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(PublishedDataSetItem existing)
                {
                    if (!base.Merge(existing) || existing is not ObjectItem other)
                    {
                        return false;
                    }

                    // Merge state
                    _object.PublishedVariables = other._object.PublishedVariables;
                    return true;
                }

                /// <inheritdoc/>
                public override PublishedDataSetVariableModel CreateVariable(
                    string nodeId, string name)
                {
                    return _object.Template with
                    {
                        Attribute = NodeAttribute.Value,
                        DataSetFieldName = name,
                        BrowsePath = null,
                        PublishedVariableNodeId = nodeId,
                        Publishing = Publishing
                    };
                }

                /// <inheritdoc/>
                public static IEnumerable<DataSetWriterModel> Split(DataSetWriterModel writer,
                    IEnumerable<PublishedDataSetItem> items, int maxItemsPerWriter)
                {
                    // Each object data set generates a writer
                    foreach (var item in items.OfType<ObjectItem>())
                    {
                        if (item._object.PublishedVariables == null)
                        {
                            continue;
                        }
                        foreach (var set in item._object.PublishedVariables.PublishedData
                            .Batch(maxItemsPerWriter))
                        {
                            var copy = Copy(writer);
                            Debug.Assert(copy.DataSet?.DataSetSource != null);

                            var oSet = item._object with
                            {
                                // No need to clone more members
                                PublishedVariables = new PublishedDataItemsModel
                                {
                                    PublishedData = set
                                        .Select((item, i) => item with { FieldIndex = i })
                                        .ToList()
                                }
                            };

                            copy.DataSet.DataSetSource.PublishedObjects =
                                new PublishedObjectItemsModel
                                {
                                    PublishedData = new[] { oSet }
                                };
                            copy.DataSet.Name = item._object.Name;
                            copy.DataSet.ExtensionFields = copy.DataSet.ExtensionFields?
                                // No need to clone more members of the field
                                .Select((f, i) => f with
                                {
                                    FieldIndex = i + oSet.PublishedVariables.PublishedData.Count
                                })
                                .ToList();
                            yield return copy;
                        }
                    }
                }

                /// <inheritdoc/>
                public override ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    return ValueTask.CompletedTask;
                }
                private readonly PublishedObjectModel _object;
            }

            /// <summary>
            /// Event item
            /// </summary>
            public sealed class EventItem : PublishedDataSetItem
            {
                /// <inheritdoc/>
                public override string? NodeId
                {
                    get => _event.EventNotifier;
                    set => _event.EventNotifier = value;
                }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath
                {
                    get => _event.BrowsePath;
                    set => _event.BrowsePath = value;
                }

                /// <inheritdoc/>
                public override string? ResolvedName
                {
                    get => _event.Name;
                    set => _event.Name = value;
                }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing
                {
                    get => _event.Publishing;
                    set => _event.Publishing = value;
                }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo
                {
                    get => _event.State;
                    set => _event.State = ErrorInfo;
                }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags { get; set; }

                /// <inheritdoc/>
                public override string? Id => _event.Id;

                /// <summary>
                /// Reset meta data
                /// </summary>
                public override bool MetaDataNeedsRefresh
                {
                    get => !MetaDataDisabled ||
                        (_event.SelectedFields?.Any(f => f.MetaData == null) ?? false);
                    set => NeedMetaDataRefresh = value;
                }

                /// <inheritdoc/>
                public override bool NeedsUpdate
                    => base.NeedsUpdate || NeedsFilterUpdate();

                /// <inheritdoc/>
                public EventItem(DataSetWriterResolver resolver,
                    DataSetWriterModel writer, PublishedDataSetEventModel evt)
                    : base(resolver, writer)
                {
                    _event = evt;

                    if (_event.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(EventItem))
                            .Append(_event.Name)
                            .Append(_event.EventNotifier)
                            .Append(_event.ReadEventNameFromNode == true)
                            .Append(_event.TypeDefinitionId)
                            .Append(_event.ModelChangeHandling != null)
                            .Append(_event.ConditionHandling != null)
                            // .Append(_variable.Triggering)
                            .Append(_event.Publishing?.QueueName ?? string.Empty)
                            ;
                        _event.BrowsePath?.ForEach(b => sb.Append(b));
                        _event.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(PublishedDataSetItem existing)
                {
                    if (!base.Merge(existing) || existing is not EventItem other)
                    {
                        return false;
                    }

                    // Merge state
                    _event.SelectedFields = other._event.SelectedFields;
                    _event.Filter = other._event.Filter;
                    return true;
                }

                /// <inheritdoc/>
                public static IEnumerable<DataSetWriterModel> Split(DataSetWriterModel writer,
                    IEnumerable<PublishedDataSetItem> items)
                {
                    //
                    // Each event data set right now generates a writer
                    //
                    // TODO: This is not that efficient as every event
                    // gets a subscription, rather we should try and have
                    // all events inside a single subscription up to
                    // max events.
                    //
                    foreach (var item in items.OfType<EventItem>())
                    {
                        var copy = Copy(writer);
                        Debug.Assert(copy.DataSet?.DataSetSource != null);

                        var evtSet = item._event with
                        {
                            // No need to clone more members
                            SelectedFields = item._event.SelectedFields?
                                .Select((item, i) => item with { FieldIndex = i })
                                .ToList()
                        };

                        copy.DataSet.DataSetSource.PublishedEvents = new PublishedEventItemsModel
                        {
                            PublishedData = new[] { evtSet }
                        };

                        copy.DataSet.Name = item._event.Name;
                        copy.DataSet.ExtensionFields = copy.DataSet.ExtensionFields?
                            // No need to clone more members of the field
                            .Select((f, i) => f with
                            {
                                FieldIndex = i + item._event.SelectedFields?.Count ?? 2
                            })
                            .ToList();
                        yield return copy;
                    }
                }

                /// <inheritdoc/>
                public override async ValueTask ResolveAsync(IOpcUaSession session,
                    CancellationToken ct)
                {
                    if (_event.ModelChangeHandling != null)
                    {
                        _event.SelectedFields = GetModelChangeEventFields().ToList();
                        _event.Filter = new ContentFilterModel
                        {
                            Elements = Array.Empty<ContentFilterElementModel>()
                        };
                    }
                    else if (_event.SelectedFields == null && _event.Filter == null)
                    {
                        if (string.IsNullOrEmpty(_event.TypeDefinitionId))
                        {
                            _event.TypeDefinitionId = ObjectTypeIds.BaseEventType
                                .AsString(session.MessageContext, NamespaceFormat);
                        }

                        // Resolve the simple event
                        await ResolveFilterForEventTypeDefinitionId(session,
                            _event, ct).ConfigureAwait(false);
                    }
                    else if (_event.SelectedFields != null)
                    {
                        await UpdateFieldNamesAsync(session,
                            _event.SelectedFields).ConfigureAwait(false);
                    }
                    else
                    {
                        // Set default fields
                    }

                    Debug.Assert(_event.SelectedFields != null);
                    Debug.Assert(_event.Filter != null);

                    _event.TypeDefinitionId = null;
                    foreach (var field in _event.SelectedFields)
                    {
                        if (field.DataSetClassFieldId == Guid.Empty)
                        {
                            field.DataSetClassFieldId = Guid.NewGuid();
                        }
                    }
                }

                /// <inheritdoc/>
                public override async ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    if (_event.ModelChangeHandling != null)
                    {
                        return;
                    }
                    try
                    {
                        Debug.Assert(_event.SelectedFields != null);

                        var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                        var fields = new FieldMetaDataCollection();
                        for (var i = 0; i < _event.SelectedFields.Count; i++)
                        {
                            var selectClause = _event.SelectedFields[i];
                            var fieldName = selectClause.DataSetFieldName;
                            if (fieldName == null)
                            {
                                continue;
                            }
                            var dataSetClassFieldId = (Uuid)selectClause.DataSetClassFieldId;
                            var targetNode = await FindNodeWithBrowsePathAsync(session,
                                selectClause.BrowsePath, selectClause.TypeDefinitionId,
                                ct).ConfigureAwait(false);

                            var version = (selectClause.MetaData?.MinorVersion ?? 0) + 1;
                            if (targetNode is VariableNode variable)
                            {
                                selectClause.MetaData = await ResolveMetaDataAsync(session,
                                    typeSystem, variable, version, ct).ConfigureAwait(false);
                            }
                            else
                            {
                                // Should this happen?
                                selectClause.MetaData = await ResolveMetaDataAsync(session,
                                    typeSystem, new VariableNode
                                    {
                                        DataType = (int)BuiltInType.Variant
                                    }, version, ct).ConfigureAwait(false);
                            }
                            MetaDataNeedsRefresh = false;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogDebug(e, "{Item}: Failed to get metadata for event.", this);
                        throw;
                    }
                }

                /// <summary>
                /// Get model change event fields
                /// </summary>
                /// <returns></returns>
                private static IEnumerable<SimpleAttributeOperandModel> GetModelChangeEventFields()
                {
                    yield return Create(BrowseNames.EventId, builtInType: BuiltInType.ByteString);
                    yield return Create(BrowseNames.EventType, builtInType: BuiltInType.NodeId);
                    yield return Create(BrowseNames.SourceNode, builtInType: BuiltInType.NodeId);
                    yield return Create(BrowseNames.Time, builtInType: BuiltInType.NodeId);
                    yield return Create("Change", builtInType: BuiltInType.ExtensionObject);

                    static SimpleAttributeOperandModel Create(string fieldName, string? dataType = null,
                        BuiltInType builtInType = BuiltInType.ExtensionObject)
                    {
                        return new SimpleAttributeOperandModel
                        {
                            DataSetClassFieldId = (Uuid)Guid.NewGuid(),
                            DataSetFieldName = fieldName,
                            MetaData = new PublishedMetaDataModel
                            {
                                DataType = dataType ?? "i=" + builtInType,
                                ValueRank = ValueRanks.Scalar,
                                // ArrayDimensions =
                                BuiltInType = (byte)builtInType
                            }
                        };
                    }
                }

                /// <summary>
                /// Checks whether the filter needs to be updated
                /// </summary>
                /// <returns></returns>
                private bool NeedsFilterUpdate()
                {
                    if (_event.SelectedFields == null || _event.Filter == null)
                    {
                        return true;
                    }
                    if (_event.SelectedFields
                        .Any(f => f.DataSetClassFieldId == Guid.Empty
                            || string.IsNullOrEmpty(f.DataSetFieldName)))
                    {
                        return true;
                    }
                    return false;
                }

                /// <summary>
                /// Builds select clause and where clause by using OPC UA reflection
                /// </summary>
                /// <param name="session"></param>
                /// <param name="dataSetEvent"></param>
                /// <param name="ct"></param>
                /// <returns></returns>
                private async ValueTask ResolveFilterForEventTypeDefinitionId(
                    IOpcUaSession session, PublishedDataSetEventModel dataSetEvent,
                    CancellationToken ct)
                {
                    Debug.Assert(dataSetEvent.TypeDefinitionId != null);
                    var selectedFields = new List<SimpleAttributeOperandModel>();

                    // Resolve select clauses
                    var typeDefinitionId = dataSetEvent.TypeDefinitionId.ToNodeId(
                        session.MessageContext);
                    var nodes = new List<Node>();
                    ExpandedNodeId? superType = null;
                    var typeDefinitionNode = await session.NodeCache.FetchNodeAsync(
                        typeDefinitionId, ct).ConfigureAwait(false);
                    nodes.Insert(0, typeDefinitionNode);
                    do
                    {
                        superType = nodes[0].GetSuperType(session.TypeTree);
                        if (superType != null)
                        {
                            typeDefinitionNode = await session.NodeCache.FetchNodeAsync(
                                superType, ct).ConfigureAwait(false);
                            nodes.Insert(0, typeDefinitionNode);
                        }
                    }
                    while (superType != null);

                    var fieldNames = new List<QualifiedName>();
                    foreach (var node in nodes)
                    {
                        await ParseFieldsAsync(session, fieldNames, node, string.Empty,
                            ct).ConfigureAwait(false);
                    }

                    fieldNames = fieldNames
                        .Distinct()
                        .OrderBy(x => x.Name).ToList();

                    // We add ConditionId first if event is derived from ConditionType
                    if (nodes.Any(x => x.NodeId == ObjectTypeIds.ConditionType))
                    {
                        selectedFields.Add(new SimpleAttributeOperandModel()
                        {
                            BrowsePath = Array.Empty<string>(),
                            TypeDefinitionId = ObjectTypeIds.ConditionType.AsString(
                                session.MessageContext, NamespaceFormat),
                            DataSetClassFieldId = Guid.NewGuid(), // Todo: Use constant here
                            IndexRange = null,
                            DataSetFieldName = "ConditionId",
                            AttributeId = NodeAttribute.NodeId
                        });
                    }

                    foreach (var fieldName in fieldNames)
                    {
                        var browsePath = fieldName.Name
                            .Split('|')
                            .Select(x => new QualifiedName(x, fieldName.NamespaceIndex))
                            .Select(q => q.AsString(session.MessageContext, NamespaceFormat))
                            .ToArray();
                        var selectClause = new SimpleAttributeOperandModel
                        {
                            TypeDefinitionId = ObjectTypeIds.BaseEventType.AsString( // IS this correct?
                                session.MessageContext, NamespaceFormat),
                            DataSetClassFieldId = Guid.NewGuid(),
                            DataSetFieldName = browsePath.LastOrDefault() ?? string.Empty,
                            IndexRange = null,
                            AttributeId = NodeAttribute.Value,
                            BrowsePath = browsePath
                        };
                        selectedFields.Add(selectClause);
                    }

                    // Simple filter of type type definition
                    dataSetEvent.Filter = new ContentFilterModel
                    {
                        Elements = new[]
                        {
                            new ContentFilterElementModel
                            {
                                FilterOperator = FilterOperatorType.OfType,
                                FilterOperands = new []
                                {
                                    new FilterOperandModel
                                    {
                                        Value = dataSetEvent.TypeDefinitionId
                                    }
                                }
                            }
                        }
                    };
                    dataSetEvent.SelectedFields = selectedFields;
                    dataSetEvent.TypeDefinitionId = null;
                    MetaDataNeedsRefresh = true;
                }

                /// <summary>
                /// Update field names
                /// </summary>
                /// <param name="session"></param>
                /// <param name="selectClauses"></param>
                private ValueTask UpdateFieldNamesAsync(IOpcUaSession session,
                    IEnumerable<SimpleAttributeOperandModel> selectClauses)
                {
                    Debug.Assert(session != null);
                    // let's loop thru the final set of select clauses and setup the field names used
                    foreach (var selectClause in selectClauses)
                    {
                        var fieldName = string.Empty;
                        if (string.IsNullOrEmpty(selectClause.DataSetFieldName))
                        {
                            // TODO: Resolve names - Use FindNodeWithBrowsePathAsync()

                            if (selectClause.BrowsePath != null && selectClause.BrowsePath.Count != 0)
                            {
                                // Format as relative path string
                                fieldName = selectClause.BrowsePath
                                    .Select(d => d.ToQualifiedName(session.MessageContext))
                                    .Select(q => q.AsString(session.MessageContext, NamespaceFormat))
                                    .Aggregate((a, b) => $"{a}/{b}");
                            }
                        }

                        if (selectClause.DataSetClassFieldId == Guid.Empty)
                        {
                            selectClause.DataSetClassFieldId = Guid.NewGuid();
                            MetaDataNeedsRefresh = true;
                        }

                        if (fieldName.Length == 0 &&
                            selectClause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            selectClause.AttributeId == NodeAttribute.NodeId)
                        {
                            fieldName = "ConditionId";
                        }
                        if (selectClause.DataSetFieldName != fieldName)
                        {
                            selectClause.DataSetFieldName = fieldName;
                            MetaDataNeedsRefresh = true;
                        }
                    }
                    return ValueTask.CompletedTask;
                }

                /// <summary>
                /// Get all the fields of a type definition node to build the
                /// select clause.
                /// </summary>
                /// <param name="session"></param>
                /// <param name="fieldNames"></param>
                /// <param name="node"></param>
                /// <param name="browsePathPrefix"></param>
                /// <param name="ct"></param>
                private static async ValueTask ParseFieldsAsync(IOpcUaSession session,
                    List<QualifiedName> fieldNames, Node node, string browsePathPrefix,
                    CancellationToken ct)
                {
                    foreach (var reference in node.ReferenceTable)
                    {
                        if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent &&
                            !reference.IsInverse)
                        {
                            var componentNode = await session.NodeCache.FetchNodeAsync(reference.TargetId,
                                ct).ConfigureAwait(false);
                            if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable)
                            {
                                var fieldName = browsePathPrefix + componentNode.BrowseName.Name;
                                fieldNames.Add(new QualifiedName(
                                    fieldName, componentNode.BrowseName.NamespaceIndex));
                                await ParseFieldsAsync(session, fieldNames, componentNode,
                                    $"{fieldName}|", ct).ConfigureAwait(false);
                            }
                        }
                        else if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty)
                        {
                            var propertyNode = await session.NodeCache.FetchNodeAsync(reference.TargetId,
                                ct).ConfigureAwait(false);
                            var fieldName = browsePathPrefix + propertyNode.BrowseName.Name;
                            fieldNames.Add(new QualifiedName(
                                fieldName, propertyNode.BrowseName.NamespaceIndex));
                        }
                    }
                }

                /// <summary>
                /// Find node by browse path
                /// </summary>
                /// <param name="session"></param>
                /// <param name="browsePath"></param>
                /// <param name="nodeId"></param>
                /// <param name="ct"></param>
                /// <returns></returns>
                private static async ValueTask<INode?> FindNodeWithBrowsePathAsync(IOpcUaSession session,
                    IReadOnlyList<string>? browsePath, ExpandedNodeId nodeId, CancellationToken ct)
                {
                    INode? found = null;
                    browsePath ??= Array.Empty<string>();
                    foreach (var browseName in browsePath
                        .Select(b => b.ToQualifiedName(session.MessageContext)))
                    {
                        found = null;
                        while (found == null)
                        {
                            found = await session.NodeCache.FindAsync(nodeId, ct).ConfigureAwait(false);
                            if (found is not Node node)
                            {
                                return null;
                            }

                            //
                            // Get all hierarchical references of the node and
                            // match browse name
                            //
                            foreach (var reference in node.ReferenceTable.Find(
                                ReferenceTypeIds.HierarchicalReferences, false,
                                    true, session.TypeTree))
                            {
                                var target = await session.NodeCache.FindAsync(reference.TargetId,
                                    ct).ConfigureAwait(false);
                                if (target?.BrowseName == browseName)
                                {
                                    nodeId = target.NodeId;
                                    found = target;
                                    break;
                                }
                            }

                            if (found == null)
                            {
                                // Try super type
                                nodeId = await session.TypeTree.FindSuperTypeAsync(nodeId,
                                    ct).ConfigureAwait(false);
                                if (Opc.Ua.NodeId.IsNull(nodeId))
                                {
                                    // Nothing can be found since there is no more super type
                                    return null;
                                }
                            }
                        }
                        nodeId = found.NodeId;
                    }
                    return found;
                }

                private readonly PublishedDataSetEventModel _event;
            }

            /// <summary>
            /// Copy writer
            /// </summary>
            /// <param name="dataSetWriter"></param>
            /// <returns></returns>
            protected static DataSetWriterModel Copy(DataSetWriterModel dataSetWriter)
            {
                return dataSetWriter with
                {
                    MessageSettings = dataSetWriter.MessageSettings == null
                        ? null : dataSetWriter.MessageSettings with { },
                    DataSet = (dataSetWriter.DataSet
                        ?? new PublishedDataSetModel()) with
                    {
                        DataSetMetaData = dataSetWriter.DataSet?.DataSetMetaData == null
                                ? null : dataSetWriter.DataSet.DataSetMetaData with { },
                        ExtensionFields = dataSetWriter.DataSet?.ExtensionFields?
                                .Select(e => e with { })
                                .ToList(),
                        DataSetSource = (dataSetWriter.DataSet?.DataSetSource
                                ?? new PublishedDataSetSourceModel()) with
                        {
                            PublishedEvents = null,
                            PublishedObjects = null,
                            PublishedVariables = null,
                        }
                    }
                };
            }

            /// <summary>
            /// Add veriable field metadata
            /// </summary>
            /// <param name="session"></param>
            /// <param name="typeSystem"></param>
            /// <param name="variable"></param>
            /// <param name="version"></param>
            /// <param name="ct"></param>
            protected async ValueTask<PublishedMetaDataModel?> ResolveMetaDataAsync(
                IOpcUaSession session, ComplexTypeSystem? typeSystem, VariableNode variable,
                uint version, CancellationToken ct)
            {
                var builtInType = (byte)await TypeInfo.GetBuiltInTypeAsync(variable.DataType,
                    session.TypeTree, ct).ConfigureAwait(false);
                var field = new PublishedMetaDataModel
                {
                    Flags = 0, // Set to 1 << 1 for PromotedField fields.
                    MinorVersion = version,
                    DataType = variable.DataType.AsString(session.MessageContext,
                        NamespaceFormat),
                    ArrayDimensions = variable.ArrayDimensions?.Count > 0
                        ? variable.ArrayDimensions : null,
                    Description = variable.Description?.Text,
                    ValueRank = variable.ValueRank,
                    MaxStringLength = 0,
                    // If the Property is EngineeringUnits, the unit of the Field Value
                    // shall match the unit of the FieldMetaData.
                    Properties = null, // TODO: Add engineering units etc. to properties
                    BuiltInType = builtInType
                };
                await AddTypeDefinitionsAsync(field, variable.DataType, session, typeSystem,
                    ct).ConfigureAwait(false);
                return field;
            }

            /// <summary>
            /// Add data types to the metadata
            /// </summary>
            /// <param name="field"></param>
            /// <param name="dataTypeId"></param>
            /// <param name="session"></param>
            /// <param name="typeSystem"></param>
            /// <param name="ct"></param>
            /// <exception cref="ServiceResultException"></exception>
            private async ValueTask AddTypeDefinitionsAsync(PublishedMetaDataModel field,
                NodeId dataTypeId, IOpcUaSession session, ComplexTypeSystem? typeSystem,
                CancellationToken ct)
            {
                if (IsBuiltInType(dataTypeId))
                {
                    return;
                }
                var dataTypes = new NodeIdDictionary<object>(); // Need to use object here
                                                                // we support 3 types
                var typesToResolve = new Queue<NodeId>();
                typesToResolve.Enqueue(dataTypeId);
                while (typesToResolve.Count > 0)
                {
                    var baseType = typesToResolve.Dequeue();
                    while (!Opc.Ua.NodeId.IsNull(baseType))
                    {
                        try
                        {
                            var dataType = await session.NodeCache.FetchNodeAsync(baseType,
                                ct).ConfigureAwait(false);
                            if (dataType == null)
                            {
                                _logger.LogWarning(
                                    "{Item}: Failed to find node for data type {BaseType}!",
                                    this, baseType);
                                break;
                            }

                            dataTypeId = dataType.NodeId;
                            Debug.Assert(!Opc.Ua.NodeId.IsNull(dataTypeId));
                            if (IsBuiltInType(dataTypeId))
                            {
                                // Do not add builtin types - we are done here now
                                break;
                            }

                            var builtInType = await TypeInfo.GetBuiltInTypeAsync(dataTypeId,
                                session.TypeTree, ct).ConfigureAwait(false);
                            baseType = await session.TypeTree.FindSuperTypeAsync(dataTypeId,
                                ct).ConfigureAwait(false);

                            var browseName = dataType.BrowseName
                                .AsString(session.MessageContext, NamespaceFormat);
                            var typeName = dataType.NodeId
                                .AsString(session.MessageContext, NamespaceFormat);
                            if (typeName == null)
                            {
                                // No type name - that should not happen
                                throw new ServiceResultException(StatusCodes.BadDataTypeIdUnknown,
                                    $"Failed to get metadata type name for {dataType.NodeId}.");
                            }

                            switch (builtInType)
                            {
                                case BuiltInType.Enumeration:
                                case BuiltInType.ExtensionObject:
                                    var types = typeSystem?.GetDataTypeDefinitionsForDataType(
                                        dataType.NodeId);
                                    if (types == null || types.Count == 0)
                                    {
                                        dataTypes.AddOrUpdate(dataType.NodeId, GetDefault(
                                            dataType, builtInType, session.MessageContext,
                                            NamespaceFormat));
                                        break;
                                    }
                                    foreach (var type in types)
                                    {
                                        if (!dataTypes.ContainsKey(type.Key))
                                        {
                                            var description = type.Value switch
                                            {
                                                StructureDefinition s =>
                                                    new StructureDescriptionModel
                                                    {
                                                        DataTypeId = typeName,
                                                        Name = browseName,
                                                        BaseDataType = s.BaseDataType.AsString(
                                                            session.MessageContext, NamespaceFormat),
                                                        DefaultEncodingId = s.DefaultEncodingId.AsString(
                                                            session.MessageContext, NamespaceFormat),
                                                        StructureType = (Models.StructureType)s.StructureType,
                                                        Fields = GetFields(s.Fields, typesToResolve,
                                                            session.MessageContext, NamespaceFormat)
                                                            .ToList()
                                                    },
                                                EnumDefinition e =>
                                                    new EnumDescriptionModel
                                                    {
                                                        DataTypeId = typeName,
                                                        Name = browseName,
                                                        IsOptionSet = e.IsOptionSet,
                                                        BuiltInType = null,
                                                        Fields = e.Fields
                                                            .Select(f => new EnumFieldDescriptionModel
                                                            {
                                                                Value = f.Value,
                                                                DisplayName = f.DisplayName?.Text,
                                                                Name = f.Name,
                                                                Description = f.Description?.Text
                                                            })
                                                            .ToList()
                                                    },
                                                _ => GetDefault(dataType, builtInType,
                                                    session.MessageContext, NamespaceFormat),
                                            };
                                            dataTypes.AddOrUpdate(type.Key, description);
                                        }
                                    }
                                    break;
                                default:
                                    var baseName = baseType
                                        .AsString(session.MessageContext, NamespaceFormat);
                                    dataTypes.AddOrUpdate(dataTypeId, new SimpleTypeDescriptionModel
                                    {
                                        DataTypeId = typeName,
                                        Name = browseName,
                                        BaseDataType = baseName,
                                        BuiltInType = (byte)builtInType
                                    });
                                    break;
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogInformation("{Item}: Failed to get meta data for type " +
                                "{DataType} (base: {BaseType}) with message: {Message}", this,
                                dataTypeId, baseType, ex.Message);
                            break;
                        }
                    }
                }

                field.EnumDataTypes = dataTypes.Values
                    .OfType<EnumDescriptionModel>()
                    .ToArray();
                field.StructureDataTypes = dataTypes.Values
                    .OfType<StructureDescriptionModel>()
                    .ToArray();
                field.SimpleDataTypes = dataTypes.Values
                    .OfType<SimpleTypeDescriptionModel>()
                    .ToArray();

                static bool IsBuiltInType(NodeId dataTypeId)
                {
                    if (dataTypeId.NamespaceIndex == 0 && dataTypeId.IdType == IdType.Numeric)
                    {
                        var id = (BuiltInType)(int)(uint)dataTypeId.Identifier;
                        if (id >= BuiltInType.Null && id <= BuiltInType.Enumeration)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                static object GetDefault(Node dataType, BuiltInType builtInType,
                    IServiceMessageContext context, NamespaceFormat namespaceFormat)
                {
                    var name = dataType.BrowseName.AsString(context, namespaceFormat);
                    var dataTypeId = dataType.NodeId.AsString(context, namespaceFormat);
                    return dataTypeId == null
                        ? throw new ServiceResultException(StatusCodes.BadConfigurationError)
                        : builtInType == BuiltInType.Enumeration
                        ? new EnumDescriptionModel
                        {
                            Fields = new List<EnumFieldDescriptionModel>(),
                            DataTypeId = dataTypeId,
                            Name = name
                        }
                        : new StructureDescriptionModel
                        {
                            Fields = new List<StructureFieldDescriptionModel>(),
                            DataTypeId = dataTypeId,
                            Name = name
                        };
                }

                static IEnumerable<StructureFieldDescriptionModel> GetFields(
                    StructureFieldCollection? fields, Queue<NodeId> typesToResolve,
                    IServiceMessageContext context, NamespaceFormat namespaceFormat)
                {
                    if (fields == null)
                    {
                        yield break;
                    }
                    foreach (var f in fields)
                    {
                        if (!IsBuiltInType(f.DataType))
                        {
                            typesToResolve.Enqueue(f.DataType);
                        }
                        yield return new StructureFieldDescriptionModel
                        {
                            IsOptional = f.IsOptional,
                            MaxStringLength = f.MaxStringLength,
                            ValueRank = f.ValueRank,
                            ArrayDimensions = f.ArrayDimensions,
                            DataType = f.DataType
                                .AsString(context, namespaceFormat)
                                ?? string.Empty,
                            Name = f.Name,
                            Description = f.Description?.Text
                        };
                    }
                }
            }

            protected readonly static ServiceResultModel kItemInvalid = new()
            {
                ErrorMessage = "Configuration Invalid",
                StatusCode = StatusCodes.BadConfigurationError
            };
            protected ILogger _logger => _resolver._logger;
            protected readonly DataSetWriterResolver _resolver;
        }

        private readonly List<PublishedDataSetItem> _items;
        private readonly ILogger _logger;
    }
}
