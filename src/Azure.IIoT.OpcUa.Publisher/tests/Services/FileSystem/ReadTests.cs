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
    public class ReadTests
    {
        public ReadTests(FileSystemServer server)
        {
            _server = server;
        }

        private ReadTests<ConnectionModel> GetTests()
        {
            return new ReadTests<ConnectionModel>(
                () => new FileSystemServices<ConnectionModel>(_server.Client,
                    new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions()),
                _server.GetConnection(), _server.TempPath);
        }

        private readonly FileSystemServer _server;

        [Fact]
        public Task ReadFileTest0Async()
        {
            return GetTests().ReadFileTest0Async();
        }

        [Fact]
        public Task ReadFileTest1Async()
        {
            return GetTests().ReadFileTest1Async();
        }

        [Fact]
        public Task ReadFileTest2Async()
        {
            return GetTests().ReadFileTest2Async();
        }

        [Fact]
        public Task ReadFileTest3Async()
        {
            return GetTests().ReadFileTest3Async();
        }

        [Fact]
        public Task ReadFileTest4Async()
        {
            return GetTests().ReadFileTest4Async();
        }
    }
}
