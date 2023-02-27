// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Api.TestData.Binary
{
    using Azure.IIoT.OpcUa.Services.WebApi.Clients;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class MetadataTests
    {
        public MetadataTests(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private NodeMetadataTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new NodeMetadataTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

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
