// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Publisher interaction
/// </summary>
internal sealed class Publisher
{
    /// <summary>
    /// Endpoint urls
    /// </summary>
    public HashSet<string> Endpoints { get; } = new HashSet<string>();

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public string? PnJson { get; set; }

    /// <summary>
    /// Addresses
    /// </summary>
    public HashSet<IPAddress> Addresses { get; } = new HashSet<IPAddress>();

    public Publisher(ILogger logger, HttpClient httpClient, string? opcServerEndpoint = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _opcServerEndpoint = opcServerEndpoint;
    }

    /// <summary>
    /// Try update the endpoints and addresses from the publisher.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<bool> TryUpdateEndpointsAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving endpoints from publisher...");

            // Get and endpoint url to monitor if not set
            var configuration = await _httpClient.GetFromJsonAsync<JsonElement>(
                "v2/configuration", JsonSerializerOptions.Default, ct).ConfigureAwait(false);
            PnJson = JsonSerializer.Serialize(configuration, CmdLine.Indented);
            foreach (var endpoint in configuration.GetProperty("endpoints").EnumerateArray())
            {
                var endpointUrl = endpoint.GetProperty("EndpointUrl").GetString();
                if (!string.IsNullOrWhiteSpace(endpointUrl))
                {
                    Endpoints.Add(endpointUrl);
                }
            }

            // Narrow endpoints to a single one that was configured
            if (_opcServerEndpoint != null)
            {
                if (!Endpoints.Contains(_opcServerEndpoint))
                {
                    _logger.LogInformation(
                        "Desired endpoint {Endpoint} not found in configuration.",
                        _opcServerEndpoint);
                    return false;
                }
                Endpoints.Clear();
                // Select just the endpoint and continue
                Endpoints.Add(_opcServerEndpoint);
            }

            // Resolve addresses
            Addresses.Clear();
            foreach (var e in Endpoints)
            {
                var uri = new Uri(e, UriKind.Absolute);
                var a = await Dns.GetHostAddressesAsync(uri.Host, ct).ConfigureAwait(false);
                Addresses.UnionWith(a);
            }

            if (Addresses.Count == 0)
            {
                _logger.LogInformation("No addresses found for {Endpoints} - waiting .....",
                    Endpoints.Count == 0 ? "none" : string.Join(",", Endpoints.ToArray()));
                return false;
            }
            _logger.LogInformation("Retrieved endpoints from publisher.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update endpoints from publisher.");
            return false;
        }
    }

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _opcServerEndpoint;
}
