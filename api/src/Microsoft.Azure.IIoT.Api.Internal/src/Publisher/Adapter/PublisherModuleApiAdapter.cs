// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Publisher.Adapter {
    using Microsoft.Azure.IIoT.Api;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
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
        public PublisherModuleApiAdapter(IPublisherModuleApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetEndpointCertificateAsync(EndpointModel endpoint,
            CancellationToken ct) {
            var result = await _client.GetEndpointCertificateAsync(endpoint, ct);
            return result;
        }

        private readonly IPublisherModuleApi _client;
    }
}
