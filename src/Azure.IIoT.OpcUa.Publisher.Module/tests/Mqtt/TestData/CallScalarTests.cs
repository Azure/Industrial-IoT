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

    [Collection(WriteCollection.Name)]
    public class CallScalarTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv311Fixture>
    {
        public CallScalarTests(TestDataServer server,
            PublisherModuleMqttv311Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private CallScalarMethodTests<ConnectionModel> GetTests()
        {
            return new CallScalarMethodTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleMqttv311Fixture _module;

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod1TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod1TestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod2TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod2TestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3TestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async(Ct);
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test1Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test3Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test3Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test4Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test4Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test5Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test5Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod2Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod2Test1Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod2Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod2Test2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3Test1Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3Test2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallBoiler2ResetTestAsync()
        {
            return GetTests().NodeMethodCallBoiler2ResetTestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallBoiler1ResetTestAsync()
        {
            return GetTests().NodeMethodCallBoiler1ResetTestAsync(Ct);
        }
    }
}
