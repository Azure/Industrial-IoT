// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server discovery on top of OPC UA endpoint discovery
    /// </summary>
    public sealed class ServerDiscovery : IServerDiscovery
    {
        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="identity"></param>
        public ServerDiscovery(IEndpointDiscovery client, IJsonSerializer serializer,
            IProcessInfo identity)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct)
        {
            if (query?.DiscoveryUrl == null)
            {
                throw new ArgumentException("Discovery url missing", nameof(query));
            }

            var discoveryUrl = new Uri(query.DiscoveryUrl);

            // Find endpoints at the real accessible ip address
            var eps = await _client.FindEndpointsAsync(discoveryUrl, null,
                ct).ConfigureAwait(false);

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
                return ep.ToServiceModel(discoveryUrl.Host,
                    _identity.SiteId, _identity.ProcessId, _identity.Id, _serializer);
            }
            throw new ResourceNotFoundException("Endpoints could not be found.");
        }

        private readonly IJsonSerializer _serializer;
        private readonly IProcessInfo _identity;
        private readonly IEndpointDiscovery _client;
    }
}
