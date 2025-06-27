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

    [Collection(WriteCollection.Name)]
    public class CallScalarTests : IClassFixture<PublisherModuleFixture>
    {
        public CallScalarTests(TestDataServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod1TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod1TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod2TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod2TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test3Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test3Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test4Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test4Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test5Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test5Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod2Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod2Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod2Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod2Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallBoiler2ResetTestAsync()
        {
            return GetTests().NodeMethodCallBoiler2ResetTestAsync();
        }

        [Fact]
        public Task NodeMethodCallBoiler1ResetTestAsync()
        {
            return GetTests().NodeMethodCallBoiler1ResetTestAsync();
        }
    }
}
