// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Model.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Models;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class MonitoredItemModelTests
    {
        private readonly DataMonitoredItemModel _dataModel = new()
        {
            StartNodeId = "DataStartNodeId",
            AggregateFilter = new AggregateFilterModel()
            {
                AggregateConfiguration = new AggregateConfigurationModel()
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
            DataChangeFilter = new DataChangeFilterModel()
            {
                DataChangeTrigger = DataChangeTriggerType.StatusValue,
                DeadbandType = DeadbandType.Absolute,
                DeadbandValue = 45
            },
            DiscardNew = true,
            SkipFirst = true,
            DisplayName = "DataDisplayName",
            HeartbeatInterval = TimeSpan.FromMilliseconds(30000),
            Id = "Id",
            IndexRange = "DataIndexRange",
            MonitoringMode = MonitoringMode.Sampling,
            RelativePath = new string[] { "DataRelativePath" }
        };
        private readonly EventMonitoredItemModel _eventModel = new()
        {
            StartNodeId = "EventStartNodeId",
            SamplingInterval = TimeSpan.FromMilliseconds(5000),
            QueueSize = 10,
            AttributeId = NodeAttribute.DataType,
            DiscardNew = true,
            DisplayName = "EventDisplayName",
            Id = "Id",
            IndexRange = "EventIndexRange",
            MonitoringMode = MonitoringMode.Sampling,
            RelativePath = new string[] { "EventRelativePath" },
            EventFilter = new EventFilterModel()
            {
                SelectClauses = new List<SimpleAttributeOperandModel>() {
                    new SimpleAttributeOperandModel() {
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
            var clone = _dataModel.Clone();

            // Should be equal
            Assert.IsType<DataMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_dataModel));
        }

        [Fact]
        public void CompareDataModelTestShouldSucceed()
        {
            var clone = _dataModel.Clone();

            // Should be equal
            Assert.IsType<DataMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_dataModel));
        }

        [Fact]
        public void CompareDataModelTestShouldFail()
        {
            var clone = _dataModel.Clone();
            clone.QueueSize = 47000;

            // Should not be equal
            Assert.IsType<DataMonitoredItemModel>(clone);
            Assert.False(clone.Equals(_dataModel));
        }

        [Fact]
        public void CloneEventModelTest()
        {
            var clone = _eventModel.Clone();

            // Should be equal
            Assert.IsType<EventMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_eventModel));
        }

        [Fact]
        public void CompareEventModelTestShouldSucceed()
        {
            var clone = _eventModel.Clone();

            // Should be equal
            Assert.IsType<EventMonitoredItemModel>(clone);
            Assert.True(clone.Equals(_eventModel));
        }

        [Fact]
        public void CompareEventModelTestShouldFail()
        {
            var clone = _eventModel.Clone();
            clone.StartNodeId = "SomethingElse";

            // Should not be equal
            Assert.IsType<EventMonitoredItemModel>(clone);
            Assert.False(clone.Equals(_eventModel));
        }
    }
}
