// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.FileSystem
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(FileCollection.Name)]
    public class FileSystemBrowseTests
    {
        public FileSystemBrowseTests(FileSystemServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private FileSystemBrowseTests<ConnectionModel> GetTests()
        {
            return new FileSystemBrowseTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level),
                    new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions()),
                _server.GetConnection());
        }

        private readonly FileSystemServer _server;
        private readonly ITestOutputHelper _output;

        [Fact]
        public Task GetFileSystemsTest1Async()
        {
            return GetTests().GetFileSystemsTest1Async();
        }

        [Fact]
        public Task GetDirectoriesTest1Async()
        {
            return GetTests().GetDirectoriesTest1Async();
        }

        [Fact]
        public Task GetDirectoriesTest2Async()
        {
            return GetTests().GetDirectoriesTest2Async();
        }

        [Fact]
        public Task GetDirectoriesTest3Async()
        {
            return GetTests().GetDirectoriesTest3Async();
        }
    }
}
