// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using static Microsoft.Azure.IIoT.OpcUa.Protocol.Services.SubscriptionServices;

    public class MonitoredItemWrapperTests {
        [Fact]
        public void SetDefaultValuesWhenPropertiesAreNullInBaseTemplate() {
            var template = new DataMonitoredItemModel {
                AttributeId = null,
                MonitoringMode = null,
                QueueSize = null,
                SamplingInterval = null,
                DiscardNew = null,
            };
            var monitoredItemWrapper = GetMonitoredItemWrapper(template);

            Assert.Equal(Attributes.Value, monitoredItemWrapper.Item.AttributeId);
            Assert.Equal(MonitoringMode.Reporting, monitoredItemWrapper.Item.MonitoringMode);
            Assert.Equal((uint)1, monitoredItemWrapper.Item.QueueSize);
            Assert.Equal(1000, monitoredItemWrapper.Item.SamplingInterval);
            Assert.True(monitoredItemWrapper.Item.DiscardOldest);
        }

        [Fact]
        public void SetBaseValuesWhenPropertiesAreSetInBaseTemplate() {
            var template = new DataMonitoredItemModel {
                Id = "i=2258",
                DisplayName = "DisplayName",
                AttributeId = (NodeAttribute)Attributes.Value,
                IndexRange = "5:20",
                RelativePath = new[] { "RelativePath1", "RelativePath2" },
                MonitoringMode = Publisher.Models.MonitoringMode.Sampling,
                StartNodeId = "i=2258",
                QueueSize = 10,
                SamplingInterval = TimeSpan.FromMilliseconds(10000),
                DiscardNew = true
            };
            var monitoredItemWrapper = GetMonitoredItemWrapper(template);

            Assert.Equal("DisplayName", monitoredItemWrapper.Item.DisplayName);
            Assert.Equal((uint)NodeAttribute.Value, monitoredItemWrapper.Item.AttributeId);
            Assert.Equal("5:20", monitoredItemWrapper.Item.IndexRange);
            Assert.Equal("RelativePath1RelativePath2", monitoredItemWrapper.Item.RelativePath);
            Assert.Equal(MonitoringMode.Sampling, monitoredItemWrapper.Item.MonitoringMode);
            Assert.Equal("i=2258", monitoredItemWrapper.Item.StartNodeId);
            Assert.Equal(10u, monitoredItemWrapper.Item.QueueSize);
            Assert.Equal(10000, monitoredItemWrapper.Item.SamplingInterval);
            Assert.False(monitoredItemWrapper.Item.DiscardOldest);
            Assert.Equal(monitoredItemWrapper, monitoredItemWrapper.Item.Handle);
        }

        [Fact]
        public void SetDataChangeFilterWhenBaseTemplateIsDataTemplate() {
            var template = new DataMonitoredItemModel {
                DataChangeFilter = new DataChangeFilterModel {
                    DataChangeTrigger = Publisher.Models.DataChangeTriggerType.StatusValue,
                    DeadBandType = Publisher.Models.DeadbandType.Percent,
                    DeadBandValue = 10.0
                }
            };
            var monitoredItemWrapper = GetMonitoredItemWrapper(template);

            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<DataChangeFilter>(monitoredItemWrapper.Item.Filter);
            
            var dataChangeFilter = (DataChangeFilter)monitoredItemWrapper.Item.Filter;
            Assert.Equal(DataChangeTrigger.StatusValue, dataChangeFilter.Trigger);
            Assert.Equal((uint)DeadbandType.Percent, dataChangeFilter.DeadbandType);
            Assert.Equal(10.0, dataChangeFilter.DeadbandValue);
        }

        [Fact]
        public void SetEventFilterWhenBaseTemplateIsEventTemplate() {
            var template = new EventMonitoredItemModel {
                EventFilter = new EventFilterModel {
                    SelectClauses = new List<SimpleAttributeOperandModel> {
                        new SimpleAttributeOperandModel {
                            TypeDefinitionId = "i=2041",
                            BrowsePath = new []{ "EventId" }
                        },
                    },
                    WhereClause = new ContentFilterModel {
                        Elements = new List<ContentFilterElementModel> {
                            new ContentFilterElementModel {
                                FilterOperator = FilterOperatorType.OfType,
                                FilterOperands = new List<FilterOperandModel> {
                                    new FilterOperandModel {
                                        Value = "ns=2;i=235"
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var monitoredItemWrapper = GetMonitoredItemWrapper(template);

            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<EventFilter>(monitoredItemWrapper.Item.Filter);

            var eventFilter = (EventFilter)monitoredItemWrapper.Item.Filter;
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[0].TypeDefinitionId);
            Assert.Equal("EventId", eventFilter.SelectClauses[0].BrowsePath.ElementAtOrDefault(0));
            Assert.NotNull(eventFilter.WhereClause);
            Assert.Single(eventFilter.WhereClause.Elements);
            Assert.Equal(FilterOperator.OfType, eventFilter.WhereClause.Elements[0].FilterOperator);
            Assert.Single(eventFilter.WhereClause.Elements[0].FilterOperands);
            Assert.IsType<LiteralOperand>(eventFilter.WhereClause.Elements[0].FilterOperands[0].Body);

            var literalOperand = (LiteralOperand)eventFilter.WhereClause.Elements[0].FilterOperands[0].Body;
            Assert.Equal(new NodeId("ns=2;i=235"), literalOperand.Value.Value);
        }

        [Fact]
        public void AddTimestampSelectClausesWhenBaseTemplateIsEventTemplate() {
            var template = new EventMonitoredItemModel();
            var monitoredItemWrapper = GetMonitoredItemWrapper(template);

            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<EventFilter>(monitoredItemWrapper.Item.Filter);

            var eventFilter = (EventFilter)monitoredItemWrapper.Item.Filter;
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(3, eventFilter.SelectClauses.Count);
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[0].TypeDefinitionId);
            Assert.Equal("Time", eventFilter.SelectClauses[0].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[1].TypeDefinitionId);
            Assert.Equal("ReceiveTime", eventFilter.SelectClauses[1].BrowsePath.ElementAtOrDefault(0));
            Assert.Equal(ObjectTypeIds.BaseEventType, eventFilter.SelectClauses[2].TypeDefinitionId);
            Assert.Equal("EventType", eventFilter.SelectClauses[2].BrowsePath.ElementAtOrDefault(0));
        }

        [Fact]
        public void AddConditionTypeSelectClausesWhenPendingAlarmsIsSetInEventTemplate() {
            var template = new EventMonitoredItemModel {
                PendingAlarms = new PendingAlarmsOptionsModel {
                    IsEnabled = true
                }
            };
            var monitoredItemWrapper = GetMonitoredItemWrapper(template);

            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<EventFilter>(monitoredItemWrapper.Item.Filter);

            var eventFilter = (EventFilter)monitoredItemWrapper.Item.Filter;
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(5, eventFilter.SelectClauses.Count);
            Assert.Equal(Attributes.NodeId, eventFilter.SelectClauses[3].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[3].TypeDefinitionId);
            Assert.Empty(eventFilter.SelectClauses[3].BrowsePath);
            Assert.Equal(Attributes.Value, eventFilter.SelectClauses[4].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[4].TypeDefinitionId);
            Assert.Equal("Retain", eventFilter.SelectClauses[4].BrowsePath.FirstOrDefault());
        }

        private INodeCache GetNodeCache() {
            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var nodeCache = mock.Mock<INodeCache>();
            var typeTable = new TypeTable(new NamespaceTable());
            typeTable.Add(new Node(new ReferenceDescription() {
                TypeDefinition = ObjectTypeIds.BaseObjectType
            }));
            typeTable.AddSubtype(ObjectTypeIds.BaseEventType, ObjectTypeIds.BaseObjectType);
            typeTable.AddSubtype(ObjectTypeIds.ConditionType, ObjectTypeIds.BaseEventType);
            nodeCache.SetupGet(x => x.TypeTree).Returns(typeTable);
            nodeCache.Setup<Node>(x => x.FetchNode().Returns(new Node)
            return nodeCache.Object;
        }

        private MonitoredItemWrapper GetMonitoredItemWrapper(BaseMonitoredItemModel template, ServiceMessageContext messageContext = null, INodeCache nodeCache = null, IVariantEncoder codec = null, bool activate = true) {
            var monitoredItemWrapper = new MonitoredItemWrapper(template, Log.Logger);
            monitoredItemWrapper.Create(
                messageContext ?? new ServiceMessageContext(),
                nodeCache ?? GetNodeCache(),
                codec ?? new VariantEncoderFactory().Default,
                activate);
            return monitoredItemWrapper;
        }
    }
}
