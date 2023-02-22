// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Storage {
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Test
    ///
    /// The referenced schema file across these test is a linked asset in the
    /// project file set to copy to the output build directory so that it can
    /// be easily referenced here.
    /// </summary>
    public class PublishedNodesJobConverterTests {

        [Fact]
        public void PnPlcEmptyTestAsync() {
            var pn = @"
[
]
";
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var jobs = converter.Read(pn);

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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());
            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSetWriterName);
        }

        [Fact]
        public void PnPlcPubSubDataSetWriterIdIsNullTest() {
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());
            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("<<UnknownDataSet>>_($b6589fc6ab0dc82cf12099d1c2d40ab994e8410c)", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSetWriterName);
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.WriterGroupId);
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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
        public void PnPlcPubSubDataSetFieldIdDuplicateTest() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DataSetFieldId"": ""testfieldid""
            },
            {
                ""Id"": ""i=2259"",
                ""DataSetFieldId"": ""testfieldid""
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testfieldid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("testfieldid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().Id);
        }

        [Fact]
        public void PnPlcPubSubDisplayNameDuplicateTest() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""testdisplayname""
            },
            {
                ""Id"": ""i=2259"",
                ""DisplayName"": ""testdisplayname""
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testdisplayname", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.First().PublishedVariableDisplayName);
            Assert.Equal("testdisplayname", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().PublishedVariableDisplayName);
        }

        [Fact]
        public void PnPlcPubSubFullDuplicateTest() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""testdisplayname"",
                ""DataSetFieldId"": ""testfieldid""
            },
            {
                ""Id"": ""i=2259"",
                ""DisplayName"": ""testdisplayname"",
                ""DataSetFieldId"": ""testfieldid""
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testfieldid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("testfieldid", jobs
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Equal("testfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("i=2258", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.PublishedVariables.PublishedData.First().PublishedVariableNodeId);
            Assert.Null(jobs
                .Single().WriterGroup.DataSetWriters
                .Last().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().Id);
            Assert.Equal("i=2259", jobs
                .Single().WriterGroup.DataSetWriters
                .Last().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().PublishedVariableNodeId);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.WriterGroupId);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.Connection.Group);
            Assert.Equal("testwriterid_($a4ac914c09d7c097fe1f4f96b897e625b6922069)", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSetWriterName);
            Assert.Equal(2000, jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Last().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);

        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingInterval1Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": 1000,
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


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
        ""DataSetPublishingInterval"": 1000,
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

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingInterval3Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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
        ""DataSetPublishingInterval"": 1000,
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""OpcPublishingInterval"": 2000
            },
            {
                ""Id"": ""i=2259""
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2000, jobs
                .First().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingIntervalTimespan1Test() {
            var pn = @"
[
    {
        ""DataSetPublishingIntervalTimespan"": ""00:00:01"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingIntervalTimespan2Test() {
            var pn = @"
[
    {
        ""DataSetPublishingIntervalTimespan"": ""00:00:01"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""OpcPublishingIntervalTimespan"": ""00:00:02""
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingIntervalTimespan3Test() {
            var pn = @"
[
    {
        ""DataSetPublishingIntervalTimespan"": ""00:00:01"",
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public void PnPlcPubSubDataSetPublishingIntervalTimespan4Test() {
            var pn = @"
[
    {
        ""DataSetPublishingIntervalTimespan"": ""00:00:01"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            },
            {
                ""Id"": ""i=2259"",
                ""OpcPublishingInterval"": 3000
            }
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }


        [Fact]
        public void PnPlcPubSubDisplayName1Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public void PnPlcPubSubDisplayName2Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": 1000,
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            },
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Null(jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubDisplayName3Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdatasetfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public void PnPlcPubSubDisplayName4Test() {
            var pn = @"
[
    {
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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
        ""DataSetPublishingInterval"": 1000,
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
            },
        ]
    }
]
";


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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
        ""DataSetPublishingInterval"": 1000,
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(2, j
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(1500, j
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(2000, j.WriterGroup.DataSetWriters.Single()
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Null(j.WriterGroup.DataSetWriters.Single()
                .DataSet.DataSetSource.SubscriptionSettings.PublishingInterval);
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(2000, j
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());


            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", j.WriterGroup.DataSetWriters
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", j.WriterGroup.DataSetWriters
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", j.WriterGroup.DataSetWriters
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(4, j.WriterGroup.DataSetWriters.Count);
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
            var endpointUrls = new string[] {
                "opc.tcp://localhost:50001",
                "opc.tcp://localhost:50002",
                "opc.tcp://localhost:50003",
                "opc.tcp://localhost:50004"
            };



            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(4, j.WriterGroup.DataSetWriters.Count);
            Assert.True(endpointUrls.ToHashSet().SetEqualsSafe(
                j.WriterGroup.DataSetWriters.Select(w => w.DataSet.DataSetSource.Connection.Endpoint.Url)));
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



            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(2, j.WriterGroup.DataSetWriters.Count);
            Assert.True(endpointUrls.ToHashSet().SetEqualsSafe(
                j.WriterGroup.DataSetWriters.Select(w => w.DataSet.DataSetSource.Connection.Endpoint.Url)));
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
                ""OpcPublishingInterval"": 2000,
                ""Id"": ""i=2262""
            },
            {
                ""OpcPublishingInterval"": 2000,
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



            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig());

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(3, j.WriterGroup.DataSetWriters.Count);
            Assert.True(endpointUrls.ToHashSet().SetEqualsSafe(
                j.WriterGroup.DataSetWriters.Select(w => w.DataSet.DataSetSource.Connection.Endpoint.Url)));
        }

        [Theory]
        [InlineData("Publisher/publishednodes_with_duplicates.json")]
        public async Task PnWithDuplicatesTest(string publishedNodesJsonFile) {

            var pn = await File.ReadAllTextAsync(publishedNodesJsonFile);
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(2, j.WriterGroup.DataSetWriters.Count);
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://10.0.0.1:59412",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.Single(j.WriterGroup.DataSetWriters, dataSetWriter => TimeSpan.FromMinutes(15) ==
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings.PublishingInterval);
            Assert.Single(j.WriterGroup.DataSetWriters, dataSetWriter => TimeSpan.FromMinutes(1) ==
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings.PublishingInterval);
            Assert.Single(j.WriterGroup.DataSetWriters, dataSetWriter =>
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData.Any(
                    p => TimeSpan.FromMinutes(15) == p.SamplingInterval));
            Assert.Equal(3, j.WriterGroup.DataSetWriters.SelectMany(dataSetWriter =>
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData).Count());
        }


        [Fact]
        public void PnPlcMultiJobBatching1Test() {

            var pn = new StringBuilder(@"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            ");

            for (var i = 1; i < 10000; i++) {
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


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn.ToString());
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(10, j.WriterGroup.DataSetWriters.Count);
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://localhost:50000",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Null(
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings.PublishingInterval));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.All(
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Null(p.SamplingInterval)));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter =>
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
                pn.Append('\"');
                pn.Append(i % 2 == 1 ? ",\"OpcPublishingInterval\": 2000" : null);
                pn.Append("},");
            }

            pn.Append(@"
            { ""Id"": ""i=10000"" }
        ]
    }
]
");


            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn.ToString());
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(10, j.WriterGroup.DataSetWriters.Count);
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://localhost:50000",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.Equal(
                new TimeSpan?[] {
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    null,
                    null,
                    null,
                    null,
                    null
                }, j.WriterGroup.DataSetWriters.Select(dataSetWriter =>
                    dataSetWriter.DataSet.DataSetSource.SubscriptionSettings?.PublishingInterval).ToList());

            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.All(
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Null(p.SamplingInterval)));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter =>
                Assert.Equal(1000,
                    dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData.Count));
        }

        [Fact]
        public void PnPlcJobWithAllEventPropertiesTest() {
            var pn = new StringBuilder(@"
[
    {
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""Id"": ""i=2258"",
                ""DisplayName"": ""TestingDisplayName"",
                ""EventFilter"": {
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
            }
        ]
    }
]
");
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn.ToString());
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

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
            Assert.Equal("TestingDisplayName", model.PublishedEventName);
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
        public void PnPlcMultiJob1TestWithDataItemsAndEvents() {
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
                ""ExpandedNodeId"": ""nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean"",
                ""DisplayName"": ""AlternatingBoolean""
            },
            {
                ""Id"": ""i=2253"",
                ""OpcPublishingInterval"": 5000,
                ""EventFilter"": {
                    ""SelectClauses"": [
                        {
                            ""TypeDefinitionId"": ""i=2041"",
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
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);


            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

            Assert.NotEmpty(jobs);
            var writers = Assert.Single(jobs).WriterGroup.DataSetWriters;

            Assert.Equal(5, writers.Count);
            Assert.Single(writers[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            var dataItemModel = writers[0].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("i=2258", dataItemModel.PublishedVariableNodeId);
            Assert.Empty(writers[0].DataSet.DataSetSource.PublishedEvents.PublishedData);

            Assert.Single(writers[1].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(writers[1].DataSet.DataSetSource.PublishedEvents.PublishedData);
            dataItemModel = writers[1].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("ns=0;i=2261", dataItemModel.PublishedVariableNodeId);

            Assert.Single(writers[2].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(writers[2].DataSet.DataSetSource.PublishedEvents.PublishedData);
            dataItemModel = writers[2].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("AlternatingBoolean", dataItemModel.PublishedVariableDisplayName);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean", dataItemModel.PublishedVariableNodeId);

            Assert.Empty(writers[3].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.NotEmpty(writers[3].DataSet.DataSetSource.PublishedEvents.PublishedData);
            var eventModel = writers[3].DataSet.DataSetSource.PublishedEvents.PublishedData[0];
            Assert.Equal("i=2253", eventModel.EventNotifier);
            Assert.Single(eventModel.SelectClauses);
            Assert.Equal("i=2041", eventModel.SelectClauses[0].TypeDefinitionId);
            Assert.Single(eventModel.SelectClauses[0].BrowsePath);
            Assert.Equal("EventId", eventModel.SelectClauses[0].BrowsePath[0]);
            Assert.NotNull(eventModel.WhereClause);
            Assert.Single(eventModel.WhereClause.Elements);
            Assert.Equal(FilterOperatorType.OfType, eventModel.WhereClause.Elements[0].FilterOperator);
            Assert.Single(eventModel.WhereClause.Elements[0].FilterOperands);
            Assert.Equal("ns=2;i=235", eventModel.WhereClause.Elements[0].FilterOperands[0].Value);

            Assert.NotEmpty(writers[4].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Empty(writers[4].DataSet.DataSetSource.PublishedEvents.PublishedData);
            dataItemModel = writers[4].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("i=2262", dataItemModel.PublishedVariableNodeId);
            dataItemModel = writers[4].DataSet.DataSetSource.PublishedVariables.PublishedData[1];
            Assert.Equal("ns=2;s=DipData", dataItemModel.PublishedVariableNodeId);
            dataItemModel = writers[4].DataSet.DataSetSource.PublishedVariables.PublishedData[2];
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=NegativeTrendData", dataItemModel.PublishedVariableNodeId);
        }

        [Fact]
        public void PnPlcJobTestWithEvents() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://desktop-fhd2fr4:62563/Quickstarts/SimpleEventsServer"",
        ""UseSecurity"": false,
        ""OpcNodes"": [
            {
                ""Id"": ""i=2253"",
                ""DisplayName"": ""DisplayName2253"",
                ""OpcPublishingInterval"": 5000,
                ""EventFilter"": {
                    ""SelectClauses"": [
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""EventId""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""EventType""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""SourceNode""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""SourceName""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""Time""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""ReceiveTime""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""LocalTime""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""Message""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""Severity""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
                            ""BrowsePath"": [
                                ""2:CycleId""
                            ]
                        },
                        {
                            ""TypeDefinitionId"": ""i=2041"",
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
            }
        ]
    }
]
";
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters);
            Assert.Empty(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);

            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter =>
               Assert.Single(dataSetWriter.DataSet.DataSetSource.PublishedEvents.PublishedData));

            var eventModel = jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData[0];
            Assert.Equal("DisplayName2253", eventModel.PublishedEventName);
            Assert.Equal("i=2253", eventModel.EventNotifier);
            Assert.Equal(11, eventModel.SelectClauses.Count);
            Assert.All(eventModel.SelectClauses, x => {
                Assert.Equal("i=2041", x.TypeDefinitionId);
                Assert.Single(x.BrowsePath);
            });
            Assert.Equal(new[] {
                "EventId",
                "EventType",
                "SourceNode",
                "SourceName",
                "Time",
                "ReceiveTime",
                "LocalTime",
                "Message",
                "Severity",
                "2:CycleId",
                "2:CurrentStep"
            }, eventModel.SelectClauses.Select(x => x.BrowsePath[0]));
        }

        [Fact]
        public void PnPlcJobTestWithConditionHandling() {
            var pn = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://desktop-fhd2fr4:62563/Quickstarts/SimpleEventsServer"",
        ""UseSecurity"": false,
        ""OpcNodes"": [
            {
                ""Id"": ""i=2253"",
                ""OpcPublishingInterval"": 5000,
                ""EventFilter"": {
                    ""TypeDefinitionId"": ""ns=2;i=235""
                },
                ""ConditionHandling"": {
                    ""UpdateInterval"": 10,
                    ""SnapshotInterval"": 30
                }
            }
        ]
    }
]
";
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = Log.Console<PublishedNodesJobConverter>();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var entries = converter.Read(pn);
            var jobs = converter.ToWriterGroupJobs(entries, GetConfig()).ToList();

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters);
            Assert.Empty(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);

            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter =>
               Assert.Single(dataSetWriter.DataSet.DataSetSource.PublishedEvents.PublishedData));

            var eventModel = jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData[0];
            Assert.Equal("i=2253", eventModel.EventNotifier);
            Assert.Equal("ns=2;i=235", eventModel.TypeDefinitionId);
            Assert.NotNull(eventModel.ConditionHandling);
            Assert.Equal(10, eventModel.ConditionHandling.UpdateInterval);
            Assert.Equal(30, eventModel.ConditionHandling.SnapshotInterval);
        }


        private static IPublisherConfiguration GetConfig() {
            var configMock = new Mock<IPublisherConfiguration>();
            configMock.SetupAllProperties();
            configMock.SetupGet(p => p.MaxNodesPerPublishedEndpoint).Returns(1000);
            configMock.SetupGet(p => p.MessagingProfile).Returns(MessagingProfile.Get(
                MessagingMode.Samples, MessageEncoding.Json));
            var config = configMock.Object;
            return config;
        }

        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
    }
}
