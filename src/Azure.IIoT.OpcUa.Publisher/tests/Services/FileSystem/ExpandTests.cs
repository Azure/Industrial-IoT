// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.FileSystem
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(FileCollection.Name)]
    public class ExpandTests
    {
        public ExpandTests(FileSystemServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private ConfigurationTests GetTests()
        {
            return new ConfigurationTests(new ConfigurationServices(null!, _server.Client,
                new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions(),
                _output.BuildLoggerFor<ConfigurationServices>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly FileSystemServer _server;
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
