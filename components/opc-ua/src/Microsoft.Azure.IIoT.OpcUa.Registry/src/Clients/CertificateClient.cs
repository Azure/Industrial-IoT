// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Module;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Client to retrieve endpoint certificate through the supervisor
    /// </summary>
    public sealed class CertificateClient : ICertificateServices<EndpointRegistrationModel> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public CertificateClient(IMethodClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetEndpointCertificateAsync(
            EndpointRegistrationModel registration, CancellationToken ct) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (registration.Endpoint == null) {
                throw new ArgumentNullException(nameof(registration.Endpoint));
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentNullException(nameof(registration.SupervisorId));
            }

            var deviceId = SupervisorModelEx.ParseDeviceId(registration.SupervisorId,
                out var moduleId);

            var sw = Stopwatch.StartNew();
            var result = await _client.CallMethodAsync(deviceId, moduleId,
                 "GetEndpointCertificate_V2",
                JsonConvertEx.SerializeObject(registration.Endpoint), null, ct);
            _logger.Debug("Calling supervisor {deviceId}/{moduleId} to get certificate." +
                "Took {elapsed} ms and returned {result}!", deviceId, moduleId,
                sw.ElapsedMilliseconds, result);
            return JsonConvertEx.DeserializeObject<byte[]>(result);
        }

        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
