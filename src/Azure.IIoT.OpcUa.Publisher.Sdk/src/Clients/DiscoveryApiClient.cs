// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC Publihser module receiving service requests via device method calls.
    /// </summary>
    public sealed class DiscoveryApiClient : IDiscoveryApi
    {
        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="target"></param>
        /// <param name="serializer"></param>
        public DiscoveryApiClient(IMethodClient methodClient, string target,
             IJsonSerializer serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            _target = target;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public DiscoveryApiClient(IMethodClient methodClient,
            ISdkConfig config = null, IJsonSerializer serializer = null) :
            this(methodClient, config?.Target, serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "GetEndpointCertificate_V2", _serializer.SerializeToMemory(endpoint),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<X509CertificateChainModel>(response);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_target,
                "Cancel_V2", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_target,
                "Discover_V2", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_target,
                "Register_V2", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "FindServer_V2", _serializer.SerializeToMemory(query),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ApplicationRegistrationModel>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
    }
}
