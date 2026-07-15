// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Builds Publisher monitored-item metadata without depending on a monitored-item
    /// implementation.
    /// </summary>
    internal sealed class MonitoredItemMetaDataBuilder
    {
        /// <summary>
        /// Create a metadata builder.
        /// </summary>
        public MonitoredItemMetaDataBuilder(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Add metadata for a data monitored-item model.
        /// </summary>
        public ValueTask BuildDataChangeAsync(IOpcUaSession session,
            ComplexTypeSystem? typeSystem, DataMonitoredItemModel template,
            List<PublishedFieldMetaDataModel> fields,
            NodeIdDictionary<object> dataTypes, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(template);
            var dataSetClassFieldId = template.DataSetClassFieldId == Guid.Empty ?
                Guid.NewGuid() : template.DataSetClassFieldId;
            return BuildDataChangeAsync(session, typeSystem, template.StartNodeId,
                template.DisplayName, (Uuid)dataSetClassFieldId, fields, dataTypes,
                template, ct);
        }

        /// <summary>
        /// Add metadata for a resolved data monitored item.
        /// </summary>
        public async ValueTask BuildDataChangeAsync(IOpcUaSession session,
            ComplexTypeSystem? typeSystem, string nodeId, string fieldName,
            Uuid dataSetClassFieldId, List<PublishedFieldMetaDataModel> fields,
            NodeIdDictionary<object> dataTypes, object context, CancellationToken ct)
        {
            var parsedNodeId = nodeId.ToNodeId(session.MessageContext);
            if (Opc.Ua.NodeIdCompat.IsNull(parsedNodeId))
            {
                return;
            }
            try
            {
                var node = await session.LruNodeCache.GetNodeAsync(parsedNodeId, ct)
                    .ConfigureAwait(false);
                if (node is VariableNode variable)
                {
                    await AddVariableFieldAsync(fields, dataTypes, session, typeSystem,
                        variable, fieldName, dataSetClassFieldId, context, ct)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.GetMetadataFailed(context, fieldName, parsedNodeId, ex.Message);
            }
        }

        /// <summary>
        /// Add variable field metadata.
        /// </summary>
        public async ValueTask AddVariableFieldAsync(
            List<PublishedFieldMetaDataModel> fields,
            NodeIdDictionary<object> dataTypes, IOpcUaSession session,
            ComplexTypeSystem? typeSystem, VariableNode variable,
            string fieldName, Uuid dataSetClassFieldId, object context,
            CancellationToken ct)
        {
            byte builtInType = 0;
            try
            {
                builtInType = (byte)await session.LruNodeCache.GetBuiltInTypeAsync(
                    variable.DataType, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.BuiltInTypeFailed(context, variable.DataType.ToString(), ex.Message);
            }
            fields.Add(new PublishedFieldMetaDataModel
            {
                Flags = 0, // Set to 1 << 1 for PromotedField fields.
                Name = fieldName,
                Id = dataSetClassFieldId,
                DataType = variable.DataType.AsString(session.MessageContext,
                    NamespaceFormat.Expanded),
                ArrayDimensions = variable.ArrayDimensions.Count > 0
                    ? variable.ArrayDimensions.ToArray() : null,
                Description = variable.Description.AsString(),
                ValueRank = variable.ValueRank,
                MaxStringLength = 0,
                // If the Property is EngineeringUnits, the unit of the Field Value
                // shall match the unit of the FieldMetaData.
                Properties = null, // TODO: Add engineering units etc. to properties
                BuiltInType = builtInType
            });
            await AddDataTypesAsync(dataTypes, variable.DataType, session, typeSystem,
                context, ct).ConfigureAwait(false);
        }

        private async ValueTask AddDataTypesAsync(NodeIdDictionary<object> dataTypes,
            NodeId dataTypeId, IOpcUaSession session, ComplexTypeSystem? typeSystem,
            object context, CancellationToken ct)
        {
            if (IsBuiltInType(dataTypeId))
            {
                return;
            }

            var typesToResolve = new Queue<NodeId>();
            typesToResolve.Enqueue(dataTypeId);
            while (typesToResolve.Count > 0)
            {
                var baseType = typesToResolve.Dequeue();
                while (!Opc.Ua.NodeIdCompat.IsNull(baseType))
                {
                    try
                    {
                        var dataType = await session.LruNodeCache.GetNodeAsync(baseType,
                            ct).ConfigureAwait(false);
                        if (dataType == null)
                        {
                            _logger.DataTypeNodeNotFound(context, baseType.ToString());
                            break;
                        }

                        dataTypeId = ExpandedNodeId.ToNodeId(dataType.NodeId,
                            session.MessageContext.NamespaceUris);
                        Debug.Assert(!Opc.Ua.NodeIdCompat.IsNull(dataTypeId));
                        if (IsBuiltInType(dataTypeId))
                        {
                            // Do not add builtin types - we are done here now
                            break;
                        }

                        var builtInType = await session.LruNodeCache.GetBuiltInTypeAsync(
                            dataTypeId, ct).ConfigureAwait(false);
                        baseType = await session.LruNodeCache.GetSuperTypeAsync(dataTypeId,
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
                                    var dtNode = await session.LruNodeCache.GetNodeAsync(
                                        dataTypeId, ct).ConfigureAwait(false);
                                    if (dtNode is DataTypeNode v &&
                                        v.DataTypeDefinition.Body is DataTypeDefinition t)
                                    {
                                        types ??= [];
                                        types.Add(dataTypeId, t);
                                    }
                                    else
                                    {
                                        dataTypes.AddOrUpdate(
                                            ExpandedNodeId.ToNodeId(dataType.NodeId,
                                                session.MessageContext.NamespaceUris),
                                            GetDefault(dataType, builtInType,
                                                session.MessageContext));
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
                                                        session.MessageContext,
                                                        NamespaceFormat.Expanded),
                                                    DefaultEncodingId =
                                                        s.DefaultEncodingId.AsString(
                                                            session.MessageContext,
                                                            NamespaceFormat.Expanded),
                                                    StructureType =
                                                        s.StructureType.ToServiceType(),
                                                    Fields = GetFields(
                                                        new StructureFieldCollection(
                                                            s.Fields.ToArray() ?? []),
                                                        typesToResolve,
                                                        session.MessageContext,
                                                        NamespaceFormat.Expanded)
                                                        .ToList()
                                                },
                                            EnumDefinition e =>
                                                new EnumDescriptionModel
                                                {
                                                    DataTypeId = typeName,
                                                    Name = browseName,
                                                    BuiltInType = null,
                                                    IsOptionSet = e.IsOptionSet,
                                                    Fields = (e.Fields.ToArray() ?? [])
                                                        .Select(f =>
                                                            new EnumFieldDescriptionModel
                                                            {
                                                                Value = f.Value,
                                                                DisplayName =
                                                                    f.DisplayName.AsString(),
                                                                Name = f.Name,
                                                                Description =
                                                                    f.Description.AsString()
                                                            })
                                                        .ToList()
                                                },
                                            _ => GetDefault(dataType, builtInType,
                                                session.MessageContext),
                                        };
                                        dataTypes.AddOrUpdate(type.Key, description);
                                    }
                                }
                                break;
                            default:
                                var baseName = baseType
                                    .AsString(session.MessageContext,
                                        NamespaceFormat.Expanded);
                                dataTypes.AddOrUpdate(dataTypeId,
                                    new SimpleTypeDescriptionModel
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
                        _logger.MetaDataFailed(context, dataTypeId, baseType, ex.Message);
                        break;
                    }
                }

                object GetDefault(INode dataType, BuiltInType builtInType,
                    IServiceMessageContext messageContext)
                {
                    _logger.TypeDefinitionNotFound(context, dataType.NodeId, builtInType);
                    var name = dataType.BrowseName.AsString(messageContext,
                        NamespaceFormat.Expanded);
                    var typeId = dataType.NodeId.AsString(messageContext,
                        NamespaceFormat.Expanded);
                    return typeId == null
                        ? throw new ServiceResultException(
                            StatusCodes.BadConfigurationError)
                        : builtInType == BuiltInType.Enumeration
                        ? new EnumDescriptionModel
                        {
                            Fields = new List<EnumFieldDescriptionModel>(),
                            DataTypeId = typeId,
                            Name = name
                        }
                        : new StructureDescriptionModel
                        {
                            Fields = new List<StructureFieldDescriptionModel>(),
                            DataTypeId = typeId,
                            Name = name
                        };
                }

                static IEnumerable<StructureFieldDescriptionModel> GetFields(
                    StructureFieldCollection? fields, Queue<NodeId> typesToResolve,
                    IServiceMessageContext messageContext,
                    NamespaceFormat namespaceFormat)
                {
                    if (fields == null)
                    {
                        yield break;
                    }
                    foreach (var field in fields)
                    {
                        if (!IsBuiltInType(field.DataType))
                        {
                            typesToResolve.Enqueue(field.DataType);
                        }
                        yield return new StructureFieldDescriptionModel
                        {
                            IsOptional = field.IsOptional,
                            MaxStringLength = field.MaxStringLength,
                            ValueRank = field.ValueRank,
                            ArrayDimensions = [.. field.ArrayDimensions],
                            DataType = field.DataType.AsString(messageContext,
                                namespaceFormat) ?? string.Empty,
                            Name = field.Name,
                            Description = field.Description.AsString()
                        };
                    }
                }
            }
        }

        private static bool IsBuiltInType(NodeId dataTypeId)
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

        private readonly ILogger _logger;
    }

    /// <summary>
    /// Source-generated logging definitions for monitored-item metadata.
    /// </summary>
    internal static partial class MonitoredItemMetaDataBuilderLogging
    {
        private const int EventClass = 1030;

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Information,
            Message = "{Item}: Failed to get built in type for type {DataType} with message: {Message}")]
        public static partial void BuiltInTypeFailed(this ILogger logger, object item,
            string dataType, string message);

        [LoggerMessage(EventId = EventClass + 16, Level = LogLevel.Error,
            Message = "{Item}: Failed to find node for data type {BaseType}!")]
        public static partial void DataTypeNodeNotFound(this ILogger logger, object item,
            string baseType);

        [LoggerMessage(EventId = EventClass + 17, Level = LogLevel.Information,
            Message = "{Item}: Failed to get meta data for type {DataType} (base: {BaseType}) with message: {Message}")]
        public static partial void MetaDataFailed(this ILogger logger, object item,
            NodeId dataType, NodeId? baseType, string message);

        [LoggerMessage(EventId = EventClass + 18, Level = LogLevel.Error,
            Message = "{Item}: Could not find a valid type definition for {Type} ({BuiltInType}). " +
            "Adding a default placeholder with no fields instead.")]
        public static partial void TypeDefinitionNotFound(this ILogger logger, object item,
            ExpandedNodeId type, BuiltInType builtInType);

        [LoggerMessage(EventId = 1061, Level = LogLevel.Debug,
            Message = "{Item}: Failed to get meta data for field {Field} with node {NodeId} with message {Message}.")]
        public static partial void GetMetadataFailed(this ILogger logger, object item,
            string field, ExpandedNodeId nodeId, string message);
    }
}
