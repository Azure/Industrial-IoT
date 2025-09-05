// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class GetSimpleEventFilterTests : OpcUaMonitoredItemTestsBase
    {
        [Fact]
        public async Task SetupSimpleFilterForBaseEventTypeAsync()
        {
            // Arrange
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    TypeDefinitionId = ObjectTypeIds.BaseEventType.ToString()
                }
            };

            // Act
            var monitoredItem = await GetMonitoredItemAsync(template);

            // Assert
            Assert.NotNull(monitoredItem.Filter);
            Assert.IsType<EventFilter>(monitoredItem.Filter);
            var eventFilter = (EventFilter)monitoredItem.Filter;

            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(2, eventFilter.SelectClauses.Count);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[0].TypeDefinitionId);
            Assert.Equal(BrowseNames.Message, eventFilter.SelectClauses[0].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[1].TypeDefinitionId);
            Assert.Equal(BrowseNames.EventType, eventFilter.SelectClauses[1].BrowsePath.ElementAtOrDefault(0));

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

        [Fact]
        public async Task SetupSimpleFilterForConditionTypeAsync()
        {
            // Arrange
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    TypeDefinitionId = ObjectTypeIds.ConditionType.ToString()
                }
            };

            // Act
            var monitoredItem = await GetMonitoredItemAsync(template);

            // Assert
            Assert.NotNull(monitoredItem.Filter);
            Assert.IsType<EventFilter>(monitoredItem.Filter);
            var eventFilter = (EventFilter)monitoredItem.Filter;

            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(7, eventFilter.SelectClauses.Count);
            Assert.Equal(Attributes.NodeId, eventFilter.SelectClauses[0].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[0].TypeDefinitionId);
            Assert.Empty(eventFilter.SelectClauses[0].BrowsePath);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[1].TypeDefinitionId);
            Assert.Equal(BrowseNames.Comment, eventFilter.SelectClauses[1].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[2].TypeDefinitionId);
            Assert.Equal(BrowseNames.ConditionName, eventFilter.SelectClauses[2].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[3].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectClauses[3].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[4].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectClauses[4].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(BrowseNames.Id, eventFilter.SelectClauses[4].BrowsePath.ElementAtOrDefault(1));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[5].TypeDefinitionId);
            Assert.Equal(BrowseNames.Message, eventFilter.SelectClauses[5].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[6].TypeDefinitionId);
            Assert.Equal(BrowseNames.EventType, eventFilter.SelectClauses[6].BrowsePath.ElementAtOrDefault(0));

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
            Assert.Equal(nodeId, ObjectTypeIds.ConditionType);
        }

        [Fact]
        public async Task SetupSimpleFilterForConditionTypeWithConditionHandlingEnabledAsync()
        {
            // Arrange
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    TypeDefinitionId = ObjectTypeIds.ConditionType.ToString()
                },
                ConditionHandling = new ConditionHandlingOptionsModel
                {
                    SnapshotInterval = 10
                }
            };

            // Act
            var monitoredItem = await GetMonitoredItemAsync(template);

            // Assert
            Assert.NotNull(monitoredItem.Filter);
            Assert.IsType<EventFilter>(monitoredItem.Filter);
            var eventFilter = (EventFilter)monitoredItem.Filter;

            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(8, eventFilter.SelectClauses.Count);
            Assert.Equal(Attributes.NodeId, eventFilter.SelectClauses[0].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[0].TypeDefinitionId);
            Assert.Empty(eventFilter.SelectClauses[0].BrowsePath);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[1].TypeDefinitionId);
            Assert.Equal(BrowseNames.Comment, eventFilter.SelectClauses[1].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[2].TypeDefinitionId);
            Assert.Equal(BrowseNames.ConditionName, eventFilter.SelectClauses[2].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[3].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectClauses[3].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[4].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectClauses[4].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(BrowseNames.Id, eventFilter.SelectClauses[4].BrowsePath.ElementAtOrDefault(1));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[5].TypeDefinitionId);
            Assert.Equal(BrowseNames.Message, eventFilter.SelectClauses[5].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[6].TypeDefinitionId);
            Assert.Equal(BrowseNames.EventType, eventFilter.SelectClauses[6].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[7].TypeDefinitionId);
            Assert.Equal(BrowseNames.Retain, eventFilter.SelectClauses[7].BrowsePath.ElementAtOrDefault(0));

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
            Assert.Equal(nodeId, ObjectTypeIds.ConditionType);
        }

        protected override MockCache SetupMockedNodeCache()
        {
            var mockCache = base.SetupMockedNodeCache();

            mockCache.Add(_baseObjectTypeNode);
            mockCache.Add(_baseEventTypeNode);
            mockCache.Add(_conditionTypeNode);
            mockCache.Add(_messageNode);
            mockCache.Add(_conditionNameNode);
            mockCache.Add(_commentNode);
            mockCache.Add(_enabledStateNode);
            mockCache.Add(_idNode);

            mockCache.AddReference(_baseObjectTypeNode.NodeId, ReferenceTypeIds.HasSubtype, ObjectTypeIds.BaseEventType);
            mockCache.AddReference(_baseEventTypeNode.NodeId, ReferenceTypeIds.HasProperty, _messageNode.NodeId);
            mockCache.AddReference(_baseEventTypeNode.NodeId, ReferenceTypeIds.HasSubtype, ObjectTypeIds.ConditionType);
            mockCache.AddReference(_conditionTypeNode.NodeId, ReferenceTypeIds.HasProperty, _conditionNameNode.NodeId);
            mockCache.AddReference(_conditionTypeNode.NodeId, ReferenceTypeIds.HasComponent, _enabledStateNode.NodeId);
            mockCache.AddReference(_enabledStateNode.NodeId, ReferenceTypeIds.HasComponent, ObjectTypeIds.ConditionType);
            mockCache.AddReference(_idNode.NodeId, ReferenceTypeIds.HasProperty, _enabledStateNode.NodeId);
            mockCache.AddReference(_commentNode.NodeId, ReferenceTypeIds.HasProperty, ObjectTypeIds.ConditionType);

            return mockCache;
        }

        private readonly Node _baseObjectTypeNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.BaseObjectType,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.ObjectType,
            NodeId = ObjectTypeIds.BaseObjectType
        };
        private readonly Node _baseEventTypeNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.BaseEventType,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.ObjectType,
            NodeId = ObjectTypeIds.BaseEventType
        };
        private readonly Node _messageNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.Message,
            BrowseName = BrowseNames.Message,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = VariableIds.BaseEventType_Message
        };
        private readonly Node _conditionTypeNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.ConditionType,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.ObjectType,
            NodeId = ObjectTypeIds.ConditionType
        };
        private readonly Node _conditionNameNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.ConditionName,
            BrowseName = BrowseNames.ConditionName,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = VariableIds.ConditionType_ConditionName
        };
        private readonly Node _commentNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.Comment,
            BrowseName = BrowseNames.Comment,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = VariableIds.ConditionType_Comment
        };
        private readonly Node _enabledStateNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.EnabledState,
            BrowseName = BrowseNames.EnabledState,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = VariableIds.ConditionType_EnabledState
        };
        private readonly Node _idNode = new()
        {
            AccessRestrictions = 0,
            Description = null,
            DisplayName = BrowseNames.Id,
            BrowseName = BrowseNames.Id,
            Handle = null,
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = VariableIds.ConditionType_EnabledState_Id
        };
    }
}
