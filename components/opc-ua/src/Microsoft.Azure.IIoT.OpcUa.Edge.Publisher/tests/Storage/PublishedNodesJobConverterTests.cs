// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Storage {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Moq;
    using Opc.Ua;
    using System;
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

        private class StandaloneIdentity : IIdentity {
            public string Gateway => Utils.GetHostName();
            public string DeviceId => Gateway;
            public string ModuleId => "standaloneModule";
            public string SiteId => null;
        }

        [Fact]
        public async Task PnPlcSchemaValidationFailureTest() {
            var pn = @"
[
    {
        ""DataSetWriterId"": ""testid"",
        ""EndpointUrl"": ""opc.tcp://localhost:50000"",
        ""OpcNodes"": [
            {
                ""HeartbeatInterval"": 2,
                ""OpcSamplingInterval"": 2000,
                ""OpcPublishingInterval"": 2000
            }
        ]
    }
]
";
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            // Verify correct exception is thrown.
            var exception = await Assert.ThrowsAsync<IIoT.Exceptions.SerializerException>(async () =>
            converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync())));

            // Verify correct message is provided in exception.
            Assert.Equal(
                "Validation failed with error: Expected 1 matching subschema but found 0 at schema path: #/items/oneOf, and configuration file location #/0; " +
                "Validation failed with error: Required properties [\"Id\"] were not present at schema path: #/items/oneOf/0/properties/OpcNodes/items/required, and configuration file location #/0/OpcNodes/0; " +
                "Validation failed with error: Required properties [\"ExpandedNodeId\"] were not present at schema path: #/items/oneOf/1/properties/OpcNodes/items/required, and configuration file location #/0/OpcNodes/0; " +
                "Validation failed with error: Required properties [\"NodeId\"] were not present at schema path: #/items/oneOf/2/required, and configuration file location #/0",
                exception.Message);
        }

        [Fact]
        public async Task PnPlcEmptyTestAsync() {
            var pn = @"
[
]
";
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var jobs = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));

            // No jobs
            Assert.Empty(jobs);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetWriterIdTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);
            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSetWriterName);
            Assert.Equal("testid", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Id);

        }

        [Fact]
        public async Task PnPlcPubSubDataSetWriterIdIsNullTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);
            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("1000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSetWriterName);
            Assert.Equal("1000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Id);

        }

        [Fact]
        public async Task PnPlcPubSubDataSetWriterGroupTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.WriterGroupId);
            Assert.Equal("testgroup", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Group);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetFieldId1Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetFieldId2Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcPubSubDataSetFieldIdDuplicateTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcPubSubDisplayNameDuplicateTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testdisplayname", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("testdisplayname", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Last().Id);
        }

        [Fact]
        public async Task PnPlcPubSubFullDuplicateTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcPubSubFullTest() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromSeconds(5)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1, jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.PublishedVariables.PublishedData.Count);
            Assert.Equal("testfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.PublishedVariables.PublishedData.First().Id);
            Assert.Equal("i=2258", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.PublishedVariables.PublishedData.First().PublishedVariableNodeId);
            Assert.Equal(null, jobs
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
            Assert.Equal("testwriterid_2000", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSetWriterName);
            Assert.Equal("testwriterid_2000", jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.Connection.Id);
            Assert.Equal(2000, jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Last().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);

        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingInterval1Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingInterval2Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingInterval3Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingInterval4Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2000, jobs
                .First().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingIntervalTimespan1Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingIntervalTimespan2Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(2000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingIntervalTimespan3Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcPubSubDataSetPublishingIntervalTimespan4Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(1000, jobs
                .Single().WriterGroup.DataSetWriters
                .First().DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }


        [Fact]
        public async Task PnPlcPubSubDisplayName1Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public async Task PnPlcPubSubDisplayName2Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal(null, jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public async Task PnPlcPubSubDisplayName3Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdatasetfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public async Task PnPlcPubSubDisplayName4Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdatasetfieldid1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().Id);
        }

        [Fact]
        public async Task PnPlcPubSubPublishedNodeDisplayName1Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public async Task PnPlcPubSubPublishedNodeDisplayName2Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Null(jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public async Task PnPlcPubSubPublishedNodeDisplayName3Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Equal("testdisplayname1", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public async Task PnPlcPubSubPublishedNodeDisplayName4Test() {
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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromMilliseconds(2000)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Null(jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.PublishedVariables.PublishedData.Single().PublishedVariableDisplayName);
        }

        [Fact]
        public async Task PnPlcHeartbeatInterval2Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcHeartbeatIntervalTimespanTest() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcHeartbeatSkipSingleTrueTest() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcHeartbeatSkipSingleFalseTest() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            Assert.Single(jobs);
            Assert.Single(jobs
                .Single().WriterGroup.DataSetWriters);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
        }

        [Fact]
        public async Task PnPlcPublishingInterval2000Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcPublishingIntervalCliTest() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel() {
                DefaultPublishingInterval = TimeSpan.FromSeconds(10)
            };
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(j.WriterGroup.DataSetWriters);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal("opc.tcp://localhost:50000", jobs
                .Single().WriterGroup.DataSetWriters
                .Single().DataSet.DataSetSource.Connection.Endpoint.Url);
            Assert.Equal(10000, j.WriterGroup.DataSetWriters.Single()
                .DataSet.DataSetSource.SubscriptionSettings.PublishingInterval.Value.TotalMilliseconds);
        }

        [Fact]
        public async Task PnPlcSamplingInterval2000Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


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
        public async Task PnPlcExpandedNodeIdTest() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


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
        public async Task PnPlcExpandedNodeId2Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);


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
        public async Task PnPlcExpandedNodeId3Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcExpandedNodeId4Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

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
        public async Task PnPlcMultiJob1Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(4, jobs.Count());
            Assert.All(jobs, j => Assert.Null(j.MessagingMode));
            Assert.All(jobs, j => Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
        }

        [Fact]
        public async Task PnPlcMultiJob2Test() {

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

            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(4, jobs.Count());
            Assert.All(jobs, j => Assert.Null(j.MessagingMode));
            Assert.All(jobs, j => Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
            Assert.Equal(endpointUrls,
                jobs.Select(job => job.WriterGroup.DataSetWriters
                    .First().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Fact]
        public async Task PnPlcMultiJob3Test() {

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

            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(2, jobs.Count());
            Assert.All(jobs, j => Assert.Null(j.MessagingMode));
            Assert.All(jobs, j => Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.All(jobs, j => Assert.Single(j.WriterGroup.DataSetWriters));
            Assert.Equal(endpointUrls,
                jobs.Select(job => job.WriterGroup.DataSetWriters
                    .First().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Fact]
        public async Task PnPlcMultiJob4Test() {

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

            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli);

            // No jobs
            Assert.NotEmpty(jobs);
            Assert.Equal(2, jobs.Count());
            Assert.All(jobs, j => Assert.Null(j.MessagingMode));
            Assert.All(jobs, j => Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0));
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            var enumerator = jobs.GetEnumerator();
            enumerator.MoveNext();
            Assert.Single(enumerator.Current.WriterGroup.DataSetWriters);
            enumerator.MoveNext();
            Assert.Equal(2, enumerator.Current.WriterGroup.DataSetWriters.Count);
            Assert.Equal(endpointUrls,
                jobs.Select(job => job.WriterGroup.DataSetWriters
                    .First().DataSet.DataSetSource.Connection.Endpoint.Url));
        }

        [Theory]
        [InlineData("Engine/publishednodes_with_duplicates.json")]
        public async Task PnWithDuplicatesTest(string publishedNodesJsonFile) {

            var pn = await File.ReadAllTextAsync(publishedNodesJsonFile);
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, null);
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

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
        public async Task PnPlcMultiJobBatching1Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn.ToString(), new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.All(jobs, j => Assert.Null(j.ConnectionString));
            Assert.Equal(10, j.WriterGroup.DataSetWriters.Count);
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://localhost:50000",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal(TimeSpan.FromSeconds(1),
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings.PublishingInterval));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.All(
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Equal(TimeSpan.FromSeconds(1), p.SamplingInterval)));
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter =>
                Assert.Equal(1000,
                    dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData.Count));
        }

        [Fact]
        public async Task PnPlcMultiJobBatching2Test() {

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
            using var schemaReader = new StreamReader("Storage/publishednodesschema.json");

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn.ToString(), new StringReader(await schemaReader.ReadToEndAsync()));
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

            // No jobs
            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Null(j.MessagingMode);
            Assert.True((j.WriterGroup.MessageSettings.NetworkMessageContentMask & NetworkMessageContentMask.MonitoredItemMessage) != 0);
            Assert.Null(j.ConnectionString);
            Assert.Equal(10, j.WriterGroup.DataSetWriters.Count);
            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.Equal("opc.tcp://localhost:50000",
                dataSetWriter.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.Equal(j.WriterGroup.DataSetWriters.Select(dataSetWriter =>
                dataSetWriter.DataSet.DataSetSource.SubscriptionSettings?.PublishingInterval).ToList(),
                new TimeSpan?[] {
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000)
                });

            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter => Assert.All(
                dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData,
                    p => Assert.Equal(TimeSpan.FromSeconds(1), p.SamplingInterval)));
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

            // ToDo: Add definition for events in schema validator.
            //using var schemaReader = new StreamReader("Storage/publishednodesschema.json");
            //var publishedNodesSchemaContent = await schemaReader.ReadToEndAsync().ConfigureAwait(false);
            //using var publishedNodesSchemaFile = new StringReader(publishedNodesSchemaContent);

            TextReader publishedNodesSchemaFile = null;

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn.ToString(), publishedNodesSchemaFile);
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

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
            Assert.Equal("TestingDisplayName", model.Id);
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

            // ToDo: Add definition for events in schema validator.
            //using var schemaReader = new StreamReader("Storage/publishednodesschema.json");
            //var publishedNodesSchemaContent = await schemaReader.ReadToEndAsync().ConfigureAwait(false);
            //using var publishedNodesSchemaFile = new StringReader(publishedNodesSchemaContent);

            TextReader publishedNodesSchemaFile = null;

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, publishedNodesSchemaFile);
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

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

            var dataItemModel = jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("i=2258", dataItemModel.PublishedVariableNodeId);

            dataItemModel = jobs[1].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("ns=0;i=2261", dataItemModel.PublishedVariableNodeId);

            dataItemModel = jobs[2].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData[0];
            Assert.Equal("AlternatingBoolean", dataItemModel.Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean", dataItemModel.PublishedVariableNodeId);

            var eventModel = jobs[2].WriterGroup.DataSetWriters[1].DataSet.DataSetSource.PublishedEvents.PublishedData[0];
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

            // ToDo: Add definition for events in schema validator.
            //using var schemaReader = new StreamReader("Storage/publishednodesschema.json");
            //var publishedNodesSchemaContent = await schemaReader.ReadToEndAsync().ConfigureAwait(false);
            //using var publishedNodesSchemaFile = new StringReader(publishedNodesSchemaContent);

            TextReader publishedNodesSchemaFile = null;

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, publishedNodesSchemaFile);
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

            Assert.NotEmpty(jobs);
            var j = Assert.Single(jobs);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters);
            Assert.Empty(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedVariables.PublishedData);
            Assert.Single(jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData);

            Assert.All(j.WriterGroup.DataSetWriters, dataSetWriter =>
               Assert.Single(dataSetWriter.DataSet.DataSetSource.PublishedEvents.PublishedData));

            var eventModel = jobs[0].WriterGroup.DataSetWriters[0].DataSet.DataSetSource.PublishedEvents.PublishedData[0];
            Assert.Equal("DisplayName2253", eventModel.Id);
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

            // ToDo: Add definition for events in schema validator.
            //using var schemaReader = new StreamReader("Storage/publishednodesschema.json");
            //var publishedNodesSchemaContent = await schemaReader.ReadToEndAsync().ConfigureAwait(false);
            //using var publishedNodesSchemaFile = new StringReader(publishedNodesSchemaContent);

            TextReader publishedNodesSchemaFile = null;

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var logger = TraceLogger.Create();

            var converter = new PublishedNodesJobConverter(logger, _serializer,
                engineConfigMock.Object, clientConfignMock.Object);

            var standaloneCli = new StandaloneCliModel();
            var entries = converter.Read(pn, publishedNodesSchemaFile);
            var jobs = converter.ToWriterGroupJobs(entries, standaloneCli).ToList();

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

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
