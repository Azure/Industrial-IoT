// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.FileSystem.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(FileCollection.Name)]
    public sealed class BrowseTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public BrowseTests(FileSystemServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private BrowseTests<ConnectionModel> GetTests()
        {
            return new BrowseTests<ConnectionModel>(
               _client.Resolve<IFileSystemServices<ConnectionModel>>,
               _server.GetConnection());
        }

        private readonly FileSystemServer _server;
        private readonly IContainer _client;

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

        [Fact]
        public Task GetDirectoriesTest4Async()
        {
            return GetTests().GetDirectoriesTest4Async();
        }

        [Fact]
        public Task GetDirectoriesTest5Async()
        {
            return GetTests().GetDirectoriesTest5Async();
        }

        [Fact]
        public Task GetDirectoriesTest6Async()
        {
            return GetTests().GetDirectoriesTest6Async();
        }

        [Fact]
        public Task GetFilesTest1Async()
        {
            return GetTests().GetFilesTest1Async();
        }

        [Fact]
        public Task GetFilesTest2Async()
        {
            return GetTests().GetFilesTest2Async();
        }

        [Fact]
        public Task GetFilesTest3Async()
        {
            return GetTests().GetFilesTest3Async();
        }

        [Fact]
        public Task GetFilesTest4Async()
        {
            return GetTests().GetFilesTest4Async();
        }

        [Fact]
        public Task GetFilesTest5Async()
        {
            return GetTests().GetFilesTest5Async();
        }

        [Fact]
        public Task GetFilesTest6Async()
        {
            return GetTests().GetFilesTest6Async();
        }
    }
}
