// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implement the discovery services through all registered publishers
    /// </summary>
    public sealed class DiscoveryServicesClient : INetworkDiscovery<string>,
        IServerDiscovery<string>, IDisposable
    {
        /// <summary>
        /// Create endpoint registry
        /// </summary>
        /// <param name="publishers"></param>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public DiscoveryServicesClient(IPublisherRegistry publishers, IMethodClient client,
            IJsonSerializer serializer, ILogger<DiscoveryServicesClient> logger)
        {
            _publishers = publishers ?? throw new ArgumentNullException(nameof(publishers));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request, string? context, CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity("CancelDiscovery");
            await foreach (var publisher in EnumeratePublishersAsync(context, ct).ConfigureAwait(false))
            {
                if (publisher.Id == null)
                {
                    _logger.LogWarning("Publisher id was unexpectedly null");
                    continue;
                }
                var client = new DiscoveryApiClient(_client, publisher.Id, kTimeout, _serializer);
                try
                {
                    await client.CancelAsync(request, ct).ConfigureAwait(false);

                    _logger.LogDebug("Cancelled discovery on publisher {Publisher}...", publisher.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to cancel discovery on publisher {Id}", publisher.Id);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(ServerEndpointQueryModel query,
            string? context, CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity("FindServer");
            var exceptions = new List<Exception>();
            await foreach (var publisher in EnumeratePublishersAsync(context, ct).ConfigureAwait(false))
            {
                if (publisher.Id == null)
                {
                    _logger.LogWarning("Publisher id was unexpectedly null");
                    continue;
                }
                var client = new DiscoveryApiClient(_client, publisher.Id, kTimeout, _serializer);
                try
                {
                    var discoveredResult = await client.FindServerAsync(query, ct).ConfigureAwait(false);
                    //
                    // Update discovery id to consistent value that matches the publisher
                    // that found the resource. This is the same we do when processing
                    // discovery results
                    //
                    discoveredResult.Application.DiscovererId = publisher.Id;
                    discoveredResult.Endpoints?.ForEach(ep => ep.DiscovererId = publisher.Id);
                    return discoveredResult;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to find server on publisher {Id}", publisher.Id);
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException("Failed to find server.", exceptions);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request, string? context, CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity("Discover");
            await foreach (var publisher in EnumeratePublishersAsync(context, ct).ConfigureAwait(false))
            {
                if (publisher.Id == null)
                {
                    _logger.LogWarning("Publisher id was unexpectedly null");
                    continue;
                }
                var client = new DiscoveryApiClient(_client, publisher.Id, kTimeout, _serializer);
                try
                {
                    // We call discovery on all publishers
                    await client.DiscoverAsync(request, ct).ConfigureAwait(false);

                    _logger.LogDebug("Started discover on publisher {Publisher}...", publisher.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to call discovery on publisher {Id}", publisher.Id);
                }
            }
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request, string? context,
            CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity("RegisterServer");
            await foreach (var publisher in EnumeratePublishersAsync(context, ct).ConfigureAwait(false))
            {
                if (publisher.Id == null)
                {
                    _logger.LogWarning("Publisher id was unexpectedly null");
                    continue;
                }
                var client = new DiscoveryApiClient(_client, publisher.Id, kTimeout, _serializer);
                try
                {
                    await client.RegisterAsync(request, ct).ConfigureAwait(false);

                    _logger.LogDebug("Registered server through publisher {Publisher}...",
                        publisher.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to register on publisher {Id}", publisher.Id);
                }
            }
        }

        /// <summary>
        /// Get a dedicated publisher or list all publishers
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async IAsyncEnumerable<PublisherModel> EnumeratePublishersAsync(string? id = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (id != null)
            {
                yield return await _publishers.GetPublisherAsync(id, ct: ct).ConfigureAwait(false);
            }
            else
            {
                string? continuationToken = null;
                do
                {
                    var result = await _publishers.ListPublishersAsync(continuationToken, false,
                        null, ct).ConfigureAwait(false);
                    if (result.Items != null)
                    {
                        foreach (var item in result.Items)
                        {
                            yield return item;
                        }
                    }
                    continuationToken = result.ContinuationToken;
                }
                while (continuationToken != null);
            }
        }

        private readonly IPublisherRegistry _publishers;
        private readonly IMethodClient _client;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private static readonly TimeSpan kTimeout = TimeSpan.FromSeconds(60);
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
