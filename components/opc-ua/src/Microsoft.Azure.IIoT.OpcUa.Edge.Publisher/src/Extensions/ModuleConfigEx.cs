// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Abstractions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Linq;

    /// <summary>
    /// Module config extensions
    /// </summary>
    public static class ModuleConfigEx {

        /// <summary>
        /// Clone module config
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static IModuleConfig Clone(this IModuleConfig config,
            string connectionString = null) {
            if (config == null) {
                return null;
            }
            return new DeviceClientConfig(config, connectionString);
        }

        /// <summary>
        /// Clone of a module config
        /// </summary>
        private sealed class DeviceClientConfig : IModuleConfig {

            /// <inheritdoc/>
            public string EdgeHubConnectionString { get; }
            /// <inheritdoc/>
            public string MqttClientConnectionString { get; }
            /// <inheritdoc/>
            public bool BypassCertVerification { get; }
            /// <inheritdoc/>
            public bool EnableMetrics { get; }
            /// <inheritdoc/>
            public TransportOption Transport { get; }

            /// <inheritdoc/>
            public string TelemetryTopicTemplate { get; }

            /// <summary>
            /// Create clone
            /// </summary>
            /// <param name="config"></param>
            /// <param name="connectionString"></param>
            public DeviceClientConfig(IModuleConfig config, string connectionString) {
                EdgeHubConnectionString = GetEdgeHubConnectionString(config, connectionString);
                BypassCertVerification = config.BypassCertVerification;
                Transport = config.Transport;
            }

            /// <summary>
            /// Create new connection string from existing EdgeHubConnectionString.
            /// </summary>
            /// <param name="config"></param>
            /// <param name="connectionString"></param>
            /// <returns></returns>
            private static string GetEdgeHubConnectionString(IModuleConfig config,
                string connectionString) {
                var cs = config.EdgeHubConnectionString;
                if (string.IsNullOrEmpty(connectionString)) {
                    return cs;
                }
                var csUpdate = ConnectionString.Parse(connectionString);
                var deviceId = csUpdate.DeviceId;
                var key = csUpdate.SharedAccessKey;
                if (string.IsNullOrEmpty(cs)) {
                    // Retrieve information from environment
                    var hostName = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_IOTHUBHOSTNAME);
                    if (string.IsNullOrEmpty(hostName)) {
                        throw new InvalidConfigurationException(
                            $"Missing {IoTEdgeVariables.IOTEDGE_IOTHUBHOSTNAME} variable in environment");
                    }
                    var edgeName = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_GATEWAYHOSTNAME);
                    cs = $"HostName={hostName};DeviceId={deviceId};SharedAccessKey={key}";
                    if (string.IsNullOrEmpty(edgeName)) {
                        cs += $";GatewayHostName={edgeName}";
                    }
                }
                else {
                    // Use existing connection string as a master plan
                    var lookup = cs
                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().Split('='))
                        .ToDictionary(s => s[0].ToLowerInvariant(), v => v[1]);
                    if (!lookup.TryGetValue("hostname", out var hostName) ||
                        string.IsNullOrEmpty(hostName)) {
                        throw new InvalidConfigurationException(
                            "Missing HostName in connection string");
                    }

                    cs = $"HostName={hostName};DeviceId={deviceId};SharedAccessKey={key}";
                    if (lookup.TryGetValue("GatewayHostName", out var edgeName) &&
                        !string.IsNullOrEmpty(edgeName)) {
                        cs += $";GatewayHostName={edgeName}";
                    }
                }
                return cs;
            }
        }
    }
}