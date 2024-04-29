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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal partial class DataSetResolver
    {
        internal abstract partial class Field
        {
            /// <summary>
            /// Event item
            /// </summary>
            public sealed class Event : Field
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
                public Event(DataSetResolver resolver,
                    DataSetWriterModel writer, PublishedDataSetEventModel evt)
                    : base(resolver, writer)
                {
                    _event = evt;

                    if (_event.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(Event))
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
                public override bool Merge(Field existing)
                {
                    if (!base.Merge(existing) || existing is not Event other)
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
                    IEnumerable<Field> items)
                {
                    //
                    // Each event data set right now generates a writer.
                    // This is the model that is defined in Part 14
                    //
                    // TODO: This is not that efficient as every event
                    // gets a subscription, rather we should try and have
                    // all events inside a single subscription up to
                    // max events.
                    //
                    foreach (var item in items.OfType<Event>())
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
                    yield return Create(BrowseNames.EventType, builtInType: BuiltInType.ExpandedNodeId);
                    yield return Create(BrowseNames.SourceNode, builtInType: BuiltInType.NodeId);
                    yield return Create(BrowseNames.Time, builtInType: BuiltInType.DateTime);
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
                                DataType = dataType ?? "i=" + (int)builtInType,
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
                        selectedFields.Add(new SimpleAttributeOperandModel
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
                            TypeDefinitionId = ObjectTypeIds.BaseEventType.AsString( // TODO: IS this correct?
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
        }
    }
}
