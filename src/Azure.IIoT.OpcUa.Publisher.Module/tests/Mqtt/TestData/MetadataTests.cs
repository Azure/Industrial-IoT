// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.TestData
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class MetadataTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public MetadataTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private NodeMetadataTests<ConnectionModel> GetTests()
        {
            return new NodeMetadataTests<ConnectionModel>(
               _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
               _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task GetServerCapabilitiesTestAsync()
        {
            return GetTests().GetServerCapabilitiesTestAsync(Ct);
        }

        [Fact]
        public Task HistoryGetServerCapabilitiesTestAsync()
        {
            return GetTests().HistoryGetServerCapabilitiesTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForFolderTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForFolderTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForServerObjectTestAsync()
        {
            return GetTests().NodeGetMetadataForServerObjectTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForConditionTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForConditionTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataTestForBaseEventTypeTestAsync()
        {
            return GetTests().NodeGetMetadataTestForBaseEventTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForBaseInterfaceTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForBaseInterfaceTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForBaseDataVariableTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForBaseDataVariableTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForPropertyTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForPropertyTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForAudioVariableTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForAudioVariableTypeTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForServerStatusVariableTestAsync()
        {
            return GetTests().NodeGetMetadataForServerStatusVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeGetMetadataForRedundancySupportPropertyTestAsync()
        {
            return GetTests().NodeGetMetadataForRedundancySupportPropertyTestAsync(Ct);
        }
    }
}
