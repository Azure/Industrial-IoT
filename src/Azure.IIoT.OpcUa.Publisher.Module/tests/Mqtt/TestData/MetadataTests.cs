// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class MetadataTests : IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public MetadataTests(TestDataServer server, PublisherModuleMqttv5Fixture module)
        {
            _server = server;
            _module = module;
        }

        private NodeMetadataTests<ConnectionModel> GetTests()
        {
            return new NodeMetadataTests<ConnectionModel>(
               () => _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>(),
               _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task GetServerCapabilitiesTestAsync()
        {
            return GetTests().GetServerCapabilitiesTestAsync();
        }

        [Fact]
        public Task HistoryGetServerCapabilitiesTestAsync()
        {
            return GetTests().HistoryGetServerCapabilitiesTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForFolderTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForFolderTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForServerObjectTestAsync()
        {
            return GetTests().NodeGetMetadataForServerObjectTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForConditionTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForConditionTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataTestForBaseEventTypeTestAsync()
        {
            return GetTests().NodeGetMetadataTestForBaseEventTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForBaseInterfaceTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForBaseInterfaceTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForBaseDataVariableTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForBaseDataVariableTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForPropertyTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForPropertyTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForAudioVariableTypeTestAsync()
        {
            return GetTests().NodeGetMetadataForAudioVariableTypeTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForServerStatusVariableTestAsync()
        {
            return GetTests().NodeGetMetadataForServerStatusVariableTestAsync();
        }

        [Fact]
        public Task NodeGetMetadataForRedundancySupportPropertyTestAsync()
        {
            return GetTests().NodeGetMetadataForRedundancySupportPropertyTestAsync();
        }
    }
}
