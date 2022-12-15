// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Tests.Publisher.Config.Models {

    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using Xunit;

    public class OpcNodeModelExTests {

        [Fact]
        public void ComparerTest()
        {
            var comparer = OpcNodeModelEx.Comparer;

            var opcNode1 = new OpcNodeModel();
            var opcNode2 = new OpcNodeModel();

            Assert.True(comparer.Equals(opcNode1, opcNode2));
            Assert.True(comparer.GetHashCode(opcNode1) == comparer.GetHashCode(opcNode2));

            opcNode1 = new OpcNodeModel {
                Id = "id",
                OpcPublishingInterval = 1500,
                OpcSamplingInterval = 2500,
                HeartbeatInterval = 35,
                QueueSize = 123,
                DataChangeTrigger = DataChangeTriggerType.StatusValue,
                DeadbandType = DeadbandType.Absolute,
                DeadbandValue = 0.1
            };

            static OpcNodeModel NewNode() => new OpcNodeModel {
                Id = "id",
                OpcPublishingIntervalTimespan = TimeSpan.Parse("00:00:01.5"),
                OpcSamplingIntervalTimespan = TimeSpan.Parse("00:00:02.500"),
                HeartbeatIntervalTimespan = TimeSpan.Parse("00:00:35"),
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
    }
}
