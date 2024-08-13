// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class ExpandTests1
    {
        public ExpandTests1(TestDataServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private ConfigurationTests1 GetTests()
        {
            return new ConfigurationTests1(new ConfigurationServices(null!, _server.Client,
                new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions(),
                _output.BuildLoggerFor<ConfigurationServices>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly ITestOutputHelper _output;

        [Fact]
        public Task ExpandTest1Async()
        {
            return GetTests().ExpandTest1Async();
        }

        [Fact]
        public Task ExpandTest2Async()
        {
            return GetTests().ExpandTest2Async();
        }

        [Fact]
        public Task ExpandTest3Async()
        {
            return GetTests().ExpandTest3Async();
        }

        [Fact]
        public Task ExpandTest4Async()
        {
            return GetTests().ExpandTest4Async();
        }

        [Fact]
        public Task ExpandTest5Async()
        {
            return GetTests().ExpandTest5Async();
        }

        [Fact]
        public Task ExpandTest6Async()
        {
            return GetTests().ExpandTest6Async();
        }

        [Fact]
        public Task ExpandTest7Async()
        {
            return GetTests().ExpandTest7Async();
        }

        [Fact]
        public Task ExpandTest8Async()
        {
            return GetTests().ExpandTest8Async();
        }
        [Fact]
        public Task ExpandTest9Async()
        {
            return GetTests().ExpandTest9Async();
        }
    }
}
