// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// A native writer group is one network message on one topic, so a group
    /// whose writers name different topics cannot be represented as one. The
    /// egress used to refuse such a group outright, which meant it published
    /// nothing at all.
    /// </summary>
    public sealed class WriterGroupDestinationSplitterTests
    {
        [Fact]
        public void GroupsThatAgreeArePassedThroughUntouched()
        {
            //
            // Identity matters more than the split here: writer group ids key
            // the durable identity registry, so a group that did not need
            // separating must come back as the same instance with the same id.
            //
            var group = Group("group-1",
                Writer("a", Queue("topic/one")),
                Writer("b", Queue("topic/one")));

            var result = WriterGroupDestinationSplitter.Split([group]);

            Assert.Same(group, Assert.Single(result));
        }

        [Fact]
        public void GroupWithoutWritersIsPassedThroughUntouched()
        {
            var group = Group("group-1");

            var result = WriterGroupDestinationSplitter.Split([group]);

            Assert.Same(group, Assert.Single(result));
        }

        [Fact]
        public void WritersInheritingTheGroupDestinationAgree()
        {
            var group = Group("group-1", Queue("group/topic"),
                Writer("a", null),
                Writer("b", null));

            var result = WriterGroupDestinationSplitter.Split([group]);

            Assert.Same(group, Assert.Single(result));
        }

        [Fact]
        public void WritersWithDifferentTopicsAreSeparated()
        {
            var group = Group("group-1",
                Writer("a", Queue("topic/one")),
                Writer("b", Queue("topic/two")));

            var result = WriterGroupDestinationSplitter.Split([group]);

            Assert.Equal(2, result.Count);
            Assert.All(result, split => Assert.Single(split.DataSetWriters!));
            Assert.Equal(["topic/one", "topic/two"],
                result.Select(split => split.Publishing?.QueueName).Order());
            //
            // Each split group carries its own destination, which is what lets
            // the egress resolve one topic per group instead of refusing.
            //
            Assert.All(result, split => Assert.Equal(
                split.Publishing?.QueueName,
                split.DataSetWriters![0].Publishing?.QueueName));
        }

        [Fact]
        public void WritersSharingATopicStayTogether()
        {
            var group = Group("group-1",
                Writer("a", Queue("topic/one")),
                Writer("b", Queue("topic/two")),
                Writer("c", Queue("topic/one")));

            var result = WriterGroupDestinationSplitter.Split([group]);

            Assert.Equal(2, result.Count);
            Assert.Equal([1, 2], result.Select(s => s.DataSetWriters!.Count).Order());
        }

        [Fact]
        public void DeliveryGuaranteeAloneSeparatesAGroup()
        {
            //
            // The egress rejects a group on any difference it cannot carry,
            // not only on the topic, so the split has to consider the same
            // fields it does.
            //
            var group = Group("group-1",
                Writer("a", Queue("topic/one", QoS.AtMostOnce)),
                Writer("b", Queue("topic/one", QoS.AtLeastOnce)));

            var result = WriterGroupDestinationSplitter.Split([group]);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SplitIdsAreDerivedFromTheDestinationNotTheOrder()
        {
            //
            // Ids derived from a counter would renumber every group when an
            // unrelated writer is added or the list is reordered, and every
            // durable identity in the group would move with them.
            //
            var forward = WriterGroupDestinationSplitter.Split([Group("group-1",
                Writer("a", Queue("topic/one")),
                Writer("b", Queue("topic/two")))]);
            var reversed = WriterGroupDestinationSplitter.Split([Group("group-1",
                Writer("b", Queue("topic/two")),
                Writer("a", Queue("topic/one")))]);

            Assert.Equal(
                forward.Select(g => g.Id).Order(),
                reversed.Select(g => g.Id).Order());
            Assert.All(forward, g => Assert.StartsWith("group-1_", g.Id, System.StringComparison.Ordinal));
            Assert.Equal(2, forward.Select(g => g.Id).Distinct().Count());
        }

        [Fact]
        public void AnUnrelatedGroupIsNotDisturbedBySplittingAnother()
        {
            var untouched = Group("group-2", Writer("x", Queue("topic/x")));
            var split = Group("group-1",
                Writer("a", Queue("topic/one")),
                Writer("b", Queue("topic/two")));

            var result = WriterGroupDestinationSplitter.Split([split, untouched]);

            Assert.Equal(3, result.Count);
            Assert.Contains(untouched, result);
        }

        private static PublishingQueueSettingsModel Queue(string name, QoS? qos = null)
        {
            return new PublishingQueueSettingsModel
            {
                QueueName = name,
                RequestedDeliveryGuarantee = qos
            };
        }

        private static DataSetWriterModel Writer(string id,
            PublishingQueueSettingsModel? publishing)
        {
            return new DataSetWriterModel
            {
                Id = id,
                DataSetWriterName = id,
                Publishing = publishing
            };
        }

        private static WriterGroupModel Group(string id,
            params DataSetWriterModel[] writers)
        {
            return Group(id, null, writers);
        }

        private static WriterGroupModel Group(string id,
            PublishingQueueSettingsModel? publishing,
            params DataSetWriterModel[] writers)
        {
            return new WriterGroupModel
            {
                Id = id,
                Name = id,
                Publishing = publishing,
                DataSetWriters = writers.Length == 0
                    ? null
                    : new List<DataSetWriterModel>(writers)
            };
        }
    }
}
