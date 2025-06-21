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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using DeadbandType = Publisher.Models.DeadbandType;
    using MonitoringMode = Publisher.Models.MonitoringMode;

    public class OpcUaMonitoredItemTests : OpcUaMonitoredItemTestsBase
    {
        [Fact]
        public async Task SetDefaultValuesWhenPropertiesAreNullInBaseTemplateAsync()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DiscardNew = null
            };
            var monitoredItem = await GetMonitoredItemAsync(template) as OpcUaMonitoredItem.DataChange;

            Assert.Equal(Attributes.Value, monitoredItem.AttributeId);
            Assert.Equal(Opc.Ua.MonitoringMode.Reporting, monitoredItem.MonitoringMode);
            Assert.Equal(1000, monitoredItem.SamplingInterval);
            Assert.True(monitoredItem.DiscardOldest);
            Assert.False(monitoredItem.SkipMonitoredItemNotification());
        }

        [Fact]
        public async Task SetSkipFirstBeforeFirstNotificationProcessedSucceedsTestsAsync()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SkipFirst = true
            };
            var monitoredItem = await GetMonitoredItemAsync(template) as OpcUaMonitoredItem.DataChange;
            Assert.False(monitoredItem.TrySetSkipFirst(true));
            Assert.True(monitoredItem.TrySetSkipFirst(false));
            Assert.True(monitoredItem.TrySetSkipFirst(true));
            Assert.True(monitoredItem.TrySetSkipFirst(false));
            Assert.True(monitoredItem.TrySetSkipFirst(true));
            Assert.True(monitoredItem.SkipMonitoredItemNotification());
            // This is allowed since it does not matter
            Assert.True(monitoredItem.TrySetSkipFirst(false));
            Assert.False(monitoredItem.TrySetSkipFirst(true));
            Assert.False(monitoredItem.SkipMonitoredItemNotification());
        }

        [Fact]
        public async Task SetSkipFirstAfterFirstNotificationProcessedFailsTestsAsync()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SkipFirst = true
            };
            var monitoredItem = await GetMonitoredItemAsync(template) as OpcUaMonitoredItem.DataChange;
            Assert.True(monitoredItem.SkipMonitoredItemNotification());
            Assert.False(monitoredItem.TrySetSkipFirst(true));
            // This is allowed since it does not matter
            Assert.True(monitoredItem.TrySetSkipFirst(false));
            Assert.False(monitoredItem.TrySetSkipFirst(true));
            // This is allowed since it does not matter
            Assert.True(monitoredItem.TrySetSkipFirst(false));
            Assert.False(monitoredItem.SkipMonitoredItemNotification());
        }

        [Fact]
        public async Task NotsetSkipFirstAfterFirstNotificationProcessedFailsSettingTestsAsync()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258"
            };
            var monitoredItem = await GetMonitoredItemAsync(template) as OpcUaMonitoredItem.DataChange;
            Assert.False(monitoredItem.SkipMonitoredItemNotification());
            Assert.False(monitoredItem.TrySetSkipFirst(true));
            Assert.False(monitoredItem.SkipMonitoredItemNotification());
        }

        [Fact]
        public async Task SetBaseValuesWhenPropertiesAreSetInBaseTemplateAsync()
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
            var monitoredItem = await GetMonitoredItemAsync(template) as OpcUaMonitoredItem.DataChange;

            Assert.Equal("DisplayName", monitoredItem.DisplayName);
            Assert.Equal((uint)NodeAttribute.Value, monitoredItem.AttributeId);
            Assert.Equal("5:20", monitoredItem.IndexRange);
            Assert.Equal(Opc.Ua.MonitoringMode.Sampling, monitoredItem.MonitoringMode);
            Assert.Equal("i=2258", monitoredItem.StartNodeId);
            Assert.Equal(10u, monitoredItem.QueueSize);
            Assert.Equal(10000, monitoredItem.SamplingInterval);
            Assert.False(monitoredItem.DiscardOldest);
            Assert.Null(monitoredItem.Handle);
            Assert.True(monitoredItem.SkipMonitoredItemNotification());
        }

        [Fact]
        public async Task SetDataChangeFilterWhenBaseTemplateIsDataTemplateAsync()
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
            var monitoredItem = await GetMonitoredItemAsync(template);

            Assert.NotNull(monitoredItem.Filter);
            Assert.IsType<DataChangeFilter>(monitoredItem.Filter);

            var dataChangeFilter = (DataChangeFilter)monitoredItem.Filter;
            Assert.Equal(DataChangeTrigger.StatusValue, dataChangeFilter.Trigger);
            Assert.Equal((uint)Opc.Ua.DeadbandType.Percent, dataChangeFilter.DeadbandType);
            Assert.Equal(10.0, dataChangeFilter.DeadbandValue);
        }

        [Fact]
        public async Task SetEventFilterWhenBaseTemplateIsEventTemplateAsync()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = new List<SimpleAttributeOperandModel> {
                        new()
                        {
                            TypeDefinitionId = "i=2041",
                            BrowsePath = new []{ "EventId" }
                        }
                    },
                    WhereClause = new ContentFilterModel
                    {
                        Elements = new List<ContentFilterElementModel> {
                            new() {
                                FilterOperator = FilterOperatorType.OfType,
                                FilterOperands = new List<FilterOperandModel> {
                                    new() {
                                        Value = "ns=2;i=235"
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var monitoredItem = await GetMonitoredItemAsync(template);

            Assert.NotNull(monitoredItem.Filter);
            Assert.IsType<EventFilter>(monitoredItem.Filter);

            var eventFilter = (EventFilter)monitoredItem.Filter;
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
        public async Task AddConditionTypeSelectClausesWhenPendingAlarmsIsSetInEventTemplateAsync()
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
            var monitoredItem = await GetMonitoredItemAsync(template);

            Assert.NotNull(monitoredItem.Filter);
            Assert.IsType<EventFilter>(monitoredItem.Filter);

            var eventFilter = (EventFilter)monitoredItem.Filter;
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
        public async Task SetupFieldNameWithNamespaceNameWhenNamespaceIndexIsUsedAsync()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = new List<SimpleAttributeOperandModel> {
                        new()
                        {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CycleId" }
                        },
                        new()
                        {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CurrentStep" }
                        }
                    },
                    WhereClause = new ContentFilterModel
                    {
                        Elements = new List<ContentFilterElementModel>
                        {
                            new()
                            {
                                FilterOperator = FilterOperatorType.OfType,
                                FilterOperands = new List<FilterOperandModel> {
                                    new() {
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
            var eventItem = await GetMonitoredItemAsync(template, namespaceTable) as OpcUaMonitoredItem.Event;

            Assert.Equal(((EventFilter)eventItem.Filter).SelectClauses.Count, eventItem.Fields.Count);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CycleId", eventItem.Fields[0].Name);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep", eventItem.Fields[1].Name);
        }

        [Fact]
        public async Task UseDefaultFieldNameWhenNamespaceTableIsEmptyAsync()
        {
            var namespaceUris = new NamespaceTable();
            namespaceUris.Append("http://test");
            namespaceUris.Append("http://opcfoundation.org/Quickstarts/SimpleEvents");

            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = new List<SimpleAttributeOperandModel>
                    {
                        new()
                        {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CycleId" }
                        },
                        new()
                        {
                            TypeDefinitionId = "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            BrowsePath = new []{ "2:CurrentStep" }
                        }
                    },
                    WhereClause = new ContentFilterModel
                    {
                        Elements = new List<ContentFilterElementModel>
                        {
                            new()
                            {
                                FilterOperator = FilterOperatorType.OfType,
                                FilterOperands = new List<FilterOperandModel>
                                {
                                    new()
                                    {
                                        Value = "ns=2;i=235"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var eventItem = await GetMonitoredItemAsync(template, namespaceUris) as OpcUaMonitoredItem.Event;

            Assert.Equal(((EventFilter)eventItem.Filter).SelectClauses.Count, eventItem.Fields.Count);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CycleId", eventItem.Fields[0].Name);
            Assert.Equal("http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep", eventItem.Fields[1].Name);
        }
    }
}
