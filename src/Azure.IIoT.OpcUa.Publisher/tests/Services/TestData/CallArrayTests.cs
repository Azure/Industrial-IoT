// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(WriteCollection.Name)]
    public class CallArrayTests
    {
        public CallArrayTests(TestDataServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private CallArrayMethodTests<ConnectionModel> GetTests()
        {
            return new CallArrayMethodTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly ITestOutputHelper _output;

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

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test3Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test4Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test4Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test5Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test5Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test3Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test4Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test4Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test3Async();
        }
    }
}
