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
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class NodeExpandTests
    {
        public NodeExpandTests(TestDataServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private ExpandTests GetTests()
        {
            return new ExpandTests(new ConfigurationServices(null!, _server.Client,
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
    }
}
