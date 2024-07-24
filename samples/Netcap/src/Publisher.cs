// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

/// <summary>
/// Publisher interaction
/// </summary>
internal sealed class Publisher
{
    /// <summary>
    /// Endpoint urls
    /// </summary>
    public HashSet<string> Endpoints { get; } = new();

    /// <summary>
    /// Addresses of the publisher on the network
    /// </summary>
    public HashSet<IPAddress> Addresses { get; } = new();

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public string? PnJson { get; set; }

    /// <summary>
    /// Create publisher
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="httpClient"></param>
    /// <param name="publisherIpAddresses"></param>
    public Publisher(ILogger logger, HttpClient httpClient, string? publisherIpAddresses)
    {
        _logger = logger;
        _httpClient = httpClient;

        if (!string.IsNullOrWhiteSpace(publisherIpAddresses))
        {
            foreach (var address in publisherIpAddresses.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (IPAddress.TryParse(address, out var ip))
                {
                    Addresses.Add(ip);
                }
            }
        }
    }

    /// <summary>
    /// Monitor publisher
    /// </summary>
    /// <param name="diagnostics"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask MonitorPublisherAsync(Func<JsonElement, Task> diagnostics,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Watch session diagnostics while we capture
            try
            {
                _logger.LogInformation("Monitoring diagnostics at {Url}...", _httpClient.BaseAddress);
                await foreach (var diagnostic in _httpClient.GetFromJsonAsAsyncEnumerable<JsonElement>(
                    "v2/diagnostics/connections/watch",
                        cancellationToken: ct).ConfigureAwait(false))
                {
                    await diagnostics(diagnostic).ConfigureAwait(false);
                }
                _logger.LogInformation("Restart monitoring diagnostics...");
            }
            catch (OperationCanceledException) { } // Done
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring diagnostics - restarting...");
            }
        }
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
            _logger.LogInformation("Retrieving endpoints from publisher on {Url}...",
                _httpClient.BaseAddress);

            // Stop and endpoint url to monitor if not set
            var configuration = await _httpClient.GetFromJsonAsync<JsonElement>(
                "v2/configuration?includeNodes=true",
                JsonSerializerOptions.Default, ct).ConfigureAwait(false);
            PnJson = JsonSerializer.Serialize(configuration, Extensions.Indented);
            foreach (var endpoint in configuration.GetProperty("endpoints").EnumerateArray())
            {
                var endpointUrl = endpoint.GetProperty("EndpointUrl").GetString();
                if (!string.IsNullOrWhiteSpace(endpointUrl))
                {
                    Endpoints.Add(endpointUrl);
                }
            }

            if (Endpoints.Count == 0)
            {
                _logger.LogInformation("No endpoints found in configuration - waiting....");
                return false;
            }
            _logger.LogInformation("Retrieved {Count} endpoints from publisher.",
                Endpoints.Count);
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
}
