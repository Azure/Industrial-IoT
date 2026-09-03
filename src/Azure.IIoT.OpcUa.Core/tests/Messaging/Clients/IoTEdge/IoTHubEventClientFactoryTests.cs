// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using global::IoTHubby;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTHubEventClientFactoryTests
    {
        [Fact]
        public async Task CreatesDedicatedClientFromConnectionStringAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            var clientFactory = new IoTEdgeTestModuleClientFactory(sdk);
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions
                {
                    Product = "product",
                    KeepAlivePeriodSeconds = 42,
                    DefaultMethodCallTimeout = TimeSpan.FromSeconds(7)
                }),
                [],
                NullLoggerFactory.Instance,
                clientFactory);
            var connectionString =
                "HostName=test.azure-devices.net;DeviceId=child;" +
                "SharedAccessKey=ZmFrZWtleQ==";

            var scope = factory.CreateEventClient(connectionString,
                out var client);

            var transport = Assert.IsType<IoTEdgeTransport>(client);
            Assert.Equal("IoTHub", factory.Name);
            Assert.Equal("IoTHub", transport.Name);
            Assert.Equal("child", transport.Identity);
            Assert.Equal(connectionString,
                clientFactory.Options?.EdgeHubConnectionString);
            Assert.Equal("product", clientFactory.Options?.Product);
            Assert.Equal(TimeSpan.FromSeconds(7),
                clientFactory.ConfiguredOptions?.OperationTimeout);

            using var @event = transport.CreateEvent()
                .SetContentType("application/json")
                .AddBuffers([
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}"))
                ]);
            await @event.SendAsync();
            Assert.Single(sdk.Telemetry);

            scope.Dispose();
            scope.Dispose();

            Assert.Equal(1, sdk.DisposeCount);
        }

        [Fact]
        public void RejectsEmptyConnectionString()
        {
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions()),
                [],
                NullLoggerFactory.Instance,
                new IoTEdgeTestModuleClientFactory(
                    new IoTEdgeTestModuleClient()));

            Assert.Throws<ArgumentException>(() =>
                factory.CreateEventClient(string.Empty, out _));
        }

        [Fact]
        public async Task RealFactoryAcceptsDeviceConnectionStringAsync()
        {
            var client = IoTHubModuleClientFactory.Instance.Create(
                new IoTEdgeClientOptions
                {
                    EdgeHubConnectionString =
                        "HostName=test.azure-devices.net;DeviceId=child;" +
                        "SharedAccessKey=ZmFrZWtleQ=="
                },
                _ => { });
            await using (client)
            {
                Assert.IsType<IoTHubModuleClientFactory.DeviceAdapter>(client);
                Assert.Equal(IoTHubConnectionState.Disconnected, client.State);
            }
        }

        [Fact]
        public void RealFactoryRoutesX509DeviceConnectionStringToDeviceClient()
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                IoTHubModuleClientFactory.Instance.Create(
                    new IoTEdgeClientOptions
                    {
                        EdgeHubConnectionString =
                            "HostName=test.azure-devices.net;DeviceId=child;" +
                            "X509=true"
                    },
                    _ => { }));

            Assert.Contains("ClientCertificate", error.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task RealFactoryAcceptsModuleConnectionStringAsync()
        {
            var client = IoTHubModuleClientFactory.Instance.Create(
                new IoTEdgeClientOptions
                {
                    EdgeHubConnectionString =
                        "HostName=test.azure-devices.net;DeviceId=device;" +
                        "ModuleId=module;SharedAccessKey=ZmFrZWtleQ=="
                },
                _ => { });
            await using (client)
            {
                Assert.IsType<IoTHubModuleClientFactory.Adapter>(client);
            }
        }
    }
}
