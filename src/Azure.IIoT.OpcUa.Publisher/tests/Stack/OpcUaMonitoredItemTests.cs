// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Opc.Ua;
    using DeadbandType = Publisher.Models.DeadbandType;
    using MonitoringMode = Publisher.Models.MonitoringMode;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class OpcUaMonitoredItemTests : OpcUaMonitoredItemTestsBase
    {
        [Fact]
        public void SetDefaultValuesWhenPropertiesAreNullInBaseTemplate()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DiscardNew = null
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem.DataItem;

            Assert.Equal(Attributes.Value, monitoredItemWrapper.Item.AttributeId);
            Assert.Equal(Opc.Ua.MonitoringMode.Reporting, monitoredItemWrapper.Item.MonitoringMode);
            Assert.Equal(1000, monitoredItemWrapper.Item.SamplingInterval);
            Assert.True(monitoredItemWrapper.Item.DiscardOldest);
            Assert.False(monitoredItemWrapper.SkipMonitoredItemNotification());
        }

        [Fact]
        public void SetSkipFirstBeforeFirstNotificationProcessedSucceedsTests()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SkipFirst = true
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem.DataItem;
            Assert.False(monitoredItemWrapper.TrySetSkipFirst(true));
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(false));
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(true));
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(false));
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(true));
            Assert.True(monitoredItemWrapper.SkipMonitoredItemNotification());
            // This is allowed since it does not matter
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(false));
            Assert.False(monitoredItemWrapper.TrySetSkipFirst(true));
            Assert.False(monitoredItemWrapper.SkipMonitoredItemNotification());
        }

        [Fact]
        public void SetSkipFirstAfterFirstNotificationProcessedFailsTests()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SkipFirst = true
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem.DataItem;
            Assert.True(monitoredItemWrapper.SkipMonitoredItemNotification());
            Assert.False(monitoredItemWrapper.TrySetSkipFirst(true));
            // This is allowed since it does not matter
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(false));
            Assert.False(monitoredItemWrapper.TrySetSkipFirst(true));
            // This is allowed since it does not matter
            Assert.True(monitoredItemWrapper.TrySetSkipFirst(false));
            Assert.False(monitoredItemWrapper.SkipMonitoredItemNotification());
        }

        [Fact]
        public void NotsetSkipFirstAfterFirstNotificationProcessedFailsSettingTests()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258"
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem.DataItem;
            Assert.False(monitoredItemWrapper.SkipMonitoredItemNotification());
            Assert.False(monitoredItemWrapper.TrySetSkipFirst(true));
            Assert.False(monitoredItemWrapper.SkipMonitoredItemNotification());
        }

        [Fact]
        public void SetBaseValuesWhenPropertiesAreSetInBaseTemplate()
        {
            var template = new DataMonitoredItemModel
            {
                DataSetFieldId = "i=2258",
                DataSetFieldName = "DisplayName",
                AttributeId = (NodeAttribute)Attributes.Value,
                IndexRange = "5:20",
                RelativePath = new[] { "RelativePath1", "RelativePath2" },
                MonitoringMode = MonitoringMode.Sampling,
                StartNodeId = "i=2258",
                DataSetClassFieldId = Guid.NewGuid(),
                QueueSize = 10,
                SkipFirst = true,
                SamplingInterval = TimeSpan.FromMilliseconds(10000),
                DiscardNew = true
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem.DataItem;

            Assert.Equal("DisplayName", monitoredItemWrapper.Item.DisplayName);
            Assert.Equal((uint)NodeAttribute.Value, monitoredItemWrapper.Item.AttributeId);
            Assert.Equal("5:20", monitoredItemWrapper.Item.IndexRange);
            Assert.Null(monitoredItemWrapper.Item.RelativePath);
            Assert.Equal(Opc.Ua.MonitoringMode.Sampling, monitoredItemWrapper.Item.MonitoringMode);
            Assert.Equal("i=2258", monitoredItemWrapper.Item.StartNodeId);
            Assert.Equal(10u, monitoredItemWrapper.Item.QueueSize);
            Assert.Equal(10000, monitoredItemWrapper.Item.SamplingInterval);
            Assert.False(monitoredItemWrapper.Item.DiscardOldest);
            Assert.Equal(monitoredItemWrapper, monitoredItemWrapper.Item.Handle);
            Assert.True(monitoredItemWrapper.SkipMonitoredItemNotification());
        }

        [Fact]
        public void SetDataChangeFilterWhenBaseTemplateIsDataTemplate()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DataChangeFilter = new DataChangeFilterModel
                {
                    DataChangeTrigger = DataChangeTriggerType.StatusValue,
                    DeadbandType = DeadbandType.Percent,
                    DeadbandValue = 10.0
                }
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem;

            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<DataChangeFilter>(monitoredItemWrapper.Item.Filter);

            var dataChangeFilter = (DataChangeFilter)monitoredItemWrapper.Item.Filter;
            Assert.Equal(DataChangeTrigger.StatusValue, dataChangeFilter.Trigger);
            Assert.Equal((uint)Opc.Ua.DeadbandType.Percent, dataChangeFilter.DeadbandType);
            Assert.Equal(10.0, dataChangeFilter.DeadbandValue);
        }

        [Fact]
        public void SetEventFilterWhenBaseTemplateIsEventTemplate()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = new List<SimpleAttributeOperandModel> {
                        new SimpleAttributeOperandModel {
                            TypeDefinitionId = "i=2041",
                            BrowsePath = new []{ "EventId" }
                        }
                    },
                    WhereClause = new ContentFilterModel
                    {
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
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem;

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
        public void AddConditionTypeSelectClausesWhenPendingAlarmsIsSetInEventTemplate()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel(),
                ConditionHandling = new ConditionHandlingOptionsModel
                {
                    SnapshotInterval = 10,
                    UpdateInterval = 20
                }
            };
            var monitoredItemWrapper = GetMonitoredItem(template) as OpcUaMonitoredItem;

            Assert.NotNull(monitoredItemWrapper.Item.Filter);
            Assert.IsType<EventFilter>(monitoredItemWrapper.Item.Filter);

            var eventFilter = (EventFilter)monitoredItemWrapper.Item.Filter;
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.Equal(11, eventFilter.SelectClauses.Count);
            Assert.Equal(Attributes.NodeId, eventFilter.SelectClauses[9].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[9].TypeDefinitionId);
            Assert.Empty(eventFilter.SelectClauses[9].BrowsePath);
            Assert.Equal(Attributes.Value, eventFilter.SelectClauses[10].AttributeId);
            Assert.Equal(ObjectTypeIds.ConditionType, eventFilter.SelectClauses[10].TypeDefinitionId);
            Assert.Equal("Retain", eventFilter.SelectClauses[10].BrowsePath.FirstOrDefault());
        }

        [Fact]
        public void SetupFieldNameWithNamespaceNameWhenNamespaceIndexIsUsed()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = new List<SimpleAttributeOperandModel> {
                        new SimpleAttributeOperandModel {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CycleId" }
                        },
                        new SimpleAttributeOperandModel {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CurrentStep" }
                        }
                    },
                    WhereClause = new ContentFilterModel
                    {
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

            var namespaceTable = new NamespaceTable(new[] {
                Namespaces.OpcUa,
                "http://opcfoundation.org/UA/Diagnostics",
                "http://opcfoundation.org/Quickstarts/SimpleEvents"
            });
            var eventItem = GetMonitoredItem(template, namespaceTable) as OpcUaMonitoredItem.EventItem;

            Assert.Equal(((EventFilter)eventItem.Item.Filter).SelectClauses.Count, eventItem.Fields.Count);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CycleId", eventItem.Fields[0].Name);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep", eventItem.Fields[1].Name);
        }

        [Fact]
        public void UseDefaultFieldNameWhenNamespaceTableIsEmpty()
        {
            var namespaceUris = new NamespaceTable();
            namespaceUris.Append("http://test");
            namespaceUris.Append("http://opcfoundation.org/Quickstarts/SimpleEvents");

            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = new List<SimpleAttributeOperandModel> {
                        new SimpleAttributeOperandModel {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CycleId" }
                        },
                        new SimpleAttributeOperandModel {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CurrentStep" }
                        }
                    },
                    WhereClause = new ContentFilterModel
                    {
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

            var eventItem = GetMonitoredItem(template, namespaceUris) as OpcUaMonitoredItem.EventItem;

            Assert.Equal(((EventFilter)eventItem.Item.Filter).SelectClauses.Count, eventItem.Fields.Count);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CycleId", eventItem.Fields[0].Name);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep", eventItem.Fields[1].Name);
        }
    }
}
