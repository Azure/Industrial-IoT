// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.TestData.MsgPack
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class ExpandTests1 : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ExpandTests1(TestDataServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private ConfigurationTests1 GetTests()
        {
            return new ConfigurationTests1(_client.Resolve<IConfigurationServices>(),
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly IContainer _client;

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

        [Fact]
        public Task ExpandTest10Async()
        {
            return GetTests().ExpandTest10Async();
        }

        [Fact]
        public Task ExpandTest11Async()
        {
            return GetTests().ExpandTest11Async();
        }
    }
}
