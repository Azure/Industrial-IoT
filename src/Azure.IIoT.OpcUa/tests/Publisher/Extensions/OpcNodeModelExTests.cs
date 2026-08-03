// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Config.Models
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Globalization;
    using Xunit;

    public class OpcNodeModelExTests
    {
        [Fact]
        public void ComparerTest()
        {
            var comparer = OpcNodeModelEx.Comparer;

            var opcNode1 = new OpcNodeModel();
            var opcNode2 = new OpcNodeModel();

            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode1 = new OpcNodeModel
            {
                Id = "id",
                OpcPublishingInterval = 1500,
                OpcSamplingInterval = 2500,
                HeartbeatInterval = 35,
                QueueSize = 123,
                DataChangeTrigger = DataChangeTriggerType.StatusValue,
                DeadbandType = DeadbandType.Absolute,
                DeadbandValue = 0.1
            };

            static OpcNodeModel NewNode() => new()
            {
                Id = "id",
                OpcPublishingIntervalTimespan = TimeSpan.Parse("00:00:01.5", CultureInfo.InvariantCulture),
                OpcSamplingIntervalTimespan = TimeSpan.Parse("00:00:02.500", CultureInfo.InvariantCulture),
                HeartbeatIntervalTimespan = TimeSpan.Parse("00:00:35", CultureInfo.InvariantCulture),
                SkipFirst = true,
                QueueSize = 123,
                DataChangeTrigger = DataChangeTriggerType.StatusValue,
                DeadbandType = DeadbandType.Absolute,
                DeadbandValue = 0.1
            };

            opcNode2 = NewNode();
            opcNode2.SkipFirst = false;
            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            // Set skip first to true like factory
            opcNode2 = NewNode();
            opcNode1.SkipFirst = true;
            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.SkipFirst = false;
            opcNode2.QueueSize = 123;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.SkipFirst = true;
            opcNode2.QueueSize = 321;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.SkipFirst = true;
            opcNode2.QueueSize = 123;
            opcNode2.DataChangeTrigger = DataChangeTriggerType.Status;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.SkipFirst = null;
            opcNode2.QueueSize = null;
            opcNode2.DataChangeTrigger = null;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.DataChangeTrigger = null;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.DeadbandType = DeadbandType.Percent;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.DeadbandType = null;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode2 = NewNode();
            opcNode2.DeadbandValue = null;

            Assert.False(comparer.Equals(opcNode1, opcNode2));
            Assert.False(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));
        }

        [Fact]
        public void ComparerHashCodeUsesEqualityDefaults()
        {
            var comparer = OpcNodeModelEx.Comparer;
            var opcNode1 = new OpcNodeModel
            {
                Id = "Node",
                ExpandedNodeId = "Expanded",
                EventFilter = new EventFilterModel
                {
                    SelectClauses =
                    [
                        new SimpleAttributeOperandModel
                        {
                            TypeDefinitionId = "i=2041",
                            BrowsePath = ["EventId"]
                        }
                    ]
                }
            };
            var opcNode2 = new OpcNodeModel
            {
                Id = "node",
                DisplayName = string.Empty,
                Topic = string.Empty,
                DataSetFieldId = string.Empty,
                ExpandedNodeId = "expanded",
                QualityOfService = QoS.AtLeastOnce,
                HeartbeatBehavior = HeartbeatBehavior.WatchdogLKV,
                SkipFirst = false,
                DiscardNew = false,
                UseCyclicRead = false,
                RegisterNode = false,
                EventFilter = new EventFilterModel
                {
                    SelectClauses =
                    [
                        new SimpleAttributeOperandModel
                        {
                            TypeDefinitionId = "i=2041",
                            BrowsePath = ["EventId"]
                        }
                    ]
                }
            };

            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.Equal(comparer.GetHashCode(opcNode1), comparer.GetHashCode(opcNode2));
        }

        [Fact]
        public void ComparerHashCodeUsesTriggeredNodeSetSemantics()
        {
            var comparer = OpcNodeModelEx.Comparer;
            var firstTrigger = new OpcNodeModel { Id = "first" };
            var secondTrigger = new OpcNodeModel { Id = "second" };
            var opcNode1 = new OpcNodeModel
            {
                Id = "node",
                TriggeredNodes = [firstTrigger, secondTrigger, firstTrigger]
            };
            var opcNode2 = new OpcNodeModel
            {
                Id = "node",
                TriggeredNodes = [secondTrigger, firstTrigger]
            };

            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.Equals(opcNode2, opcNode1));
            Assert.Equal(comparer.GetHashCode(opcNode1), comparer.GetHashCode(opcNode2));
        }

        [Fact]
        public void ComparerTreatsMissingAndEmptyTriggeredNodesAsEqual()
        {
            var comparer = OpcNodeModelEx.Comparer;
            var opcNode1 = new OpcNodeModel { Id = "node" };
            var opcNode2 = new OpcNodeModel
            {
                Id = "node",
                TriggeredNodes = []
            };

            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.Equals(opcNode2, opcNode1));
            Assert.Equal(comparer.GetHashCode(opcNode1), comparer.GetHashCode(opcNode2));
        }

        [Fact]
        public void TryGetIdReturnsFirstAvailableIdentity()
        {
            var node = new OpcNodeModel
            {
                Id = " id ",
                ExpandedNodeId = "expanded"
            };

            var result = node.TryGetId(out var id);

            Assert.Equal(true, result);
            Assert.Equal(" id ", id);

            node = new OpcNodeModel
            {
                Id = " ",
                ExpandedNodeId = "expanded"
            };

            result = node.TryGetId(out id);

            Assert.Equal(true, result);
            Assert.Equal("expanded", id);

            node = new OpcNodeModel
            {
                BrowsePath = ["Objects", "Server"]
            };

            result = node.TryGetId(out id);

            Assert.Equal(true, result);
            Assert.Equal(Opc.Ua.ObjectIds.RootFolder.ToString(), id);

            node = new OpcNodeModel
            {
                ModelChangeHandling = new ModelChangeHandlingOptionsModel()
            };

            result = node.TryGetId(out id);

            Assert.Equal(true, result);
            Assert.Equal(Opc.Ua.ObjectIds.Server.ToString(), id);
        }

        [Fact]
        public void TryGetIdReturnsFalseWhenNoIdentityIsAvailable()
        {
            var node = new OpcNodeModel
            {
                Id = " ",
                BrowsePath = []
            };

            var result = node.TryGetId(out var id);

            Assert.Equal(false, result);
            Assert.Null(id);
        }

        [Fact]
        public void NormalizedIntervalsPreferTimeSpanThenLegacyIntegerThenDefault()
        {
            var node = new OpcNodeModel
            {
                HeartbeatInterval = 2,
                HeartbeatIntervalTimespan = TimeSpan.FromSeconds(3),
                OpcPublishingInterval = 100,
                OpcPublishingIntervalTimespan = TimeSpan.FromMilliseconds(200),
                OpcSamplingInterval = 300,
                OpcSamplingIntervalTimespan = TimeSpan.FromMilliseconds(400),
                CyclicReadMaxAge = 500,
                CyclicReadMaxAgeTimespan = TimeSpan.FromMilliseconds(600)
            };

            Assert.Equal(TimeSpan.FromSeconds(3),
                node.GetNormalizedHeartbeatInterval(TimeSpan.FromSeconds(4)));
            Assert.Equal(TimeSpan.FromMilliseconds(200),
                node.GetNormalizedPublishingInterval(TimeSpan.FromMilliseconds(700)));
            Assert.Equal(TimeSpan.FromMilliseconds(400),
                node.GetNormalizedSamplingInterval(TimeSpan.FromMilliseconds(800)));
            Assert.Equal(TimeSpan.FromMilliseconds(600),
                node.GetNormalizedCyclicReadMaxAge(TimeSpan.FromMilliseconds(900)));

            node = new OpcNodeModel
            {
                HeartbeatInterval = 2,
                OpcPublishingInterval = 100,
                OpcSamplingInterval = 300,
                CyclicReadMaxAge = 500
            };

            Assert.Equal(TimeSpan.FromSeconds(2),
                node.GetNormalizedHeartbeatInterval(TimeSpan.FromSeconds(4)));
            Assert.Equal(TimeSpan.FromMilliseconds(100),
                node.GetNormalizedPublishingInterval(TimeSpan.FromMilliseconds(700)));
            Assert.Equal(TimeSpan.FromMilliseconds(300),
                node.GetNormalizedSamplingInterval(TimeSpan.FromMilliseconds(800)));
            Assert.Equal(TimeSpan.FromMilliseconds(500),
                node.GetNormalizedCyclicReadMaxAge(TimeSpan.FromMilliseconds(900)));

            node = new OpcNodeModel();

            Assert.Equal(TimeSpan.FromSeconds(4),
                node.GetNormalizedHeartbeatInterval(TimeSpan.FromSeconds(4)));
            Assert.Equal(TimeSpan.FromMilliseconds(700),
                node.GetNormalizedPublishingInterval(TimeSpan.FromMilliseconds(700)));
            Assert.Equal(TimeSpan.FromMilliseconds(800),
                node.GetNormalizedSamplingInterval(TimeSpan.FromMilliseconds(800)));
            Assert.Equal(TimeSpan.FromMilliseconds(900),
                node.GetNormalizedCyclicReadMaxAge(TimeSpan.FromMilliseconds(900)));
        }

        [Fact]
        public void TimeSpanConversionHelpersReturnNullWithoutValueOrDefault()
        {
            TimeSpan? timespan = null;

            Assert.Null(timespan.GetTimeSpanFromSeconds(null));
            Assert.Null(timespan.GetTimeSpanFromMiliseconds(null));
        }
    }
}
