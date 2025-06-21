// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.Asset.Json
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(WriteCollection2.Name)]
    public sealed class ConfigurationTest2 : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ConfigurationTest2(AssetServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private AssetTests2 GetTests()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new AssetTests2(_ => _client.Resolve<IAssetConfiguration<Stream>>(),
                _server.GetConnection(), false);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        private readonly AssetServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task ConfigureAsset1Async()
        {
            return GetTests().ConfigureAsset1Async();
        }

        [Fact]
        public Task ConfigureAsset2Async()
        {
            return GetTests().ConfigureAsset2Async();
        }

        [Fact]
        public Task ConfigureAsset3Async()
        {
            return GetTests().ConfigureAsset3Async();
        }
    }
}
