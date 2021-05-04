// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Tests.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class GetSimpleEventFilterTests : EventTestsBase {
        [Fact]
        public void SetupSimpleFilterForBaseEventType() {
            // Arrange
            var template = new EventMonitoredItemModel {
                EventFilter = new EventFilterModel() {
                    TypeDefinitionId = ObjectTypeIds.BaseEventType.ToString()
                }
            };

            // Act
            var monitoredItemWrapper = GetMonitoredItemWrapper(template, null, null);

            // Assert
            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<EventFilter>(monitoredItemWrapper.Item.Filter);
            var eventFilter = (EventFilter)monitoredItemWrapper.Item.Filter;

            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(4, eventFilter.SelectClauses.Count);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[0].TypeDefinitionId);
            Assert.Equal(BrowseNames.EventId, eventFilter.SelectClauses[0].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[1].TypeDefinitionId);
            Assert.Equal(BrowseNames.Time, eventFilter.SelectClauses[1].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[2].TypeDefinitionId);
            Assert.Equal(BrowseNames.ReceiveTime, eventFilter.SelectClauses[2].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[3].TypeDefinitionId);
            Assert.Equal(BrowseNames.EventType, eventFilter.SelectClauses[3].BrowsePath.ElementAtOrDefault(0));

            Assert.NotNull(eventFilter.WhereClause);
            Assert.NotNull(eventFilter.WhereClause.Elements);
            Assert.Single(eventFilter.WhereClause.Elements);
            Assert.Equal(FilterOperator.OfType, eventFilter.WhereClause.Elements[0].FilterOperator);
            Assert.NotNull(eventFilter.WhereClause.Elements[0].FilterOperands);
            Assert.Single(eventFilter.WhereClause.Elements[0].FilterOperands);
            Assert.IsType<LiteralOperand>(eventFilter.WhereClause.Elements[0].FilterOperands[0].Body);
            var operand = (LiteralOperand)eventFilter.WhereClause.Elements[0].FilterOperands[0].Body;
            Assert.IsType<NodeId>(operand.Value.Value);
            var nodeId = (NodeId)operand.Value.Value;
            Assert.Equal(nodeId, ObjectTypeIds.BaseEventType);
        }

        protected override Mock<INodeCache> SetupMockedNodeCache() {
            var nodeCache = base.SetupMockedNodeCache();
            AddNode(_baseObjectTypeNode);
            AddNode(_baseEventTypeNode);
            AddNode(_eventIdNode);
            var typeTable = nodeCache.Object.TypeTree as TypeTable;
            typeTable.Add(_baseObjectTypeNode);
            typeTable.Add(_baseEventTypeNode);
            typeTable.AddSubtype(ObjectTypeIds.BaseEventType, ObjectTypeIds.BaseObjectType);
            _baseObjectTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasSubtype, false, ObjectTypeIds.BaseEventType);
            _baseEventTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasSubtype, true, ObjectTypeIds.BaseObjectType);
            _baseObjectTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, false, _eventIdNode.NodeId);
            _eventIdNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, true, _baseEventTypeNode.NodeId);
            nodeCache.Setup<Node>(x => x.FetchNode(It.IsAny<ExpandedNodeId>())).Returns((ExpandedNodeId x) => {
                if (x.IdType == IdType.Numeric && x.Identifier is uint id) {
                    return _nodes[id];
                }
                else {
                    return null;
                }
            });
            return nodeCache;
        }

        private void AddNode(Node node) {
            _nodes[(uint)node.NodeId.Identifier] = node;
        }

        private readonly Node _baseObjectTypeNode = new Node() {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.BaseObjectType,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.ObjectType,
            NodeId = ObjectTypeIds.BaseObjectType
        };
        private readonly Node _baseEventTypeNode = new Node() {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.BaseEventType,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.ObjectType,
            NodeId = ObjectTypeIds.BaseEventType
        };
        private readonly Node _eventIdNode = new Node() {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.EventId,
            BrowseName = BrowseNames.EventId,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = VariableIds.BaseEventType_EventId
        };

        private readonly Dictionary<uint, Node> _nodes = new Dictionary<uint, Node>();
    }
}
