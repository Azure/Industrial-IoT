// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Method client using twin services
    /// </summary>
    public sealed class IoTHubTwinMethodClient : IJsonMethodClient {

        /// <inheritdoc/>
        public int MaxMethodPayloadCharacterCount => 120 * 1024;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public IoTHubTwinMethodClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<string> CallMethodAsync(string deviceId, string moduleId,
            string method, string payload, TimeSpan? timeout, CancellationToken ct) {
            _logger.Verbose("Call {method} on {device} ({module}) with {payload}... ",
            method, deviceId, moduleId, payload);
            var result = await _twin.CallMethodAsync(deviceId, moduleId,
                new MethodParameterModel {
                    Name = method,
                    ResponseTimeout = timeout ?? TimeSpan.FromSeconds(kDefaultMethodTimeout),
                    JsonPayload = payload
                }, ct);
            if (result.Status != 200) {
                _logger.Debug("Call {method} on {device} ({module}) with {payload} " +
                    "returned with error {status}: {result}",
                    method, deviceId, moduleId, payload, result.Status, result.JsonPayload);
                throw new MethodCallStatusException(result.JsonPayload, result.Status);
            }
            return result.JsonPayload;
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
        private const int kDefaultMethodTimeout = 300; // 5 minutes - default is 30 seconds
    }
}
