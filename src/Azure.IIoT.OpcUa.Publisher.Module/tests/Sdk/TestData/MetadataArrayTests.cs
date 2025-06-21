// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.TestData
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class MetadataArrayTests : IClassFixture<PublisherModuleFixture>
    {
        public MetadataArrayTests(TestDataServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private CallArrayMethodTests<ConnectionModel> GetTests()
        {
            return new CallArrayMethodTests<ConnectionModel>(
               _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
               _server.GetConnection(), newMetadata: true);
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod1TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod1TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod2TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod2TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod3TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod3TestAsync();
        }
    }
}
