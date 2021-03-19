// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Test
    /// </summary>
    public class PublishedNodesJobConverterTests {

        private class StandaloneIdentity : IIdentity {
            public string Gateway => Utils.GetHostName();
            public string DeviceId => Gateway;
            public string ModuleId => "standaloneModule";
            public string SiteId => null;
        }

        [Fact]
        public void PnPlcEmptyTest() {
            var pn = @"
[
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.Empty(jobs);
        }

        [Fact]
        public void PnPlcPubSubDataSetWriterIdTest() {
            var pn = @"
[
    {
        ""DataSetWriterId"": ""testid"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""HeartbeatInterval"": 2
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Id);
        }

        [Fact]
        public void PnPlcPubSubDataSetWriterGroupTest() {
            var pn = @"
[
    {
        ""DataSetWriterGroup"": ""testgroup"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""HeartbeatInterval"": 2
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Group);
        }

        [Fact]
        public void PnPlcPubSubDataSetFieldId1Test() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DataSetFieldId"": ""testfieldid1""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubDataSetFieldId2Test() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DataSetFieldId"": ""testfieldid1""
            },
            {
                ""Id"": ""i=2259"",
                ""DataSetFieldId"": ""testfieldid2""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("testfieldid2", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().Id);
        }

        [Fact]
        public void PnPlcPubSubFullTest() {
            var pn = @"
[
    {
        ""DataSetWriterGroup"": ""testgroup"",
        ""DataSetWriterId"": ""testwriterid"",
        ""DataSetPublishingInterval"": 1000,
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DataSetFieldId"": ""testfieldid1"",
                ""OpcPublishingInterval"": 2000
            },
            {
                ""Id"": ""i=2259"",
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromSeconds(5) } );

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("i=2259", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().Id);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Group);
            Assert.Equal("testwriterid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Id);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingInterval1Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingInterval2Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""OpcPublishingInterval"": 2000
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingInterval3Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258""
            },
            {
                ""Id"": ""i=2259""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingInterval4Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""OpcPublishingInterval"": 2000
            },
            {
                ""Id"": ""i=2259"",
                ""OpcPublishingInterval"": 3000
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDisplayName1Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""testdisplayname1""
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubDisplayName2Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("i=2258", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubDisplayName3Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""testdisplayname1"",
                ""DataSetFieldId"": ""testdatasetfieldid1"",
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubDisplayName4Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DataSetFieldId"": ""testdatasetfieldid1"",
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdatasetfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubPublishedNodeDisplayName1Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""testdisplayname1""
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public void PnPlcPubSubPublishedNodeDisplayName2Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Null(jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public void PnPlcPubSubPublishedNodeDisplayName3Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""testdisplayname1"",
                ""DataSetFieldId"": ""testdatasetfieldid1"",
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public void PnPlcPubSubPublishedNodeDisplayName4Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": ""1000"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DataSetFieldId"": ""testdatasetfieldid1"",
            },
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Null(jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public void PnPlcHeartbeatInterval2Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""HeartbeatInterval"": 2
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(2, jobs.Single()
                .WriterGroup.DataSetWriters.Single()
                .DataSet.DataSetSource.PublishedVariables.PublishedData.Single()
                .HeartbeatInterval.Value.TotalSeconds);
        }

        [Fact]
        public void PnPlcHeartbeatIntervalTimespanTest() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""HeartbeatIntervalTimespan"": ""00:00:01.500""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(1500, jobs.Single()
                .WriterGroup.DataSetWriters.Single()
                .DataSet.DataSetSource.PublishedVariables.PublishedData.Single()
                .HeartbeatInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcHeartbeatSkipSingleTrueTest() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""SkipSingle"": true
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }


        [Fact]
        public void PnPlcHeartbeatSkipSingleFalseTest() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""SkipSingle"": false
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }

        [Fact]
        public void PnPlcPublishingInterval2000Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""OpcPublishingInterval"": 2000
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(2000, jobs.Single().WriterGroup.DataSetWriters.Single()
                .DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPublishingIntervalCliTest() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel() { DefaultPublishingInterval = TimeSpan.FromSeconds(10) });

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(10000, jobs.Single().WriterGroup.DataSetWriters.Single()
                .DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcSamplingInterval2000Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""OpcSamplingInterval"": 2000
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(2000, jobs.Single()
             .WriterGroup.DataSetWriters.Single()
             .DataSet.DataSetSource.PublishedVariables.PublishedData.Single()
             .SamplingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcExpandedNodeIdTest() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""ExpandedNodeId"": ""nsu=http://opcfoundation.org/UA/;i=2258""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }


        [Fact]
        public void PnPlcExpandedNodeId2Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""ExpandedNodeId"": ""nsu=http://opcfoundation.org/UA/;i=2258""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2262""
            },
            {
                ""Id"": ""ns=2;s=AlternatingBoolean""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }


        [Fact]
        public void PnPlcExpandedNodeId3Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258""
            },
            {
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Single(jobs
              .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }


        [Fact]
        public void PnPlcExpandedNodeId4Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""NodeId"": {
                ""Identifier"": ""i=2258""
        }
        },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""NodeId"": {
            ""Identifier"": ""ns=0;i=2261""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [

            {
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2262""
            },
            {
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }

        [Fact]
        public void PnPlcMultiJob1Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost1:50000"",
        ""NodeId"": {
                ""Identifier"": ""i=2258""
        }
        },
    {
        ""EndpointUrl"": ""opc.tcp://localhost2:50000"",
        ""NodeId"": {
            ""Identifier"": ""ns=0;i=2261""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost3:50000"",
        ""OpcNodes"": [

            {
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost4:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2262""
            },
            {
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(4, jobs.Count());
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
        }

        [Fact]
        public void PnPlcMultiJob2Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50001"",
        ""NodeId"": {
                ""Identifier"": ""i=2258"",
        }
        },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50002"",
        ""NodeId"": {
            ""Identifier"": ""ns=0;i=2261""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50003"",
        ""OpcNodes"": [
            {
                ""OpcPublishingInterval"": 1000,
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50004"",
        ""OpcNodes"": [
            {
                ""OpcPublishingInterval"": 1000,
                ""Id"": ""i=2262""
            },
            {
                ""OpcPublishingInterval"": 1000,
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData"",
                ""OpcPublishingInterval"": 1000
            }
        ]
    }
]
";
            var endpointUrls = new string [] { 
                "opc.tcp://localhost:50001", 
                "opc.tcp://localhost:50002",
                "opc.tcp://localhost:50003",
                "opc.tcp://localhost:50004"
            };

            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(4, jobs.Count());
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
            Assert.Equal(endpointUrls,
                jobs.Select(job => job.WriterGroup.DataSetWriters
                    .First().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Fact]
        public void PnPlcMultiJob3Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""NodeId"": {
                ""Identifier"": ""i=2258"",
        }
        },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""NodeId"": {
            ""Identifier"": ""ns=0;i=2261""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50001"",
        ""OpcNodes"": [
            {
                ""OpcPublishingInterval"": 1000,
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50001"",
        ""OpcNodes"": [
            {
                ""OpcPublishingInterval"": 1000,
                ""Id"": ""i=2262""
            },
            {
                ""OpcPublishingInterval"": 1000,
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData"",
                ""OpcPublishingInterval"": 1000
            }
        ]
    }
]
";
            var endpointUrls = new string[] {
                "opc.tcp://localhost:50000",
                "opc.tcp://localhost:50001",
            };

            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(2, jobs.Count());
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
            Assert.Equal(endpointUrls,
                jobs.Select(job => job.WriterGroup.DataSetWriters
                    .First().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Fact]
        public void PnPlcMultiJob4Test() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""NodeId"": {
                ""Identifier"": ""i=2258"",
        }
        },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""NodeId"": {
            ""Identifier"": ""ns=0;i=2261""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50001"",
        ""OpcNodes"": [
            {
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50001"",
        ""OpcNodes"": [
            {
                ""OpcPublishingInterval"": 1000,
                ""Id"": ""i=2262""
            },
            {
                ""OpcPublishingInterval"": 1000,
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData"",
            }
        ]
    }
]
";
            var endpointUrls = new string[] {
                "opc.tcp://localhost:50000",
                "opc.tcp://localhost:50001",
            };

            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(2, jobs.Count());
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            var enumerator = jobs.GetEnumerator();
            enumerator.MoveNext();
            Assert.Single(enumerator.Current.WriterGroup.DataSetWriters);
            enumerator.MoveNext();
            Assert.Equal(2, enumerator.Current.WriterGroup.DataSetWriters.Count());
            Assert.Equal(endpointUrls,
                jobs.Select(job => job.WriterGroup.DataSetWriters
                    .First().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Fact]
        public void PnPlcMultiJobBatching1Test() {

            var pn = new StringBuilder(@"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            ");

            for(var i = 1; i < 10000; i++) {
                pn.Append("{ \"Id\": \"i=");
                pn.Append(i);
                pn.Append("\" },");
            }

            pn.Append(@"
            { ""Id"": ""i=10000"" }
        ]
    }
]
");
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn.ToString()), new LegacyCliModel()).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal(10, jobs.Single().WriterGroup.DataSetWriters.Count());
            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://localhost:50000",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter => Assert.Null(
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings.PublishingInterval));
            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter => Assert.All(
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Null(p.SamplingInterval)));
            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter =>
                Assert.Equal(1000, 
                    dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData.Count));
        }

        [Fact]
        public void PnPlcMultiJobBatching2Test() {

            var pn = new StringBuilder(@"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            ");

            for (var i = 1; i < 10000; i++) {
                pn.Append("{ \"Id\": \"i=");
                pn.Append(i);
                pn.Append("\"");
                pn.Append(i % 2 == 1 ? ",\"OpcPublishingInterval\": 1000" : null);
                pn.Append("},");
            }

            pn.Append(@"
            { ""Id"": ""i=10000"" }
        ]
    }
]
");
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn.ToString()), new LegacyCliModel()).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal(10, jobs.Single().WriterGroup.DataSetWriters.Count());
            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://localhost:50000",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.Equal(jobs.Single().WriterGroup.DataSetWriters.Select(dataSetWriter => 
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings?.PublishingInterval).ToList(),
                new TimeSpan?[] { 
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    null, null, null, null, null});

            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter => Assert.All(
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Null(p.SamplingInterval)));
            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter =>
                Assert.Equal(1000,
                    dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData.Count));
        }

        [Fact]
        public void PnPlcJobWithAllEventPropertiesTest() {
            var pn = new StringBuilder(@"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcEvents"": [
            {
                ""Id"": ""i=2258"",
                ""SelectClauses"": [
                    {
                        ""TypeDefinitionId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""EventId""
                        ],
                        ""AttributeId"": ""BrowseName"",
                        ""IndexRange"": ""5:20""
                    }
                ],
                ""WhereClause"": {
                    ""Elements"": [
                        {
                            ""FilterOperator"": ""OfType"",
                            ""FilterOperands"": [
                                {
                                    ""NodeId"": ""i=2041"",
                                    ""BrowsePath"": [
                                        ""EventId""
                                    ],
                                    ""AttributeId"": ""BrowseName"",
                                    ""Value"": ""ns=2;i=235"",
                                    ""IndexRange"": ""5:20"",
                                    ""Index"": 10,
                                    ""Alias"": ""Test"",
                                }
                            ]
                        }
                    ]
                }
            }
        ]
    }
]
");

            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn.ToString()), new LegacyCliModel()).ToList();

            // Check jobs
            Assert.Single(jobs);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);
            Assert.NotNull(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables);
            Assert.NotNull(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents);
            Assert.Empty(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);

            // Check model
            var model = jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData[0];
            Assert.Equal("i=2258", model.Id);
            Assert.Equal("i=2258", model.EventNotifier);

            // Check select clauses
            Assert.Single(model.SelectClauses);
            Assert.Equal("i=2041", model.SelectClauses[0].TypeDefinitionId);
            Assert.Single(model.SelectClauses[0].BrowsePath);
            Assert.Equal("EventId", model.SelectClauses[0].BrowsePath[0]);
            Assert.Equal(NodeAttribute.BrowseName, model.SelectClauses[0].AttributeId.Value);
            Assert.Equal("5:20", model.SelectClauses[0].IndexRange);
            Assert.NotNull(model.WhereClause);
            Assert.Single(model.WhereClause.Elements);
            Assert.Equal(FilterOperatorType.OfType, model.WhereClause.Elements[0].FilterOperator);
            Assert.Single(model.WhereClause.Elements[0].FilterOperands);
            Assert.Equal("i=2041", model.WhereClause.Elements[0].FilterOperands[0].NodeId);
            Assert.Equal("ns=2;i=235", model.WhereClause.Elements[0].FilterOperands[0].Value);

            // Check where clause
            Assert.Single(model.WhereClause.Elements[0].FilterOperands[0].BrowsePath);
            Assert.Equal("EventId", model.WhereClause.Elements[0].FilterOperands[0].BrowsePath[0]);
            Assert.Equal(NodeAttribute.BrowseName, model.WhereClause.Elements[0].FilterOperands[0].AttributeId.Value);
            Assert.Equal("5:20", model.WhereClause.Elements[0].FilterOperands[0].IndexRange);
            Assert.NotNull(model.WhereClause.Elements[0].FilterOperands[0].Index);
            Assert.Equal((uint)10, model.WhereClause.Elements[0].FilterOperands[0].Index.Value);
            Assert.Equal("Test", model.WhereClause.Elements[0].FilterOperands[0].Alias);
        }

        [Fact]
        public void PnPlcMultiJob1TestWithEvents() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost1:50000"",
        ""NodeId"": {
            ""Identifier"": ""i=2258""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost2:50000"",
        ""NodeId"": {
            ""Identifier"": ""ns=0;i=2261""
        }
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost3:50000"",
        ""OpcNodes"": [
            {
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ],
        ""OpcEvents"": [
            {
                ""Id"": ""i=2253"",
                ""OpcSamplingInterval"": 2000,
                ""OpcPublishingInterval"": 5000,
                ""Heartbeat"": 3600,
                ""SkipFirst"": true,
                ""SelectClauses"": [
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""EventId""
                        ]
                    }
                ],
                ""WhereClause"": {
                    ""Elements"": [
                        {
                            ""FilterOperator"": ""OfType"",
                            ""FilterOperands"": [
                                {
                                    ""Value"": ""ns=2;i=235""
                                }
                            ]
                        }
                    ]
                }
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost4:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2262""
            },
            {
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel()).ToList();

            Assert.Equal(4, jobs.Count);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters);
            Assert.Single(jobs[1].WriterGroup.DataSetWriters);
            Assert.Equal(2, jobs[2].WriterGroup.DataSetWriters.Count);
            Assert.Single(jobs[3].WriterGroup.DataSetWriters);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);
            Assert.Single(jobs[1].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(jobs[1].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);
            Assert.Single(jobs[2].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(jobs[2].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);
            Assert.Empty(jobs[2].WriterGroup.DataSetWriters[1].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Single(jobs[2].WriterGroup.DataSetWriters[1].DataSet.DataSetSource.PublishedEvents.PublishedData);
            Assert.NotEmpty(jobs[3].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(jobs[3].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);
        }

        [Fact]
        public void PnPlcJobTestWithEvents() {

            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://desktop-fhd2fr4:62563/Quickstarts/SimpleEventsServer"",
        ""UseSecurity"": false,
        ""OpcEvents"": [
            {
                ""Id"": ""i=2253"",
                ""OpcSamplingInterval"": 2000,
                ""OpcPublishingInterval"": 5000,
                ""Heartbeat"": 3600,
                ""SkipFirst"": true,
                ""SelectClauses"": [
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""EventId""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""EventType""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""SourceNode""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""SourceName""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""Time""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""ReceiveTime""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""LocalTime""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""Message""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""Severity""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""2:CycleId""
                        ]
                    },
                    {
                        ""TypeId"": ""i=2041"",
                        ""BrowsePath"": [
                            ""2:CurrentStep""
                        ]
                    }
                ],
                ""WhereClause"": {
                    ""Elements"": [
                        {
                            ""FilterOperator"": ""OfType"",
                            ""FilterOperands"": [
                                {
                                    ""Value"": ""ns=2;i=235""
                                }
                            ]
                        }
                    ]
                }
            }
        ]
    }
]
";

            var converter = new PublishedNodesJobConverter(TraceLogger.Create(), _serializer);
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);

            Assert.All(jobs.Single().WriterGroup.DataSetWriters, dataSetWriter =>
               Assert.Single(dataSetWriter.DataSet.DataSetSource.PublishedEvents.PublishedData));
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
