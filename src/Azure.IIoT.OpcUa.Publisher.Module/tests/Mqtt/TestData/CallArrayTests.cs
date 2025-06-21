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
    public class CallArrayTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public CallArrayTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private CallArrayMethodTests<ConnectionModel> GetTests()
        {
            return new CallArrayMethodTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod1TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod1TestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod2TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod2TestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod3TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod3TestAsync(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test1Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test3Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test4Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test4Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test5Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test5Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test1Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test3Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test4Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test4Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test1Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test2Async(Ct);
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test3Async(Ct);
        }
    }
}
