// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
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
        public DiscoveryApiClient(IMethodClient methodClient, string target,
            TimeSpan? timeout = null)
        {
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
        public DiscoveryApiClient(IMethodClient methodClient,
            IOptions<SdkOptions> options) :
            this(methodClient, options.Value.Target!, options.Value.Timeout)
        {
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            var response = await _methodClient.CallMethodAsync(_target,
                "GetEndpointCertificate_V2", Json.SerializeToMemory(endpoint),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<X509CertificateChainModel>();
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Cancel_V2", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Discover_V2", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Register_V2", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            var response = await _methodClient.CallMethodAsync(_target,
                "FindServer_V2", Json.SerializeToMemory(query),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<ApplicationRegistrationModel>();
        }

        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
