// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.Asset.MsgPack
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

    [Collection(WriteCollection1.Name)]
    public sealed class ConfigurationTest1 : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ConfigurationTest1(AssetServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private AssetTests1 GetTests()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new AssetTests1(_ => _client.Resolve<IAssetConfiguration<Stream>>(),
                _server.GetConnection(), false);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        private readonly AssetServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task ConfigureAndDeleteAssetsAsync()
        {
            return GetTests().ConfigureAndDeleteAssetsAsync();
        }
    }
}
