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
    using Microsoft.Extensions.Options;
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
        /// <param name="timeout"></param>
        /// <param name="serializer"></param>
        public DiscoveryApiClient(IMethodClient methodClient, string target,
            TimeSpan? timeout = null, IJsonSerializer? serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }
            _target = target;
            _timeout = timeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public DiscoveryApiClient(IMethodClient methodClient,
            IOptions<SdkOptions> options, IJsonSerializer? serializer = null) :
            this(methodClient, options.Value.Target!, options.Value.Timeout,
                serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            var response = await _methodClient.CallMethodAsync(_target,
                "GetEndpointCertificate_V2", _serializer.SerializeToMemory(endpoint),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<X509CertificateChainModel>(response);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Cancel_V2", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Discover_V2", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Register_V2", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            var response = await _methodClient.CallMethodAsync(_target,
                "FindServer_V2", _serializer.SerializeToMemory(query),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<ApplicationRegistrationModel>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
