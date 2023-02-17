// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Publisher.Adapter {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of twin api.
    /// </summary>
    public sealed class PublisherModuleApiAdapter : ICertificateServices<EndpointModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public PublisherModuleApiAdapter(IDiscoveryApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct) {
            var result = await _client.GetEndpointCertificateAsync(endpoint, ct);
            return result;
        }

        private readonly IDiscoveryApi _client;
    }
}
