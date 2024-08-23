// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.Asset.Json
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(WriteCollection4.Name)]
    public sealed class ConfigurationTest4 : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ConfigurationTest4(AssetServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private AssetTests4 GetTests()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new AssetTests4(_ => _client.Resolve<IAssetConfiguration<Stream>>(),
                _server.GetConnection(), false);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        private readonly AssetServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task ConfigureDuplicateAssetFailsAsync()
        {
            return GetTests().ConfigureDuplicateAssetFailsAsync();
        }

        [SkippableFact]
        public Task ConfigureAssetFails1Async()
        {
            Skip.If(true, "Using bytes only");
            return GetTests().ConfigureAssetFails1Async();
        }

        [SkippableFact]
        public Task ConfigureWithBadStreamFails1Async()
        {
            Skip.If(true, "Using bytes only");
            return GetTests().ConfigureWithBadStreamFails1Async();
        }

        [SkippableFact]
        public Task ConfigureWithBadStreamFails2Async()
        {
            Skip.If(true, "Using bytes only");
            return GetTests().ConfigureWithBadStreamFails2Async();
        }

        [SkippableFact]
        public Task ConfigureWithBadStreamFails3Async()
        {
            Skip.If(true, "Using bytes only");
            return GetTests().ConfigureWithBadStreamFails3Async();
        }
    }
}
