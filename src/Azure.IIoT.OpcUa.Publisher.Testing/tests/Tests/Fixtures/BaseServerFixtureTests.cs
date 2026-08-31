// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Xunit;

    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class ServerIntegrationCollection : ICollectionFixture<TestDataServer>
    {
        public const string Name = "ServerIntegration";
    }

    [Collection(ServerIntegrationCollection.Name)]
    [Trait("Category", "ServerIntegration")]
    public sealed class BaseServerFixtureTests
    {
        public BaseServerFixtureTests(TestDataServer server)
        {
            _server = server;
        }

        [Fact]
        public void UsesCanonicalLoopbackForItsDefaultConnection()
        {
            var connection = _server.GetConnection();

            Assert.Equal("127.0.0.1", _server.Host?.HostName);
            Assert.StartsWith("opc.tcp://127.0.0.1:", _server.EndpointUrl,
                StringComparison.Ordinal);
            Assert.Equal(_server.EndpointUrl, connection.Endpoint.Url);
            Assert.True(connection.Endpoint.AlternativeUrls is null ||
                connection.Endpoint.AlternativeUrls.Count == 0);
            Assert.StartsWith(_server.TempPath, _server.ClientPkiRootPath,
                StringComparison.Ordinal);
            Assert.StartsWith(_server.TempPath, _server.ServerPkiRootPath,
                StringComparison.Ordinal);
            Assert.NotEqual(_server.ClientPkiRootPath, _server.ServerPkiRootPath);
        }

        [Fact]
        public void UsesExplicitAlternativeHostsWithoutEnumeratingMachineAddresses()
        {
            string tempPath;
            string clientPkiPath;
            string serverPkiPath;
            using (var server = new AlternativeAddressServer())
            {
                tempPath = server.TempPath;
                clientPkiPath = server.ClientPkiRootPath;
                serverPkiPath = server.ServerPkiRootPath;

                var connection = server.GetConnection();

                Assert.StartsWith("opc.tcp://127.0.0.1:", server.EndpointUrl,
                    StringComparison.Ordinal);
                Assert.Contains(connection.Endpoint.AlternativeUrls!,
                    url => url.StartsWith("opc.tcp://127.0.0.2:", StringComparison.Ordinal));
                Assert.DoesNotContain(connection.Endpoint.AlternativeUrls!,
                    url => url.Contains("127.0.0.1", StringComparison.Ordinal));
            }

            Assert.False(Directory.Exists(clientPkiPath));
            Assert.False(Directory.Exists(serverPkiPath));
            Assert.False(Directory.Exists(tempPath));
        }

        private readonly TestDataServer _server;

        private sealed class AlternativeAddressServer : BaseServerFixture
        {
            public AlternativeAddressServer()
                : base(CreateNodes, alternativeHosts: ["127.0.0.2"])
            {
            }

            private static IEnumerable<INodeManagerFactory> CreateNodes(
                ILoggerFactory? factory, TimeService timeService)
            {
                return TestDataServer.TestData(factory, timeService);
            }
        }
    }
}
