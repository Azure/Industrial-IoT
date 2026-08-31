// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Storage
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Core.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Server side triggering, from published nodes configuration through to
    /// the monitored item template the subscription is built from.
    /// </summary>
    /// <remarks>
    /// This layer had no coverage, which is how the configuration came to be
    /// discarded without anything noticing: the subscription adapter's own
    /// triggering tests construct <c>TriggeredItems</c> directly and never
    /// reach the converter, so they passed either way.
    /// </remarks>
    public class PublishedNodesTriggeringTests
    {
        [Fact]
        public void TriggeredNodesReachTheMonitoredItemTemplate()
        {
            const string pn = """
            [
                {
                    "EndpointUrl": "opc.tcp://localhost:50000",
                    "DataSetWriterGroup": "group",
                    "DataSetWriterId": "writer",
                    "OpcNodes": [
                        {
                            "Id": "i=2258",
                            "DataSetFieldId": "trigger",
                            "TriggeredNodes": [
                                { "Id": "i=2259", "DataSetFieldId": "reported-a" },
                                { "Id": "i=2260", "DataSetFieldId": "reported-b" }
                            ]
                        }
                    ]
                }
            ]
            """;

            var items = ToMonitoredItems(pn);

            var trigger = Assert.Single(items);
            Assert.Equal("trigger", ((DataMonitoredItemModel)trigger).DataSetFieldId);
            Assert.NotNull(trigger.TriggeredItems);
            Assert.Equal(["reported-a", "reported-b"],
                trigger.TriggeredItems!
                    .Cast<DataMonitoredItemModel>()
                    .Select(item => item.DataSetFieldId)
                    .Order());
        }

        [Fact]
        public void ANodeWithoutTriggeredNodesHasNoTriggeredItems()
        {
            const string pn = """
            [
                {
                    "EndpointUrl": "opc.tcp://localhost:50000",
                    "DataSetWriterGroup": "group",
                    "DataSetWriterId": "writer",
                    "OpcNodes": [ { "Id": "i=2258", "DataSetFieldId": "plain" } ]
                }
            ]
            """;

            var items = ToMonitoredItems(pn);

            Assert.Null(Assert.Single(items).TriggeredItems);
        }

        [Fact]
        public void TriggeringDoesNotRecurse()
        {
            //
            // OPC UA triggering is one level - Part 4 has a triggering item
            // and the items it reports. A triggered item that carried its own
            // triggers would previously have built them, which is the same
            // inverted flag that discarded the configured ones.
            //
            const string pn = """
            [
                {
                    "EndpointUrl": "opc.tcp://localhost:50000",
                    "DataSetWriterGroup": "group",
                    "DataSetWriterId": "writer",
                    "OpcNodes": [
                        {
                            "Id": "i=2258",
                            "DataSetFieldId": "trigger",
                            "TriggeredNodes": [
                                {
                                    "Id": "i=2259",
                                    "DataSetFieldId": "reported",
                                    "TriggeredNodes": [
                                        { "Id": "i=2260", "DataSetFieldId": "nested" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
            """;

            var items = ToMonitoredItems(pn);

            var trigger = Assert.Single(items);
            var reported = Assert.Single(trigger.TriggeredItems!);
            Assert.Null(reported.TriggeredItems);
        }

        [Fact]
        public void ATriggeredNodeIsSampledRatherThanReported()
        {
            //
            // Part 4 5.12.1.6 only reports a triggered item on its trigger
            // while it is sampling. A reporting item publishes whenever it
            // changes, so the link would change nothing and the documented
            // contract - report this node when its parent changes - would be
            // false.
            //
            const string pn = """
            [
                {
                    "EndpointUrl": "opc.tcp://localhost:50000",
                    "DataSetWriterGroup": "group",
                    "DataSetWriterId": "writer",
                    "OpcNodes": [
                        {
                            "Id": "i=2258",
                            "DataSetFieldId": "trigger",
                            "TriggeredNodes": [
                                { "Id": "i=2259", "DataSetFieldId": "reported" }
                            ]
                        }
                    ]
                }
            ]
            """;

            var items = ToMonitoredItems(pn);

            var trigger = Assert.Single(items);
            //
            // The trigger names no mode, so it takes the subscription default,
            // which is Reporting.
            //
            Assert.Null(trigger.MonitoringMode);
            var reported = Assert.Single(trigger.TriggeredItems!);
            Assert.Equal(MonitoringMode.Sampling, reported.MonitoringMode);
        }

        private static IReadOnlyList<BaseMonitoredItemModel> ToMonitoredItems(string pn)
        {
            var logger = Log.Console<PublishedNodesConverter>();
            var converter = new PublishedNodesConverter(logger, GetOptions(), null);

            var group = Assert.Single(converter.ToWriterGroups(converter.Read(pn)));
            var writer = Assert.Single(group.DataSetWriters!);
            return writer.DataSet!.DataSetSource!.ToMonitoredItems(NamespaceFormat.Uri);
        }

        private static IOptions<PublisherOptions> GetOptions()
        {
            return new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
        }
    }
}
