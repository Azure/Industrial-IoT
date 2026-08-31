// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class MonitoredItemMetaDataBuilderTests :
        OpcUaMonitoredItemTestsBase
    {
        [Fact]
        public async Task BuilderCreatesExpectedBuiltInDataChangeMetadataAsync()
        {
            var fieldId = Guid.NewGuid();
            var template = new DataMonitoredItemModel
            {
                StartNodeId = $"i={kVariableNodeId}",
                DataSetFieldName = "Temperature",
                DataSetClassFieldId = fieldId
            };
            var session = SetupMockedSession().Object;
            var builderFields = new List<PublishedFieldMetaDataModel>();
            var builderDataTypes = new NodeIdDictionary<object>();
            await new MonitoredItemMetaDataBuilder(NullLogger.Instance)
                .BuildDataChangeAsync(session, typeSystem: null, template,
                    builderFields, builderDataTypes, default);

            var field = Assert.Single(builderFields);
            Assert.Equal("Temperature", field.Name);
            Assert.Equal(fieldId, (Guid)field.Id);
            Assert.Equal("i=6", field.DataType);
            Assert.Equal(ValueRanks.Scalar, field.ValueRank);
            Assert.Equal("Temperature@en", field.Description);
            Assert.Equal((byte)BuiltInType.Int32, field.BuiltInType);
            Assert.Empty(builderDataTypes);
        }

        [Fact]
        public async Task BuilderIgnoresUnknownNodeWithoutAddingMetadataAsync()
        {
            var fields = new List<PublishedFieldMetaDataModel>();
            var dataTypes = new NodeIdDictionary<object>();

            await new MonitoredItemMetaDataBuilder(NullLogger.Instance)
                .BuildDataChangeAsync(SetupMockedSession().Object, typeSystem: null,
                    new DataMonitoredItemModel
                    {
                        StartNodeId = "i=9999",
                        DataSetFieldName = "Missing"
                    }, fields, dataTypes, default);

            Assert.Empty(fields);
            Assert.Empty(dataTypes);
        }

        [Fact]
        public async Task BuilderCreatesExpectedEventMetadataAsync()
        {
            var fieldId = Guid.NewGuid();
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2253",
                EventFilter = new EventFilterModel
                {
                    SelectClauses =
                    [
                        new SimpleAttributeOperandModel
                        {
                            DataSetClassFieldId = fieldId
                        }
                    ]
                }
            };
            var filter = new EventFilter
            {
                SelectClauses =
                [
                    new SimpleAttributeOperand
                    {
                        TypeDefinitionId = _eventType.NodeId,
                        BrowsePath = [new QualifiedName("Temperature")]
                    }
                ]
            };
            var fields = new List<PublishedFieldMetaDataModel>();
            var dataTypes = new NodeIdDictionary<object>();
            var session = SetupMockedSession();
            var nodeCache = new Mock<ILruNodeCache>();
            nodeCache.Setup(cache => cache.GetNodeAsync(
                    It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
                .Returns((NodeId nodeId, CancellationToken _) =>
                    ValueTask.FromResult<INode>(nodeId == _variable.NodeId ?
                        _variable : _eventType));
            nodeCache.Setup(cache => cache.GetReferencesAsync(
                    It.IsAny<NodeId>(), It.IsAny<NodeId>(), false, true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new INode[] { _variable });
            nodeCache.Setup(cache => cache.GetBuiltInTypeAsync(
                    It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
                .Returns((NodeId dataTypeId, CancellationToken _) =>
                    ValueTask.FromResult(dataTypeId == DataTypeIds.Int32 ?
                        BuiltInType.Int32 : BuiltInType.Variant));
            session.SetupGet(value => value.LruNodeCache).Returns(nodeCache.Object);

            await new MonitoredItemMetaDataBuilder(NullLogger.Instance)
                .BuildEventAsync(session.Object, typeSystem: null,
                    template, filter, ["Temperature"], [fieldId],
                    fields, dataTypes, default);

            var field = Assert.Single(fields);
            Assert.Equal("Temperature", field.Name);
            Assert.Equal(fieldId, (Guid)field.Id);
            Assert.Equal("i=6", field.DataType);
            Assert.Equal((byte)BuiltInType.Int32, field.BuiltInType);
        }

        protected override Node GetNode(uint id)
        {
            return id switch
            {
                kVariableNodeId => _variable,
                kEventTypeNodeId => _eventType,
                _ => base.GetNode(id)
            };
        }

        private const uint kVariableNodeId = 1001;
        private const uint kEventTypeNodeId = 2001;
        private readonly ObjectTypeNode _eventType = new()
        {
            BrowseName = new QualifiedName("EventType"),
            DisplayName = new LocalizedText("EventType"),
            NodeClass = Opc.Ua.NodeClass.ObjectType,
            NodeId = new NodeId(kEventTypeNodeId)
        };
        private readonly VariableNode _variable = new()
        {
            BrowseName = new QualifiedName("Temperature"),
            DataType = DataTypeIds.Int32,
            Description = new LocalizedText("en", "Temperature"),
            DisplayName = new LocalizedText("en", "Temperature"),
            NodeClass = Opc.Ua.NodeClass.Variable,
            NodeId = new NodeId(kVariableNodeId),
            ValueRank = ValueRanks.Scalar
        };
    }
}
