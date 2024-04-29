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
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
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
    internal partial class DataSetResolver
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
        /// <param name="format"></param>
        /// <param name="logger"></param>
        public DataSetResolver(IEnumerable<DataSetWriterModel> writers,
            NamespaceFormat format, ILogger<DataSetResolver> logger)
        {
            _items = writers
                .SelectMany(w => Field.Create(this, w))
                .ToList();
            _format = format;
            _logger = logger;
        }

        /// <summary>
        /// Create resolver
        /// </summary>
        /// <param name="writers"></param>
        /// <param name="oldWriters"></param>
        /// <param name="format"></param>
        /// <param name="logger"></param>
        public DataSetResolver(IEnumerable<DataSetWriterModel> writers,
            IDictionary<string, DataSetWriterModel> oldWriters,
            NamespaceFormat format, ILogger<DataSetResolver> logger)
        {
            _items = writers
                .SelectMany(w => Field.Merge(this, w, oldWriters))
                .ToList();
            _format = format;
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
            await ResolveMetaDataAsync(session, ct).ConfigureAwait(false);
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
                var writerId = -1;
                foreach (var writer in Field.Telemetry
                        .Split(writers.Key, writers, maxItemsPerDataSet)
                    .Concat(Field.Event
                        .Split(writers.Key, writers))
                    .Concat(Field.Object
                        .Split(writers.Key, writers, maxItemsPerDataSet)))
                {
                    writerId++;
                    yield return writer with
                    {
                        DataSetWriterId = (ushort)writerId,
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
                    var newItems = new Dictionary<Field,
                        SortedSet<PublishedDataSetVariableModel>>();
                    await foreach (var result in results.ConfigureAwait(false))
                    {
                        var item = result.Description?.Handle as Field;
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

        private readonly List<Field> _items;
        private readonly NamespaceFormat _format;
        private readonly ILogger _logger;
    }
}
