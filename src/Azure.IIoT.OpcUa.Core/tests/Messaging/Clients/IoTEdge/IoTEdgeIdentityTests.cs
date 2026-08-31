// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using Xunit;

    public sealed class IoTEdgeIdentityTests
    {
        [Fact]
        public void ConstructorReadsIdentityFromEnvironment()
        {
            using var environment = new EdgeEnvironment(
                "hub.azure-devices.net", "device", "module", "gateway");

            var identity = new IoTEdgeIdentity(Options.Create(
                new IoTEdgeClientOptions()),
                NullLogger<IoTEdgeIdentity>.Instance);

            Assert.Equal("hub.azure-devices.net", identity.Hub);
            Assert.Equal("device", identity.DeviceId);
            Assert.Equal("module", identity.ModuleId);
            Assert.Equal("gateway", identity.Gateway);
        }

        [Fact]
        public void ConstructorPrefersConnectionStringOverEnvironment()
        {
            using var environment = new EdgeEnvironment(
                "env.azure-devices.net", "env-device", "env-module", "env-gateway");

            var identity = new IoTEdgeIdentity(Options.Create(
                new IoTEdgeClientOptions
                {
                    EdgeHubConnectionString =
                        "HostName=cs.azure-devices.net;DeviceId=cs-device;" +
                        "ModuleId=cs-module;GatewayHostName=cs-gateway;" +
                        "SharedAccessKey=key"
                }), NullLogger<IoTEdgeIdentity>.Instance);

            Assert.Equal("cs.azure-devices.net", identity.Hub);
            Assert.Equal("cs-device", identity.DeviceId);
            Assert.Equal("cs-module", identity.ModuleId);
            Assert.Equal("cs-gateway", identity.Gateway);
        }

        [Fact]
        public void ConstructorFallsBackToEnvironmentWhenConnectionStringIsBad()
        {
            using var environment = new EdgeEnvironment(
                "hub.azure-devices.net", "device", "module", "gateway");

            var identity = new IoTEdgeIdentity(Options.Create(
                new IoTEdgeClientOptions
                {
                    EdgeHubConnectionString = "not a connection string"
                }), NullLogger<IoTEdgeIdentity>.Instance);

            Assert.Equal("hub.azure-devices.net", identity.Hub);
            Assert.Equal("device", identity.DeviceId);
            Assert.Equal("module", identity.ModuleId);
            Assert.Equal("gateway", identity.Gateway);
        }

        [Fact]
        public void ConstructorRejectsIncompleteConfiguration()
        {
            using var environment = new EdgeEnvironment(null, null, null, null);

            Assert.Throws<InvalidConfigurationException>(() =>
                new IoTEdgeIdentity(Options.Create(new IoTEdgeClientOptions()),
                    NullLogger<IoTEdgeIdentity>.Instance));
        }

        [Fact]
        public void ConstructorRejectsNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new IoTEdgeIdentity(null!, NullLogger<IoTEdgeIdentity>.Instance));
            Assert.Throws<ArgumentNullException>(() =>
                new IoTEdgeIdentity(Options.Create(new IoTEdgeClientOptions()),
                    null!));
        }

        private sealed class EdgeEnvironment : IDisposable
        {
            public EdgeEnvironment(string? hub, string? deviceId,
                string? moduleId, string? gateway)
            {
                _hub = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
                _deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
                _moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
                _gateway = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
                Environment.SetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME", hub);
                Environment.SetEnvironmentVariable("IOTEDGE_DEVICEID", deviceId);
                Environment.SetEnvironmentVariable("IOTEDGE_MODULEID", moduleId);
                Environment.SetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME", gateway);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME", _hub);
                Environment.SetEnvironmentVariable("IOTEDGE_DEVICEID", _deviceId);
                Environment.SetEnvironmentVariable("IOTEDGE_MODULEID", _moduleId);
                Environment.SetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME", _gateway);
            }

            private readonly string? _hub;
            private readonly string? _deviceId;
            private readonly string? _moduleId;
            private readonly string? _gateway;
        }
    }
}
