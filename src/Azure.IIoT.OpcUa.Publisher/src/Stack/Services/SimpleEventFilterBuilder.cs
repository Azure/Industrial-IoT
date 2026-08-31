// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Builds simple event filters from an event monitored-item template by
    /// reflecting over the event type definition in the server address space.
    /// </summary>
    internal static class SimpleEventFilterBuilder
    {
    /// <summary>
    /// Builds select clause and where clause by using OPC UA reflection
    /// </summary>
    /// <param name="session"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal static async ValueTask<EventFilter> CreateSimpleEventFilterAsync(
        EventMonitoredItemModel template, IOpcUaSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(template);
        var typeDefinitionId = template.EventFilter.TypeDefinitionId.ToNodeId(
            session.MessageContext);
        var nodes = new List<INode>();
        NodeId superType;
        var typeDefinitionNode = await session.LruNodeCache.GetNodeAsync(typeDefinitionId,
            ct).ConfigureAwait(false);
        nodes.Insert(0, typeDefinitionNode);
        while (true)
        {
            superType = await session.LruNodeCache.GetSuperTypeAsync(
                ExpandedNodeId.ToNodeId(nodes[0].NodeId, session.MessageContext.NamespaceUris),
                ct).ConfigureAwait(false);
            if (Opc.Ua.NodeIdCompat.IsNull(superType))
            {
                break;
            }
            typeDefinitionNode = await session.LruNodeCache.GetNodeAsync(superType,
                ct).ConfigureAwait(false);
            nodes.Insert(0, typeDefinitionNode);
        }
        var fieldNames = new List<QualifiedName>();

        foreach (var node in nodes)
        {
            await ParseFieldsAsync(session, fieldNames, node, string.Empty,
                ct).ConfigureAwait(false);
        }
        fieldNames = [.. fieldNames
            .Distinct()
            .OrderBy(x => x.Name)];

        var eventFilter = new EventFilter();
        // Let's add ConditionId manually first if event is derived from ConditionType
        if (nodes.Any(x => x.NodeId == ObjectTypeIds.ConditionType))
        {
            eventFilter.SelectClauses = eventFilter.SelectClauses.AddItem(new SimpleAttributeOperand()
            {
                BrowsePath = [],
                TypeDefinitionId = ObjectTypeIds.ConditionType,
                AttributeId = Attributes.NodeId
            });
        }

        foreach (var fieldName in fieldNames)
        {
            var selectClause = new SimpleAttributeOperand()
            {
                TypeDefinitionId = ObjectTypeIds.BaseEventType,
                AttributeId = Attributes.Value,
                BrowsePath = fieldName.Name
                    .Split('|')
                    .Select(x => new QualifiedName(x, fieldName.NamespaceIndex))
                    .ToArray()
            };
            eventFilter.SelectClauses = eventFilter.SelectClauses.AddItem(selectClause);
        }
        eventFilter.WhereClause = new ContentFilter();
        eventFilter.WhereClause.Push(FilterOperator.OfType, typeDefinitionId);

        return eventFilter;
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
        List<QualifiedName> browsePath, NodeId nodeId, CancellationToken ct)
    {
        INode? found = null;
        foreach (var browseName in browsePath)
        {
            found = null;
            while (found == null)
            {
                found = await session.LruNodeCache.GetNodeAsync(nodeId, ct).ConfigureAwait(false);

                //
                // Get all hierarchical references of the node and match browse name
                //
                var references = await session.LruNodeCache.GetReferencesAsync(nodeId,
                    ReferenceTypeIds.HierarchicalReferences, false, true, ct).ConfigureAwait(false);
                foreach (var reference in references)
                {
                    var target = await session.LruNodeCache.GetNodeAsync(
                        ExpandedNodeId.ToNodeId(reference.NodeId, session.MessageContext.NamespaceUris),
                        ct).ConfigureAwait(false);
                    if (target?.BrowseName == browseName)
                    {
                        found = target;
                        break;
                    }
                }

                if (found == null)
                {
                    // Try super type
                    nodeId = await session.LruNodeCache.GetSuperTypeAsync(nodeId, ct).ConfigureAwait(false);
                    if (Opc.Ua.NodeIdCompat.IsNull(nodeId))
                    {
                        // Nothing can be found since there is no more super type
                        return null;
                    }
                }
            }
            nodeId = ExpandedNodeId.ToNodeId(found.NodeId, session.MessageContext.NamespaceUris);
        }
        return found;
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
    internal static async ValueTask ParseFieldsAsync(IOpcUaSession session, List<QualifiedName> fieldNames,
        INode node, string browsePathPrefix, CancellationToken ct)
    {
        var references = await session.LruNodeCache.GetReferencesAsync(
            ExpandedNodeId.ToNodeId(node.NodeId, session.MessageContext.NamespaceUris),
            ReferenceTypeIds.HasComponent, false, true, ct).ConfigureAwait(false);
        foreach (var reference in references)
        {
            var componentNode = await session.LruNodeCache.GetNodeAsync(
                ExpandedNodeId.ToNodeId(reference.NodeId, session.MessageContext.NamespaceUris),
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
        references = await session.LruNodeCache.GetReferencesAsync(
            ExpandedNodeId.ToNodeId(node.NodeId, session.MessageContext.NamespaceUris),
            ReferenceTypeIds.HasProperty, false, false, ct).ConfigureAwait(false);
        foreach (var reference in references)
        {
            var propertyNode = await session.LruNodeCache.GetNodeAsync(
                ExpandedNodeId.ToNodeId(reference.NodeId, session.MessageContext.NamespaceUris),
                ct).ConfigureAwait(false);
            var fieldName = browsePathPrefix + propertyNode.BrowseName.Name;
            fieldNames.Add(new QualifiedName(
                fieldName, propertyNode.BrowseName.NamespaceIndex));
        }
    }
    }
}
