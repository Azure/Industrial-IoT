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
    public class BrowsePathTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public BrowsePathTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
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
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test1Async(Ct);
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test2Async(Ct);
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test3Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test3Async(Ct);
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethodsTestAsync()
        {
            return GetTests().NodeBrowsePathStaticScalarMethodsTestAsync(Ct);
        }
    }
}
