// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Linq;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Messaging;
    using System.Text;
    using Microsoft.Extensions.Options;
    using KeyValuePair = System.Collections.Generic.KeyValuePair;

    /// <summary>
    /// Manages data set writer inside writer group
    /// </summary>
    internal sealed class DataSetWriter
    {
        /// <summary>
        /// Name of the writer
        /// </summary>
        public string Name => _dataSetWriter.DataSetWriterName
            ?? Constants.DefaultDataSetWriterName;

        /// <summary>
        /// Identifier of the writer
        /// </summary>
        public string Id => _dataSetWriter.Id;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="options"></param>
        /// <param name="outer"></param>
        /// <param name="loggerFactory"></param>
        public DataSetWriter(DataSetWriterModel dataSetWriter,
            IOptions<PublisherOptions> options, WriterGroup outer, ILoggerFactory loggerFactory)
        {
            _dataSetWriter = dataSetWriter;
            _loggerFactory = loggerFactory;

            _logger = loggerFactory.CreateLogger<DataSetWriter>();
            _outer = outer;

            var dataSetClassId = dataSetWriter.DataSet?.DataSetMetaData?.DataSetClassId
                ?? Guid.Empty;
            var escWriterName = TopicFilter.Escape(
                _dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName);
            var escWriterGroup = TopicFilter.Escape(
                outer.Configuration.Name ?? Constants.DefaultWriterGroupName);

            _variables = new Dictionary<string, string>
            {
                [PublisherConfig.DataSetWriterIdVariableName] = _dataSetWriter.Id,
                [PublisherConfig.DataSetWriterVariableName] = escWriterName,
                [PublisherConfig.DataSetWriterNameVariableName] = escWriterName,
                [PublisherConfig.DataSetClassIdVariableName] = dataSetClassId.ToString(),
                [PublisherConfig.WriterGroupIdVariableName] = outer.Configuration.Id,
                [PublisherConfig.DataSetWriterGroupVariableName] = escWriterGroup,
                [PublisherConfig.WriterGroupVariableName] = escWriterGroup
                // ...
            };

            var builder = new TopicBuilder(options, _outer.Configuration.MessageType,
                new TopicTemplatesOptions
                {
                    Telemetry = _dataSetWriter.Publishing?.QueueName
                        ?? _outer.Configuration.Publishing?.QueueName,
                    DataSetMetaData = _dataSetWriter.MetaData?.QueueName
                }, _variables);

            _topic = builder.TelemetryTopic;
            _qos = _dataSetWriter.Publishing?.RequestedDeliveryGuarantee
                ?? _outer.Configuration.Publishing?.RequestedDeliveryGuarantee;
        }

        private readonly Dictionary<string, string> _variables;

        /// <summary>
        /// Abstract data set
        /// </summary>
        private abstract class DataSet
        {
            /// <summary>
            /// Data set
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="logger"></param>
            protected DataSet(DataSetWriter outer, ILogger logger)
            {
                _outer = outer;
                _logger = logger;
                _extensionFields =
                    outer._dataSetWriter.DataSet?.ExtensionFields?.ToDictionary()
                        ?? new Dictionary<string, VariantValue>();
            }

            /// <summary>
            /// Update data set with new dataset configuration
            /// </summary>
            /// <param name="dataSet"></param>
            /// <returns></returns>
            public virtual bool TryUpdate(PublishedDataSetModel dataSet)
            {
                if (!_extensionFields.DictionaryEqualsSafe(dataSet.ExtensionFields))
                {
                    _extensionFields = dataSet.ExtensionFields!.ToDictionary();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Resolve field names
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public abstract ValueTask ResolveFieldNamesAsync(IOpcUaSession session,
                CancellationToken ct);

            /// <summary>
            /// Resolve field names
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public abstract ValueTask ResolveQueueNamesAsync(IOpcUaSession session,
                CancellationToken ct);

            /// <summary>
            /// Get meta data
            /// </summary>
            /// <param name="session"></param>
            /// <param name="typeSystem"></param>
            /// <param name="fields"></param>
            /// <param name="dataTypes"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public virtual ValueTask GetMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, Opc.Ua.FieldMetaDataCollection fields,
                Opc.Ua.NodeIdDictionary<DataTypeDescription> dataTypes,
                CancellationToken ct)
            {
                return ValueTask.CompletedTask;
            }

            /// <summary>
            /// Add veriable field metadata
            /// </summary>
            /// <param name="fields"></param>
            /// <param name="dataTypes"></param>
            /// <param name="session"></param>
            /// <param name="typeSystem"></param>
            /// <param name="variable"></param>
            /// <param name="fieldName"></param>
            /// <param name="dataSetClassFieldId"></param>
            /// <param name="ct"></param>
            protected async ValueTask AddVariableFieldAsync(FieldMetaDataCollection fields,
                NodeIdDictionary<DataTypeDescription> dataTypes, IOpcUaSession session,
                ComplexTypeSystem? typeSystem, VariableNode variable,
                string fieldName, Uuid dataSetClassFieldId, CancellationToken ct)
            {
                byte builtInType = 0;
                try
                {
                    builtInType = (byte)await TypeInfo.GetBuiltInTypeAsync(variable.DataType,
                        session.TypeTree, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("{Item}: Failed to get built in type for type {DataType}" +
                        " with message: {Message}", this, variable.DataType, ex.Message);
                }
                fields.Add(new FieldMetaData
                {
                    Name = fieldName,
                    DataSetFieldId = dataSetClassFieldId,
                    FieldFlags = 0, // Set to 1 << 1 for PromotedField fields.
                    DataType = variable.DataType,
                    ArrayDimensions = variable.ArrayDimensions?.Count > 0
                        ? variable.ArrayDimensions : null,
                    Description = variable.Description,
                    ValueRank = variable.ValueRank,
                    MaxStringLength = 0,
                    // If the Property is EngineeringUnits, the unit of the Field Value
                    // shall match the unit of the FieldMetaData.
                    Properties = null, // TODO: Add engineering units etc. to properties
                    BuiltInType = builtInType
                });
                await AddDataTypesAsync(dataTypes, variable.DataType, session, typeSystem,
                    ct).ConfigureAwait(false);
            }

            /// <summary>
            /// Add data types to the metadata
            /// </summary>
            /// <param name="dataTypes"></param>
            /// <param name="dataTypeId"></param>
            /// <param name="session"></param>
            /// <param name="typeSystem"></param>
            /// <param name="ct"></param>
            protected async ValueTask AddDataTypesAsync(NodeIdDictionary<DataTypeDescription> dataTypes,
                NodeId dataTypeId, IOpcUaSession session, ComplexTypeSystem? typeSystem,
                CancellationToken ct)
            {
                if (IsBuiltInType(dataTypeId))
                {
                    return;
                }

                var baseType = dataTypeId;
                while (!NodeId.IsNull(baseType))
                {
                    try
                    {
                        var dataType = await session.NodeCache.FetchNodeAsync(baseType, ct).ConfigureAwait(false);
                        if (dataType == null)
                        {
                            _logger.LogWarning("{Item}: Failed to find node for data type {BaseType}!",
                                this, baseType);
                            break;
                        }

                        dataTypeId = dataType.NodeId;
                        Debug.Assert(!NodeId.IsNull(dataTypeId));
                        if (IsBuiltInType(dataTypeId))
                        {
                            // Do not add builtin types
                            break;
                        }

                        var builtInType = await TypeInfo.GetBuiltInTypeAsync(dataTypeId, session.TypeTree,
                            ct).ConfigureAwait(false);
                        baseType = await session.TypeTree.FindSuperTypeAsync(dataTypeId, ct).ConfigureAwait(false);

                        switch (builtInType)
                        {
                            case BuiltInType.Enumeration:
                            case BuiltInType.ExtensionObject:
                                var types = typeSystem?.GetDataTypeDefinitionsForDataType(
                                    dataType.NodeId);
                                if (types == null || types.Count == 0)
                                {
                                    dataTypes.AddOrUpdate(dataType.NodeId, GetDefault(dataType, builtInType));
                                    break;
                                }
                                foreach (var type in types)
                                {
                                    if (!dataTypes.ContainsKey(type.Key))
                                    {
                                        var description = type.Value switch
                                        {
                                            StructureDefinition s =>
                                                new StructureDescription
                                                {
                                                    DataTypeId = type.Key,
                                                    Name = dataType.BrowseName,
                                                    StructureDefinition = s
                                                },
                                            EnumDefinition e =>
                                                new EnumDescription
                                                {
                                                    DataTypeId = type.Key,
                                                    Name = dataType.BrowseName,
                                                    EnumDefinition = e
                                                },
                                            _ => GetDefault(dataType, builtInType),
                                        };
                                        dataTypes.AddOrUpdate(type.Key, description);
                                    }
                                }
                                break;
                            default:
                                dataTypes.AddOrUpdate(dataTypeId, new SimpleTypeDescription
                                {
                                    DataTypeId = dataTypeId,
                                    Name = dataType.BrowseName,
                                    BaseDataType = baseType,
                                    BuiltInType = (byte)builtInType
                                });
                                break;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogInformation("{Item}: Failed to get meta data for type {DataType}" +
                            " (base: {BaseType}) with message: {Message}", this, dataTypeId,
                            baseType, ex.Message);
                        break;
                    }
                }

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
                static DataTypeDescription GetDefault(Node dataType, BuiltInType builtInType)
                {
                    return builtInType == BuiltInType.Enumeration
                        ? new EnumDescription
                        {
                            DataTypeId = dataType.NodeId,
                            Name = dataType.BrowseName
                        } : new StructureDescription
                        {
                            DataTypeId = dataType.NodeId,
                            Name = dataType.BrowseName
                        };
                }
            }

            /// <summary>
            /// Create queue name
            /// </summary>
            /// <param name="subPath"></param>
            /// <param name="includeNamespaceIndex"></param>
            /// <returns></returns>
            protected string ToQueueName(RelativePath subPath, bool includeNamespaceIndex)
            {
                var sb = new StringBuilder().Append(_outer._topic);
                foreach (var path in subPath.Elements)
                {
                    sb.Append('/');
                    if (path.TargetName.NamespaceIndex != 0 && includeNamespaceIndex)
                    {
                        sb.Append(path.TargetName.NamespaceIndex).Append(':');
                    }
                    sb.Append(TopicFilter.Escape(path.TargetName.Name));
                }
                return sb.ToString();
            }

            /// <summary>
            /// Merge items
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="oldItems"></param>
            /// <param name="newItems"></param>
            /// <param name="updated"></param>
            /// <returns></returns>
            protected static List<KeyValuePair<string, T>> Merge<T>(List<KeyValuePair<string, T>> oldItems,
                IEnumerable<KeyValuePair<string, T>> newItems, out bool updated)
            {
                var newList = new List<KeyValuePair<string, T>>();
                updated = false;
                foreach (var item in newItems)
                {
                    if (oldItems.Count > 0)
                    {
                        var i = oldItems.FindIndex(i => i.Key == item.Key);
                        if (i != -1)
                        {
                            newList.Add(oldItems[i]);
                            oldItems.RemoveRange(0, i + 1);
                            continue;
                        }
                    }
                    newList.Add(item);
                    updated = true;
                }
                return newList;
            }

            protected Dictionary<string, VariantValue> _extensionFields;
            protected readonly ILogger _logger;
            protected readonly DataSetWriter _outer;
        }

        /// <summary>
        /// UA-Data data set items
        /// </summary>
        private sealed class PublishedDataItems : DataSet
        {
            /// <summary>
            /// Create event item
            /// </summary>
            /// <param name="items"></param>
            /// <param name="outer"></param>
            public PublishedDataItems(PublishedDataItemsModel items, DataSetWriter outer) :
                base(outer, outer._loggerFactory.CreateLogger<PublishedDataItems>())
            {
                _items = items.PublishedData?
                    .Select((item, index) => KeyValuePair.Create(item.GetUniqueId(index), item.Clone()))
                    .ToList() ?? new List<KeyValuePair<string, PublishedDataSetVariableModel>>();
            }

            /// <inheritdoc/>
            public override bool TryUpdate(PublishedDataSetModel dataSet)
            {
                var newItems = dataSet.DataSetSource?.PublishedVariables?.PublishedData?
                    .Select((item, index) => KeyValuePair.Create(item.GetUniqueId(index), item.Clone()))
                    .ToList();
                if (newItems == null)
                {
                    return _items.Count != 0;
                }

                _items = Merge(_items, newItems, out var updated);
                return base.TryUpdate(dataSet) || updated;
            }

            /// <inheritdoc/>
            public override async ValueTask GetMetaDataAsync(IOpcUaSession session, ComplexTypeSystem? typeSystem,
                FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct)
            {
                foreach (var item in _items.Select(i => i.Value))
                {
                    Debug.Assert(item.Id != null);
                    var nodeId = item.PublishedVariableNodeId.ToNodeId(session.MessageContext);
                    if (NodeId.IsNull(nodeId))
                    {
                        // Failed.
                        return;
                    }
                    try
                    {
                        var node = await session.NodeCache.FetchNodeAsync(nodeId, ct).ConfigureAwait(false);
                        if (node is VariableNode variable)
                        {
                            await AddVariableFieldAsync(fields, dataTypes, session, typeSystem, variable,
                                item.Id, (Uuid)item.DataSetClassFieldId, ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogDebug("{Item}: Failed to get meta data for field {Field} " +
                            "with node {NodeId} with message {Message}.", this, item.Id,
                            nodeId, ex.Message);
                    }
                }
                await base.GetMetaDataAsync(session, typeSystem, fields, dataTypes, ct).ConfigureAwait(false);
            }

            /// <inheritdoc/>
            public override async ValueTask ResolveFieldNamesAsync(IOpcUaSession session, CancellationToken ct)
            {
                // Get limits to batch requests during resolve
                var operationLimits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
                foreach (var item in _items.Select(i => i.Value))
                {
                    item.Id ??= item.PublishedVariableDisplayName;
                }
                foreach (var displayNameUpdates in _items
                    .Select(i => i.Value)
                    .Where(p => p.Id == null)
                    .Batch(operationLimits.GetMaxNodesPerRead()))
                {
                    var response = await session.Services.ReadAsync(new RequestHeader(),
                        0, Opc.Ua.TimestampsToReturn.Neither, new ReadValueIdCollection(
                        displayNameUpdates.Select(a => new ReadValueId
                        {
                            NodeId = a!.PublishedVariableNodeId.ToNodeId(session.MessageContext),
                            AttributeId = (uint)NodeAttribute.DisplayName
                        })), ct).ConfigureAwait(false);
                    var results = response.Validate(response.Results,
                        s => s.StatusCode, response.DiagnosticInfos, displayNameUpdates);

                    if (results.ErrorInfo == null)
                    {
                        foreach (var result in results)
                        {
                            if (result.Result.Value is not null)
                            {
                                result.Request!.Id =
                                    (result.Result.Value as LocalizedText)?.ToString();
                                // metadataChanged = true;
                            }
                            else
                            {
                                _logger.LogWarning("Failed to read display name for {NodeId} " +
                                    "due to '{ServiceResult}'",
                                    result.Request!.PublishedVariableNodeId, result.ErrorInfo);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to resolve display name in {Subscription} due to {ErrorInfo}...",
                            this, results.ErrorInfo);

                        // We will retry later.
                        // noErrorFound = false;
                    }
                }
            }

            /// <inheritdoc/>
            public override async ValueTask ResolveQueueNamesAsync(IOpcUaSession session, CancellationToken ct)
            {
                var operationLimits = await session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
                foreach (var getPathsBatch in _items
                    .Select(i => i.Value)
                    .Where(i => i.Publishing?.QueueName == null)
                    .Batch(100))
                {
                    var getPath = getPathsBatch.ToList();
                    var paths = await session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        getPath.Select(n => n!.PublishedVariableNodeId.ToNodeId(session.MessageContext)),
                        ct).ConfigureAwait(false);
                    for (var index = 0; index < paths.Count; index++)
                    {
                        var item = getPath[index].Publishing ??= new PublishingQueueSettingsModel();
                        item.QueueName = ToQueueName(paths[index].Path, true); // TODO
                        if (paths[index].ErrorInfo != null)
                        {
                            _logger.LogWarning("Failed to get root path for {NodeId} due to '{ServiceResult}'",
                                getPath[index]!.PublishedVariableNodeId, paths[index].ErrorInfo);
                        }
                    }
                }
            }

            private List<KeyValuePair<string, PublishedDataSetVariableModel>> _items;
        }

        /// <summary>
        /// UA-Event items
        /// </summary>
        private sealed class PublishedEventItems : DataSet
        {
            /// <summary>
            /// Create event item
            /// </summary>
            /// <param name="items"></param>
            /// <param name="outer"></param>
            public PublishedEventItems(PublishedEventItemsModel items, DataSetWriter outer) :
                base(outer, outer._loggerFactory.CreateLogger<PublishedEventItems>())
            {
                _items = items.PublishedData?
                    .Select((item, index) => KeyValuePair.Create(item.GetUniqueId(index), item.Clone()))
                    .ToList() ?? new List<KeyValuePair<string, PublishedDataSetEventModel>>();
            }

            /// <inheritdoc/>
            public override bool TryUpdate(PublishedDataSetModel dataSet)
            {
                var newItems = dataSet.DataSetSource?.PublishedEvents?.PublishedData?
                    .Select((item, index) => KeyValuePair.Create(item.GetUniqueId(index), item.Clone()))
                    .ToList();
                if (newItems == null)
                {
                    return _items.Count != 0;
                }

                _items = Merge(_items, newItems, out var updated);
                return base.TryUpdate(dataSet) ;
            }

            /// <inheritdoc/>
            public override ValueTask GetMetaDataAsync(IOpcUaSession session, ComplexTypeSystem? typeSystem,
                FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct)
            {
                return ValueTask.CompletedTask;

            //   if (Filter is not EventFilter eventFilter)
            //   {
            //       return;
            //   }
            //   try
            //   {
            //       Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
            //       for (var i = 0; i < eventFilter.SelectClauses.Count; i++)
            //       {
            //           var selectClause = eventFilter.SelectClauses[i];
            //           var fieldName = Fields[i].Name;
            //           if (fieldName == null)
            //           {
            //               continue;
            //           }
            //           var dataSetClassFieldId = (Uuid)Fields[i].DataSetFieldId;
            //           var targetNode = await FindNodeWithBrowsePathAsync(session, selectClause.BrowsePath,
            //               selectClause.TypeDefinitionId, ct).ConfigureAwait(false);
            //           if (targetNode is VariableNode variable)
            //           {
            //               await AddVariableFieldAsync(fields, dataTypes, session, typeSystem, variable,
            //                   fieldName, dataSetClassFieldId, ct).ConfigureAwait(false);
            //           }
            //           else
            //           {
            //               fields.Add(new FieldMetaData
            //               {
            //                   Name = fieldName,
            //                   DataSetFieldId = dataSetClassFieldId
            //               });
            //           }
            //       }
            //   }
            //   catch (Exception e)
            //   {
            //       _logger.LogDebug(e, "{Item}: Failed to get metadata for event.", this);
            //       throw;
            //   }
            }

            /// <inheritdoc/>
            public override ValueTask ResolveFieldNamesAsync(IOpcUaSession session, CancellationToken ct)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override ValueTask ResolveQueueNamesAsync(IOpcUaSession session, CancellationToken ct)
            {
                throw new NotImplementedException();
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
                QualifiedNameCollection browsePath, ExpandedNodeId nodeId, CancellationToken ct)
            {
                INode? found = null;
                foreach (var browseName in browsePath)
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

            private List<KeyValuePair<string, PublishedDataSetEventModel>> _items;
        }

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly DataSetWriterModel _dataSetWriter;
        private readonly WriterGroup _outer;
        private readonly string _topic;
        private readonly QoS? _qos;
    }
}
