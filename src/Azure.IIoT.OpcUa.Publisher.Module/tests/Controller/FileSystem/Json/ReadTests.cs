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
    public sealed class ReadTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ReadTests(FileSystemServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private ReadTests<ConnectionModel> GetTests()
        {
            return new ReadTests<ConnectionModel>(
                _client.Resolve<IFileSystemServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly FileSystemServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task ReadFileTest0Async()
        {
            return GetTests().ReadFileTest0Async();
        }

        [SkippableFact]
        public Task ReadFileTest1Async()
        {
            Skip.If(true);
            return GetTests().ReadFileTest1Async();
        }

        [SkippableFact]
        public Task ReadFileTest2Async()
        {
            Skip.If(true);
            return GetTests().ReadFileTest2Async();
        }

        [SkippableFact]
        public Task ReadFileTest3Async()
        {
            Skip.If(true);
            return GetTests().ReadFileTest3Async();
        }

        [SkippableFact]
        public Task ReadFileTest4Async()
        {
            Skip.If(true);
            return GetTests().ReadFileTest4Async();
        }
    }
}
