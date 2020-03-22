// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Client for supervisor diagnostics services
    /// </summary>
    public sealed class TwinModuleDiagnosticsClient : ISupervisorDiagnostics {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public TwinModuleDiagnosticsClient(IMethodClient client, IJsonSerializer serializer,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusModel> GetSupervisorStatusAsync(
            string supervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId,
                out var moduleId);
            var result = await _client.CallMethodAsync(deviceId, moduleId,
                "GetStatus_V2", null, null, ct);
            _logger.Debug("Get {deviceId}/{moduleId} status took " +
                "{elapsed} ms.", deviceId, moduleId, sw.ElapsedMilliseconds);
            return _serializer.Deserialize<SupervisorStatusModel>(
                result);
        }

        /// <inheritdoc/>
        public async Task ResetSupervisorAsync(string supervisorId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId,
                out var moduleId);
            var result = await _client.CallMethodAsync(deviceId, moduleId,
                "Reset_V2", null, null, ct);
            _logger.Debug("Reset supervisor {deviceId}/{moduleId} took " +
                "{elapsed} ms.", deviceId, moduleId, sw.ElapsedMilliseconds);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
