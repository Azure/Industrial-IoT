// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;
    using Microsoft.Azure.IIoT.Module;

    /// <summary>
    /// Test
    /// </summary>
    public class PublishedNodesJobConverterTests {

        private class StandaloneIdentity : IIdentity {
            public string DeviceId => "StandaloneDeviceId";
            public string ModuleId => "StandaloneModuleId";
            public string SiteId => "StandaloneSiteId";
        }

        [Fact]
        public void PnPlcEmptyTest() {
            var pn = @"
[
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.Empty(jobs);
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
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
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [

            {
                ""OpcPublishingInterval"": 1000,
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean""
            }
        ]
    },
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""OpcPublishingInterval"": 2000,
                ""Id"": ""i=2262""
            },
            {
                ""OpcPublishingInterval"": 3000,
                ""Id"": ""ns=2;s=DipData""
            },
            {
                ""Id"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData""
            }
        ]
    }
]
";
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
            var jobs = converter.Read(new StringReader(pn), new LegacyCliModel());

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(4, jobs.Count());
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
            Assert.All(jobs, j => Assert.Equal("opc.tcp://localhost:50000",
                j.WriterGroup.DataSetWriters
                    .Single().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Fact]
        public void PnPlcMultiJobBatchingTest() {

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
            var converter = new PublishedNodesJobConverter(TraceLogger.Create(),
                _serializer, new StandaloneIdentity());
            var jobs = converter.Read(new StringReader(pn.ToString()), new LegacyCliModel()).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(10, jobs.Count());
            Assert.All(jobs, j => Assert.Equal(MessagingMode.Samples, j.MessagingMode));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
            Assert.All(jobs, j => Assert.Equal("opc.tcp://localhost:50000",
                j.WriterGroup.DataSetWriters
                    .Single().DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.All(jobs, j => Assert.Null(
                j.WriterGroup.DataSetWriters
                    .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval));
            Assert.All(jobs, j => Assert.All(
                j.WriterGroup.DataSetWriters
                    .Single().DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Null(p.SamplingInterval)));
            Assert.All(jobs, j =>
                Assert.Equal(1000, j.WriterGroup.DataSetWriters
                    .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count));
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
