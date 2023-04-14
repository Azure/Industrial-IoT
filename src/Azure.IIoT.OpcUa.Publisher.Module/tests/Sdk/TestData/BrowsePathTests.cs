// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class BrowsePathTests : IClassFixture<PublisherModuleFixture>
    {
        public BrowsePathTests(TestDataServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private BrowsePathTests<ConnectionModel> GetTests()
        {
            return new BrowsePathTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test1Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test2Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test3Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test3Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethodsTestAsync()
        {
            return GetTests().NodeBrowsePathStaticScalarMethodsTestAsync();
        }
    }
}
