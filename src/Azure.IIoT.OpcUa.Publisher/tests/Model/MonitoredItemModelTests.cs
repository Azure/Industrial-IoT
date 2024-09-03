// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class MonitoredItemModelTests
    {
        private readonly BaseMonitoredItemModel _dataModel = new DataMonitoredItemModel
        {
            StartNodeId = "DataStartNodeId",
            AggregateFilter = new AggregateFilterModel
            {
                AggregateConfiguration = new AggregateConfigurationModel
                {
                    PercentDataBad = 10,
                    PercentDataGood = 20,
                    TreatUncertainAsBad = false,
                    UseSlopedExtrapolation = false
                },
                AggregateTypeId = "DataAggregateTypeId",
                ProcessingInterval = TimeSpan.FromMilliseconds(25),
                StartTime = DateTime.Now
            },
            DataSetClassFieldId = Guid.NewGuid(),
            SamplingInterval = TimeSpan.FromMilliseconds(5000),
            QueueSize = 10,
            AttributeId = NodeAttribute.DataType,
            DataChangeFilter = new DataChangeFilterModel
            {
                DataChangeTrigger = DataChangeTriggerType.StatusValue,
                DeadbandType = DeadbandType.Absolute,
                DeadbandValue = 45
            },
            DiscardNew = true,
            SkipFirst = true,
            DataSetFieldName = "DataSetFieldName",
            HeartbeatInterval = TimeSpan.FromMilliseconds(30000),
            DataSetFieldId = "DataSetFieldId",
            IndexRange = "DataIndexRange",
            MonitoringMode = MonitoringMode.Sampling,
            RelativePath = new string[] { "DataRelativePath" }
        };
        private readonly BaseMonitoredItemModel _eventModel = new EventMonitoredItemModel
        {
            StartNodeId = "EventStartNodeId",
            QueueSize = 10,
            AttributeId = NodeAttribute.DataType,
            DiscardNew = true,
            DataSetFieldName = "EventDataSetFieldName",
            DataSetFieldId = "DataSetFieldId",
            MonitoringMode = MonitoringMode.Sampling,
            RelativePath = new string[] { "EventRelativePath" },
            EventFilter = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new()
                    {
                        AttributeId = NodeAttribute.DataType,
                        BrowsePath = new string[] { "EventBrowsePath "},
                        IndexRange = "SelectClauseIndexRange",
                        TypeDefinitionId = "SelectClauseTypeDefinitionId",
                        DisplayName = "SelectClauseDisplayName",
                        DataSetClassFieldId = Guid.NewGuid()
                    }
                }
            }
        };

        [Fact]
        public void CloneDataModelTest()
        {
            var clone = _dataModel with { };

            // Should be equal
            Assert.IsType<DataMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_dataModel));
        }

        [Fact]
        public void CompareDataModelTestShouldSucceed()
        {
            var clone = _dataModel with { };

            // Should be equal
            Assert.IsType<DataMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_dataModel));
        }

        [Fact]
        public void CompareDataModelTestShouldFail()
        {
            var clone = _dataModel with { QueueSize = 47000 };

            // Should not be equal
            Assert.IsType<DataMonitoredItemModel>(clone);
            Assert.False(clone.Equals(_dataModel));
        }

        [Fact]
        public void CloneEventModelTest()
        {
            var clone = _eventModel with { };

            // Should be equal
            Assert.IsType<EventMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_eventModel));
        }

        [Fact]
        public void CompareEventModelTestShouldSucceed()
        {
            var clone = _eventModel with { };

            // Should be equal
            Assert.IsType<EventMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_eventModel));
        }

        [Fact]
        public void CompareEventModelTestShouldFail()
        {
            var clone = _eventModel with { StartNodeId = "SomethingElse" };

            // Should not be equal
            Assert.IsType<EventMonitoredItemModel>(clone);
            Assert.False(clone.Equals(_eventModel));
        }
    }
}
