// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server discovery on top of OPC UA endpoint discovery
    /// </summary>
    public sealed class ServerDiscovery : IServerDiscovery<object>
    {
        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        public ServerDiscovery(IEndpointDiscovery client, IJsonSerializer serializer,
            IOptions<PublisherOptions> options)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, object? context, CancellationToken ct)
        {
            if (query?.DiscoveryUrl == null)
            {
                throw new ArgumentException("Discovery url missing", nameof(query));
            }

            var discoveryUrl = new Uri(query.DiscoveryUrl);

            // Find endpoints at the real accessible ip address
            var eps = await _client.FindEndpointsAsync(discoveryUrl,
                findServersOnNetwork: true, ct: ct).ConfigureAwait(false);

            // Match endpoints
            foreach (var ep in eps)
            {
                if ((ep.Description.SecurityMode.ToServiceType() ?? SecurityMode.None)
                    != (query.SecurityMode ?? SecurityMode.None))
                {
                    // no match
                    continue;
                }
                if (query.SecurityPolicy != null &&
                    query.SecurityPolicy != ep.Description.SecurityPolicyUri)
                {
                    // no match
                    continue;
                }
                if (query.Certificate != null &&
                    query.Certificate != ep.Description.ServerCertificate.ToThumbprint())
                {
                    // no match
                    continue;
                }
                return ep.ToServiceModel(discoveryUrl.Host, _options.Value.SiteId,
                    _options.Value.PublisherId ?? Constants.DefaultPublisherId, _serializer);
            }
            throw new ResourceNotFoundException("Endpoints could not be found.");
        }

        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IEndpointDiscovery _client;
    }
}
