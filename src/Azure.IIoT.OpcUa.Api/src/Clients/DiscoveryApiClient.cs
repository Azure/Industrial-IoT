// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Clients {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC Publihser module receiving service requests via device method calls.
    /// </summary>
    public sealed class DiscoveryApiClient : IDiscoveryApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public DiscoveryApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId;
            _deviceId = deviceId;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public DiscoveryApiClient(IMethodClient methodClient,
            IModuleApiConfig config = null, IJsonSerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "GetEndpointCertificate_V2", _serializer.SerializeToString(endpoint), null, ct);
            return _serializer.Deserialize<X509CertificateChainModel>(response);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelModel request,
            CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Cancel_V2", _serializer.SerializeToString(request), null, ct);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Discover_V2", _serializer.SerializeToString(request), null, ct);
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Register_V2", _serializer.SerializeToString(request), null, ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "FindServer_V2", _serializer.SerializeToString(query), null, ct);
            return _serializer.Deserialize<ApplicationRegistrationModel>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
