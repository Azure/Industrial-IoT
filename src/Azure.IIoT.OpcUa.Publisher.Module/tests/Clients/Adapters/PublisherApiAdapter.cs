// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Service.Clients.Adapters
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of twin api.
    /// </summary>
    public sealed class PublisherApiAdapter : ICertificateServices<EndpointModel>
    {
        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public PublisherApiAdapter(IDiscoveryApi client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct)
        {
            return await _client.GetEndpointCertificateAsync(endpoint, ct).ConfigureAwait(false);
        }

        private readonly IDiscoveryApi _client;
    }
}
