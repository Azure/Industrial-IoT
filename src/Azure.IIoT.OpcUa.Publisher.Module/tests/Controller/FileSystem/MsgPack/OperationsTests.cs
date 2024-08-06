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
    public sealed class OperationsTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public OperationsTests(FileSystemServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private OperationsTests<ConnectionModel> GetTests()
        {
            return new OperationsTests<ConnectionModel>(
              _client.Resolve<IFileSystemServices<ConnectionModel>>,
              _server.GetConnection(), _server.TempPath);
        }

        private readonly FileSystemServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task CreateDirectoryTest1Async()
        {
            return GetTests().CreateDirectoryTest1Async();
        }

        [Fact]
        public Task CreateDirectoryTest2Async()
        {
            return GetTests().CreateDirectoryTest2Async();
        }

        [Fact]
        public Task CreateDirectoryTest3Async()
        {
            return GetTests().CreateDirectoryTest3Async();
        }

        [Fact]
        public Task CreateDirectoryTest4Async()
        {
            return GetTests().CreateDirectoryTest4Async();
        }

        [Fact]
        public Task DeleteDirectoryTest1Async()
        {
            return GetTests().DeleteDirectoryTest1Async();
        }

        [Fact]
        public Task DeleteDirectoryTest2Async()
        {
            return GetTests().DeleteDirectoryTest2Async();
        }

        [Fact]
        public Task DeleteDirectoryTest3Async()
        {
            return GetTests().DeleteDirectoryTest3Async();
        }

        [Fact]
        public Task CreateFileTest1Async()
        {
            return GetTests().CreateFileTest1Async();
        }

        [Fact]
        public Task CreateFileTest2Async()
        {
            return GetTests().CreateFileTest2Async();
        }

        [Fact]
        public Task CreateFileTest3Async()
        {
            return GetTests().CreateFileTest3Async();
        }

        [Fact]
        public Task CreateFileTest4Async()
        {
            return GetTests().CreateFileTest4Async();
        }

        [Fact]
        public Task GetFileInfoTest1Async()
        {
            return GetTests().GetFileInfoTest1Async();
        }

        [Fact]
        public Task GetFileInfoTest2Async()
        {
            return GetTests().GetFileInfoTest2Async();
        }

        [Fact]
        public Task GetFileInfoTest3Async()
        {
            return GetTests().GetFileInfoTest3Async();
        }

        [Fact]
        public Task DeleteFileTest1Async()
        {
            return GetTests().DeleteFileTest1Async();
        }

        [Fact]
        public Task DeleteFileTest2Async()
        {
            return GetTests().DeleteFileTest2Async();
        }

        [Fact]
        public Task DeleteFileTest3Async()
        {
            return GetTests().DeleteFileTest3Async();
        }

        [Fact]
        public Task DeleteFileTest4Async()
        {
            return GetTests().DeleteFileTest4Async();
        }

        [Fact]
        public Task DeleteFileTest5Async()
        {
            return GetTests().DeleteFileTest5Async();
        }
    }
}
