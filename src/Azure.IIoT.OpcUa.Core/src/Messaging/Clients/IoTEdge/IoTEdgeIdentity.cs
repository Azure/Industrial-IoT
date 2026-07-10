// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// Edge client identity.
    /// </summary>
    public sealed class IoTEdgeIdentity : IIoTEdgeDeviceIdentity
    {
        /// <inheritdoc />
        public string? Hub { get; }

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public string? ModuleId { get; }

        /// <inheritdoc />
        public string? Gateway { get; }

        /// <summary>
        /// Create identity.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public IoTEdgeIdentity(IOptions<IoTEdgeClientOptions> options,
            ILogger<IoTEdgeIdentity> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            var deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            var moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            var gateway = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
            var hub = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");

            try
            {
                if (!string.IsNullOrEmpty(options.Value.EdgeHubConnectionString))
                {
                    var cs = ConnectionString.Parse(options.Value.EdgeHubConnectionString);
                    deviceId = cs.DeviceId;
                    moduleId = cs.ModuleId;
                    hub = cs.HostName;
                    gateway = cs.GatewayHostName ?? gateway;
                }
            }
            catch (Exception ex)
            {
                logger.BadConfigurationValue(ex);
            }

            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(hub))
            {
                throw new InvalidConfigurationException(
                    "IoT Edge configuration is incomplete. Set the IOTEDGE_* " +
                    "environment variables or EdgeHubConnectionString.");
            }

            Hub = hub;
            ModuleId = moduleId;
            DeviceId = deviceId;
            Gateway = gateway;
        }
    }

    /// <summary>
    /// Source-generated logging for IoTEdgeIdentity.
    /// </summary>
    internal static partial class IoTEdgeIdentityLogging
    {
        private const int EventClass = 920;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Error,
            Message = "Bad configuration value in EdgeHubConnectionString config.")]
        public static partial void BadConfigurationValue(this ILogger logger, Exception ex);
    }
}
