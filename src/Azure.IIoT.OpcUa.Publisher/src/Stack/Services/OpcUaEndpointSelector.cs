// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Selects an OPC UA endpoint from a server discovery response.
    /// </summary>
    internal interface IOpcUaEndpointSelector
    {
        /// <summary>
        /// Select an endpoint.
        /// </summary>
        Task<EndpointDescription?> SelectAsync(ApplicationConfiguration configuration,
            Uri? discoveryUrl, ITransportWaitingConnection? connection,
            SecurityMode securityMode, string? securityPolicy, ILogger logger,
            object? context, string? endpointUrl = null, CancellationToken ct = default);
    }

    /// <summary>
    /// Default OPC UA endpoint selector.
    /// </summary>
    internal sealed class OpcUaEndpointSelector : IOpcUaEndpointSelector
    {
        /// <summary>
        /// Shared selector instance.
        /// </summary>
        public static OpcUaEndpointSelector Instance { get; } = new();

        /// <inheritdoc/>
        public async Task<EndpointDescription?> SelectAsync(
            ApplicationConfiguration configuration, Uri? discoveryUrl,
            ITransportWaitingConnection? connection, SecurityMode securityMode,
            string? securityPolicy, ILogger logger, object? context,
            string? endpointUrl = null, CancellationToken ct = default)
        {
            var endpointConfiguration = EndpointConfiguration.Create();
            endpointConfiguration.OperationTimeout =
                (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

            // needs to add the /discovery onto http urls
            if (connection == null)
            {
                if (discoveryUrl == null)
                {
                    return null;
                }
                if (discoveryUrl.Scheme == Utils.UriSchemeHttp &&
                    !discoveryUrl.AbsolutePath.EndsWith("/discovery",
                        StringComparison.OrdinalIgnoreCase))
                {
                    discoveryUrl = new UriBuilder(discoveryUrl)
                    {
                        Path = discoveryUrl.AbsolutePath.TrimEnd('/') + "/discovery"
                    }.Uri;
                }
            }

            using var client = connection != null ?
                DiscoveryClient.Create(configuration, connection, endpointConfiguration) :
                DiscoveryClient.Create(configuration, discoveryUrl, endpointConfiguration);
            var uri = new Uri(endpointUrl ?? client.Endpoint.EndpointUrl);
            var endpoints = (await client.GetEndpointsAsync(default, ct).ConfigureAwait(false))
                .ToArray() ?? [];
            discoveryUrl ??= uri;

            return SelectEndpoint(endpoints, uri, discoveryUrl, connection != null,
                securityMode, securityPolicy, logger, context);
        }

        /// <summary>
        /// Select from an already discovered endpoint set.
        /// </summary>
        internal static EndpointDescription? SelectEndpoint(
            IReadOnlyList<EndpointDescription> endpoints, Uri endpointUri,
            Uri discoveryUrl, bool reverseConnect, SecurityMode securityMode,
            string? securityPolicy, ILogger logger, object? context)
        {
            var ctx = context?.ToString() ?? "null";
            logger.DiscoveryEndpointReturnedEndpoints(ctx,
                discoveryUrl, endpointUri, securityMode, securityPolicy ?? "any", endpoints
                    .Select(ep => "      " + Format(ep))
                    .DefaultIfEmpty("      (none)")
                    .Aggregate((a, b) => $"{a}\n{b}"));

            var filtered = endpoints
                .Where(ep =>
                    SecurityPolicies.Default.GetDisplayName(ep.SecurityPolicyUri) != null &&
                    ep.SecurityMode.IsSame(securityMode) &&
                    (securityPolicy == null ||
                     string.Equals(ep.SecurityPolicyUri,
                        securityPolicy,
                        StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(ep.SecurityPolicyUri,
                        "http://opcfoundation.org/UA/SecurityPolicy#" + securityPolicy,
                        StringComparison.OrdinalIgnoreCase)))
                //
                // The security level is a relative measure assigned by the server
                // to the endpoints that it returns. Clients should always pick the
                // highest level unless they have a reason not too. Some servers
                // however, mess this up a bit. So group SecurityLevel also by
                // security mode and then pick the highest in that group.
                //
                .OrderByDescending(ep => ((int)ep.SecurityMode << 8) | ep.SecurityLevel)
                .ToList();

            //
            // Try to find endpoint that matches scheme and endpoint url path
            // but fall back to match just the scheme. We need to match only
            // scheme to support the reverse connect (indicated by connection
            // being not null here).
            //
            var selected = filtered.Find(ep => Match(ep, endpointUri, true, true))
                        ?? filtered.Find(ep => Match(ep, endpointUri, true, false));
            if (reverseConnect)
            {
                //
                // Only allow same uri scheme (which must also be opc.tcp)
                // for when reverse connection is used.
                //
                if (selected != null)
                {
                    logger.EndpointSelectedViaReverseConnect(ctx, Format(selected));
                }
                return selected;
            }

            if (selected == null)
            {
                //
                // Fall back to first supported endpoint matching absolute path
                // then fall back to first endpoint (backwards compatibilty)
                //
                selected = filtered.Find(ep => Match(ep, endpointUri, false, true))
                        ?? filtered.Find(ep => Match(ep, endpointUri, false, false));

                if (selected == null)
                {
                    return null;
                }
            }

            //
            // Adjust the host name and port to the host name and port
            // that was use to successfully connect the discovery client
            //
            var selectedUrl = Utils.ParseUri(selected.EndpointUrl);
            if (selectedUrl != null && selectedUrl.Scheme == discoveryUrl.Scheme)
            {
                selected.EndpointUrl = new UriBuilder(selectedUrl)
                {
                    Host = discoveryUrl.DnsSafeHost,
                    Port = discoveryUrl.Port
                }.ToString();
            }

            logger.EndpointSelected(ctx, Format(selected));
            return selected;
        }

        private static string Format(EndpointDescription endpoint) =>
            $"#{endpoint.SecurityLevel:000}: {endpoint.EndpointUrl}|" +
            $"{endpoint.SecurityMode} [{endpoint.SecurityPolicyUri}]";

        private static bool Match(EndpointDescription endpointDescription,
            Uri endpointUrl, bool includeScheme, bool includePath)
        {
            var url = Utils.ParseUri(endpointDescription.EndpointUrl);
            return url != null &&
                (!includeScheme || string.Equals(url.Scheme,
                    endpointUrl.Scheme, StringComparison.OrdinalIgnoreCase)) &&
                (!includePath || string.Equals(url.AbsolutePath,
                    endpointUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase));
        }

        private OpcUaEndpointSelector()
        {
        }
    }

    /// <summary>
    /// Source-generated logging definitions for endpoint selection.
    /// </summary>
    internal static partial class OpcUaEndpointSelectorLogging
    {
        private const int EventClass = 520;

        [LoggerMessage(EventId = EventClass + 60, Level = LogLevel.Information,
            Message = "{Context}: Discovery endpoint {DiscoveryUrl} returned endpoints. Selecting endpoint {EndpointUri} " +
            "with SecurityMode {SecurityMode} and {SecurityPolicy} SecurityPolicyUri from:\n{Endpoints}")]
        public static partial void DiscoveryEndpointReturnedEndpoints(this ILogger logger,
            string? context, Uri discoveryUrl, Uri endpointUri, SecurityMode securityMode,
            string securityPolicy, string endpoints);

        [LoggerMessage(EventId = EventClass + 61, Level = LogLevel.Information,
            Message = "{Context}: Endpoint {Endpoint} selected via reverse connect!")]
        public static partial void EndpointSelectedViaReverseConnect(this ILogger logger,
            string? context, string endpoint);

        [LoggerMessage(EventId = EventClass + 62, Level = LogLevel.Information,
            Message = "{Context}: Endpoint {Endpoint} selected!")]
        public static partial void EndpointSelected(this ILogger logger, string? context,
            string endpoint);
    }
}
