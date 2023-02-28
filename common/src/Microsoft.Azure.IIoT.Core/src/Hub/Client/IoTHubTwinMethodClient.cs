// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client
{
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Method client using twin services
    /// </summary>
    public sealed class IoTHubTwinMethodClient : IJsonMethodClient
    {
        /// <inheritdoc/>
        public int MaxMethodPayloadCharacterCount => 120 * 1024;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public IoTHubTwinMethodClient(IIoTHubTwinServices twin, ILogger logger)
        {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<string> CallMethodAsync(string deviceId, string moduleId,
            string method, string json, TimeSpan? timeout, CancellationToken ct)
        {
            _logger.LogTrace("Call {Method} on {DeviceId} ({ModuleId}) with {Payload}... ",
                method, deviceId, moduleId, json);
            var result = await _twin.CallMethodAsync(deviceId, moduleId,
                new MethodParameterModel
                {
                    Name = method,
                    ResponseTimeout = timeout ?? TimeSpan.FromSeconds(kDefaultMethodTimeout),
                    JsonPayload = json
                }, ct).ConfigureAwait(false);
            if (result.Status != 200)
            {
                _logger.LogDebug("Call {Method} on {DeviceId} ({ModuleId}) with {Payload} " +
                    "returned with error {Status}: {Result}",
                    method, deviceId, moduleId, json, result.Status, result.JsonPayload);
                throw new MethodCallStatusException(result.JsonPayload, result.Status);
            }
            return result.JsonPayload;
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;

        /// <summary>
        /// 5 minutes - default is 30 seconds
        /// </summary>
        private const int kDefaultMethodTimeout = 300;
    }
}
