// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Furly.Extensions.Logging;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class DataSetWriterResolverEventTests : DataSetWriterResolverTestBase
    {
        [Fact]
        public async Task SetupSimpleFilterForBaseEventType()
        {
            // Arrange
            var eventFilter = new PublishedDataSetEventModel
            {
                Id = "1",
                EventNotifier = "i=2258",
                Name = "BaseEvent",
                TypeDefinitionId = ObjectTypeIds.BaseEventType.ToString()
            };

            // Act
            await ResolveAsync(eventFilter);

            // Assert
            Assert.NotNull(eventFilter.SelectedFields);
            var evt = Assert.Single(eventFilter.SelectedFields);
            Assert.Equal(ObjectTypeIds.BaseEventType, evt.TypeDefinitionId);
            Assert.Equal(BrowseNames.Message, evt.BrowsePath.ElementAtOrDefault(0));

            Assert.NotNull(eventFilter.Filter);
            Assert.NotNull(eventFilter.Filter.Elements);
            Assert.Single(eventFilter.Filter.Elements);
            Assert.Equal(FilterOperatorType.OfType, eventFilter.Filter.Elements[0].FilterOperator);
            Assert.NotNull(eventFilter.Filter.Elements[0].FilterOperands);
            var operand = Assert.Single(eventFilter.Filter.Elements[0].FilterOperands);
            Assert.IsType<string>(operand.Value.Value);
            var nodeId = (string)operand.Value.Value;
            Assert.Equal(nodeId, ObjectTypeIds.BaseEventType.ToString());
        }

        [Fact]
        public async Task SetupSimpleFilterForConditionType()
        {
            // Arrange
            var eventFilter = new PublishedDataSetEventModel
            {
                Id = "1",
                EventNotifier = "i=2258",
                Name = "Condition",
                TypeDefinitionId = ObjectTypeIds.ConditionType.ToString()
            };

            // Act
            await ResolveAsync(eventFilter);

            // Assert

            Assert.NotNull(eventFilter.SelectedFields);
            Assert.Equal(6, eventFilter.SelectedFields.Count);
            Assert.Equal(NodeAttribute.NodeId, eventFilter.SelectedFields[0].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectedFields[0].TypeDefinitionId);
            Assert.Empty(eventFilter.SelectedFields[0].BrowsePath);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[1].TypeDefinitionId);
            Assert.Equal(BrowseNames.Comment, eventFilter.SelectedFields[1].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[2].TypeDefinitionId);
            Assert.Equal(BrowseNames.ConditionName, eventFilter.SelectedFields[2].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[3].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectedFields[3].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[4].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectedFields[4].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(BrowseNames.Id, eventFilter.SelectedFields[4].BrowsePath.ElementAtOrDefault(1));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[5].TypeDefinitionId);
            Assert.Equal(BrowseNames.Message, eventFilter.SelectedFields[5].BrowsePath.ElementAtOrDefault(0));

            Assert.NotNull(eventFilter.Filter);
            Assert.NotNull(eventFilter.Filter.Elements);
            Assert.Single(eventFilter.Filter.Elements);
            Assert.Equal(FilterOperatorType.OfType, eventFilter.Filter.Elements[0].FilterOperator);
            Assert.NotNull(eventFilter.Filter.Elements[0].FilterOperands);
            Assert.Single(eventFilter.Filter.Elements[0].FilterOperands);
            var operand = Assert.Single(eventFilter.Filter.Elements[0].FilterOperands);
            Assert.IsType<string>(operand.Value.Value);
            var nodeId = (string)operand.Value.Value;
            Assert.Equal(nodeId, ObjectTypeIds.ConditionType.ToString());
        }

        [Fact]
        public async Task SetupSimpleFilterForConditionTypeWithConditionHandlingEnabled()
        {
            // Arrange
            var eventFilter = new PublishedDataSetEventModel
            {
                Id = "111",
                EventNotifier = "i=2258",
                Name = "Condition",
                TypeDefinitionId = ObjectTypeIds.ConditionType.ToString(),
                ConditionHandling = new ConditionHandlingOptionsModel
                {
                    SnapshotInterval = 10
                }
            };

            // Act
            await ResolveAsync(eventFilter);

            // Assert
            Assert.NotNull(eventFilter.SelectedFields);
            Assert.Equal(6, eventFilter.SelectedFields.Count);
            Assert.Equal(NodeAttribute.NodeId, eventFilter.SelectedFields[0].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectedFields[0].TypeDefinitionId);
            Assert.Empty(eventFilter.SelectedFields[0].BrowsePath);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[1].TypeDefinitionId);
            Assert.Equal(BrowseNames.Comment, eventFilter.SelectedFields[1].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[2].TypeDefinitionId);
            Assert.Equal(BrowseNames.ConditionName, eventFilter.SelectedFields[2].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[3].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectedFields[3].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[4].TypeDefinitionId);
            Assert.Equal(BrowseNames.EnabledState, eventFilter.SelectedFields[4].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(BrowseNames.Id, eventFilter.SelectedFields[4].BrowsePath.ElementAtOrDefault(1));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectedFields[5].TypeDefinitionId);
            Assert.Equal(BrowseNames.Message, eventFilter.SelectedFields[5].BrowsePath.ElementAtOrDefault(0));

            Assert.NotNull(eventFilter.Filter);
            Assert.NotNull(eventFilter.Filter.Elements);
            Assert.Single(eventFilter.Filter.Elements);
            Assert.Equal(FilterOperatorType.OfType, eventFilter.Filter.Elements[0].FilterOperator);
            Assert.NotNull(eventFilter.Filter.Elements[0].FilterOperands);
            Assert.Single(eventFilter.Filter.Elements[0].FilterOperands);
            var operand = Assert.Single(eventFilter.Filter.Elements[0].FilterOperands);
            Assert.IsType<string>(operand.Value.Value);
            var nodeId = (string)operand.Value.Value;
            Assert.Equal(nodeId, ObjectTypeIds.ConditionType.ToString());
        }

        internal async Task ResolveAsync(
            PublishedDataSetEventModel template, NamespaceTable namespaceUris = null)
        {
            var session = SetupMockedSession(namespaceUris).Object;

            var writer = new DataSetWriterModel
            {
                Id = "1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedEvents = new PublishedEventItemsModel
                        {
                            PublishedData = new[] { template }
                        }
                    }
                }
            };
            var resolver = new DataSetWriterResolver(new[] { writer },
                Log.Console<DataSetWriterResolver>());
            await resolver.ResolveAsync(session, default);
        }

        protected override Mock<INodeCache> SetupMockedNodeCache(NamespaceTable namespaceTable = null)
        {
            var nodeCache = base.SetupMockedNodeCache(namespaceTable);
            AddNode(_baseObjectTypeNode);
            AddNode(_baseEventTypeNode);
            AddNode(_messageNode);
            AddNode(_conditionTypeNode);
            AddNode(_conditionNameNode);
            AddNode(_commentNode);
            AddNode(_enabledStateNode);
            AddNode(_idNode);
            var typeTable = nodeCache.Object.TypeTree as TypeTable;
            typeTable.Add(_baseObjectTypeNode);
            typeTable.Add(_baseEventTypeNode);
            typeTable.Add(_conditionTypeNode);
            typeTable.AddSubtype(ObjectTypeIds.BaseEventType, ObjectTypeIds.BaseObjectType);
            typeTable.AddSubtype(ObjectTypeIds.ConditionType, ObjectTypeIds.BaseEventType);
            _baseObjectTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasSubtype, false, ObjectTypeIds.BaseEventType);
            _baseEventTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasSubtype, true, ObjectTypeIds.BaseObjectType);
            _baseEventTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, false, _messageNode.NodeId);
            _messageNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, true, ObjectTypeIds.BaseEventType);
            _baseEventTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasSubtype, false, ObjectTypeIds.ConditionType);
            _conditionTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasSubtype, true, ObjectTypeIds.BaseEventType);
            _conditionTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, false, _conditionNameNode.NodeId);
            _conditionTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, false, _commentNode.NodeId);
            _conditionNameNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, true, ObjectTypeIds.ConditionType);
            _conditionTypeNode.ReferenceTable.Add(ReferenceTypeIds.HasComponent, false, _enabledStateNode.NodeId);
            _enabledStateNode.ReferenceTable.Add(ReferenceTypeIds.HasComponent, true, ObjectTypeIds.ConditionType);
            _enabledStateNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, false, _idNode.NodeId);
            _idNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, true, _enabledStateNode.NodeId);
            _commentNode.ReferenceTable.Add(ReferenceTypeIds.HasProperty, true, ObjectTypeIds.ConditionType);
            nodeCache.Setup(x => x.FetchNodeAsync(It.IsAny<ExpandedNodeId>(), It.IsAny<CancellationToken>())).Returns((ExpandedNodeId x, CancellationToken _) =>
            {
                if (x.IdType == IdType.Numeric && x.Identifier is uint id)
                {
                    return Task.FromResult(_nodes[id]);
                }
                return Task.FromResult<Node>(null);
            });
            return nodeCache;
        }

        private void AddNode(Node node)
        {
            _nodes[(uint)node.NodeId.Identifier] = node;
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

        private readonly Dictionary<uint, Node> _nodes = new();
    }
}
