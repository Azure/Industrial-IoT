// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal partial class DataSetResolver
    {
        /// <summary>
        /// Data set item adapts data set entries using a common interface
        /// </summary>
        internal abstract partial class Field
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
            public virtual void Expand(DataSetResolver resolver,
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
            public NamespaceFormat NamespaceFormat => _resolver._format;

            /// <summary>
            /// Create data set item
            /// </summary>
            /// <param name="resolver"></param>
            /// <param name="dataSetWriter"></param>
            protected Field(DataSetResolver resolver,
                DataSetWriterModel dataSetWriter)
            {
                _resolver = resolver;
                DataSetWriter = dataSetWriter;
            }

            /// <summary>
            /// Merge an existing item's state into this one
            /// </summary>
            /// <param name="existing"></param>
            public virtual bool Merge(Field existing)
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
            public static IEnumerable<Field> Merge(DataSetResolver resolver,
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
            public static IEnumerable<Field> Create(DataSetResolver resolver,
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
                            yield return new Telemetry(resolver, writer, item);
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
                            yield return new ObjectVariable(resolver, writer, item);
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
                            yield return new Event(resolver, writer, item);
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
                            yield return new Object(resolver, writer, item);
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
                            yield return new Extension(resolver, writer, item);
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
                        NamespaceFormat.Expanded),
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
                                _logger.LogError(
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
                                .AsString(session.MessageContext, NamespaceFormat.Expanded);
                            var typeName = dataType.NodeId
                                .AsString(session.MessageContext, NamespaceFormat.Expanded);
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
                                        var dtNode = await session.NodeCache.FetchNodeAsync(dataTypeId,
                                            ct).ConfigureAwait(false);
                                        if (dtNode is DataTypeNode v &&
                                            v.DataTypeDefinition.Body is DataTypeDefinition t)
                                        {
                                            types ??= new NodeIdDictionary<DataTypeDefinition>();
                                            types.Add(dataTypeId, t);
                                        }
                                        else
                                        {
                                            dataTypes.AddOrUpdate(dataType.NodeId, GetDefault(
                                                dataType, builtInType, session.MessageContext));
                                            break;
                                        }
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
                                                            session.MessageContext, NamespaceFormat.Expanded),
                                                        DefaultEncodingId = s.DefaultEncodingId.AsString(
                                                            session.MessageContext, NamespaceFormat.Expanded),
                                                        StructureType = (Models.StructureType)s.StructureType,
                                                        Fields = GetFields(s.Fields, typesToResolve,
                                                            session.MessageContext, NamespaceFormat.Expanded)
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
                                                _ => GetDefault(dataType, builtInType, session.MessageContext),
                                            };
                                            dataTypes.AddOrUpdate(type.Key, description);
                                        }
                                    }
                                    break;
                                default:
                                    var baseName = baseType
                                        .AsString(session.MessageContext, NamespaceFormat.Expanded);
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

                object GetDefault(Node dataType, BuiltInType builtInType, IServiceMessageContext context)
                {
                    _logger.LogError("{Item}: Could not find a valid type definition for {Type} " +
                        "({BuiltInType}). Adding a default placeholder with no fields instead.",
                        this, dataType, builtInType);
                    var name = dataType.BrowseName.AsString(context, NamespaceFormat.Expanded);
                    var dataTypeId = dataType.NodeId.AsString(context, NamespaceFormat.Expanded);
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
                            DataType = f.DataType.AsString(context, namespaceFormat)
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
            protected readonly DataSetResolver _resolver;
        }
    }
}
