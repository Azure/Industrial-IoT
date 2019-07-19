// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Text;
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
        public IoTHubTwinMethodClient(IIoTHubTwinServices twin) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
        }

        /// <inheritdoc/>
        public async Task<string> CallMethodAsync(string deviceId, string moduleId,
            string method, string payload, TimeSpan? timeout, CancellationToken ct) {
            var result = await _twin.CallMethodAsync(deviceId, moduleId,
                new MethodParameterModel {
                    Name = method,
                    ResponseTimeout = timeout,
                    JsonPayload = payload
                }, ct);
            if (result.Status != 200) {
                throw new MethodCallStatusException(
                    Encoding.UTF8.GetBytes(result.JsonPayload), result.Status);
            }
            return result.JsonPayload;
        }

        private readonly IIoTHubTwinServices _twin;
    }
}
