// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.FileSystem.Json
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
    public sealed class WriteTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public WriteTests(FileSystemServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private WriteTests<ConnectionModel> GetTests()
        {
            return new WriteTests<ConnectionModel>(
                _client.Resolve<IFileSystemServices<ConnectionModel>>,
                _server.GetConnection(), _server.TempPath);
        }

        private readonly FileSystemServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task WriteFileTest0Async()
        {
            return GetTests().WriteFileTest0Async();
        }

        [SkippableFact]
        public Task WriteFileTest1Async()
        {
            Skip.If(true);
            return GetTests().WriteFileTest1Async();
        }

        [SkippableFact]
        public Task WriteFileTest2Async()
        {
            Skip.If(true);
            return GetTests().WriteFileTest2Async();
        }

        [Fact]
        public Task AppendFileTest0Async()
        {
            return GetTests().AppendFileTest0Async();
        }

        [SkippableFact]
        public Task AppendFileTest1Async()
        {
            Skip.If(true);
            return GetTests().AppendFileTest1Async();
        }

        [Fact]
        public Task AppendFileTest2Async()
        {
            return GetTests().AppendFileTest2Async();
        }
    }
}
