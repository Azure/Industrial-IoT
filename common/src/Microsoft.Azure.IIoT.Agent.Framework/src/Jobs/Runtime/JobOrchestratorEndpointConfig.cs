// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Default endpoint configuration
    /// </summary>
    public class JobOrchestratorEndpointConfig : ConfigBase, IJobOrchestratorEndpointConfig {

        /// <summary>
        /// Property keys
        /// </summary>
        private const string kJobOrchestratorUrlKey = "JobOrchestratorUrl";
        private const string kJobOrchestratorUrlSyncIntervalKey = "JobOrchestratorUrlSyncInterval";

        /// <inheritdoc/>
        public string JobOrchestratorUrl => GetStringOrDefault(kJobOrchestratorUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL,
                () => GetDefaultUrl("9051", "edge/publisher")));

        /// <inheritdoc/>
        public TimeSpan JobOrchestratorUrlSyncInterval =>
            GetDurationOrDefault(kJobOrchestratorUrlSyncIntervalKey,
                () => TimeSpan.FromMinutes(1));

        /// <summary>
        /// Create endpoint config
        /// </summary>
        /// <param name="configuration"></param>
        public JobOrchestratorEndpointConfig(IConfiguration configuration) :
            base(configuration) {
        }

        /// <summary>
        /// Get endpoint url
        /// </summary>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string GetDefaultUrl(string port, string path) {
            var cloudEndpoint = GetStringOrDefault(PcsVariable.PCS_SERVICE_URL)?.Trim()?.TrimEnd('/');
            if (string.IsNullOrEmpty(cloudEndpoint)) {
                // Test port is open
                if (!int.TryParse(port, out var nPort)) {
                    return $"http://localhost:9080/{path}";
                }
                using (var socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Unspecified)) {
                    try {
                        socket.Connect(IPAddress.Loopback, nPort);
                        return $"http://localhost:{port}";
                    }
                    catch {
                        return $"http://localhost:9080/{path}";
                    }
                }
            }
            return $"{cloudEndpoint}/{path}";
        }
    }
}
