// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Isa95Jobs
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeServicesTests : IClassFixture<Isa95JobsServer>, IClassFixture<PublisherModuleFixture>
    {
        public NodeServicesTests(Isa95JobsServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        [Fact]
        public Task BrowseJobResponseDataTypeAsync()
        {
            return GetTests().BrowseJobResponseDataTypeAsync();
        }

        [Fact]
        public Task ReadJobResponseDataTypeAttributesAsync()
        {
            return GetTests().ReadJobResponseDataTypeAttributesAsync();
        }

        [Fact]
        public Task GetJobResponseDataTypeMetadataAsync()
        {
            return GetTests().GetJobResponseDataTypeMetadataAsync();
        }

        [Fact]
        public Task GetStoreMethodMetadataAsync()
        {
            return GetTests().GetStoreMethodMetadataAsync();
        }

        [Fact]
        public Task EncodeDecodeJobResponseDataTypeAsync()
        {
            return GetTests().EncodeDecodeJobResponseDataTypeAsync();
        }

        private Isa95JobsServerTests<ConnectionModel> GetTests()
        {
            return new Isa95JobsServerTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly PublisherModuleFixture _module;
        private readonly Isa95JobsServer _server;
    }
}
