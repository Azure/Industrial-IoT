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
internal sealed class Publisher : IDisposable
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
    /// Create publisher
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="publisherIpAddresses"></param>
    /// <param name="logger"></param>
    public Publisher(HttpClient httpClient, string? publisherIpAddresses, ILogger logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);

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

    /// <inheritdoc>
    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    /// <summary>
    /// Collect traces
    /// </summary>
    /// <param name="itf"></param>
    /// <param name="maxPcapFileSize"></param>
    /// <param name="maxPcapDuration"></param>
    /// <returns></returns>
    public Pcap.CaptureConfiguration GetCaptureConfiguration(
        Pcap.InterfaceType itf = Pcap.InterfaceType.AnyIfAvailable,
        int? maxPcapFileSize = null, TimeSpan? maxPcapDuration = null)
    {
        // Base filter
        // https://www.wireshark.org/docs/man-pages/pcap-filter.html
        // src or dst host 192.168.80.2
        // "ip and tcp and not port 80 and not port 25";
        // TODO: Filter on src/dst of publisher ip
        var addresses = Addresses
            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToList();
        if (addresses.Count == 0)
        {
            return new Pcap.CaptureConfiguration(itf, "ip and tcp",
                maxPcapFileSize, maxPcapDuration);
        }
        var filter = "src or dst host " + ((addresses.Count == 1) ? addresses.First() :
            ("(" + string.Join(" or ", addresses.Select(a => $"{a}")) + ")"));

        return new Pcap.CaptureConfiguration(itf, filter, maxPcapFileSize, maxPcapDuration);
    }

    /// <summary>
    /// Monitor publisher
    /// </summary>
    /// <param name="storage"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask UploadChannelLogsAsync(Storage storage, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            // Watch session diagnostics while we capture
            try
            {
                _logger.LogInformation("Monitoring channels at {Url}...", _httpClient.BaseAddress);
                await foreach (var diagnostic in _httpClient.GetFromJsonAsAsyncEnumerable<JsonElement>(
                    "v2/diagnostics/channels/watch",
                        cancellationToken: ct).ConfigureAwait(false))
                {
                    try
                    {
                        await UploadSessionKeysFromDiagnosticsAsync(storage,
                            diagnostic, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading session keys.");
                    }
                }
                _logger.LogInformation("Restart monitoring channel diagnostics...");
            }
            catch (OperationCanceledException) { } // Done
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring channel diagnostics - restarting...");
            }
        }
    }

    /// <summary>
    /// Try update the endpoints and addresses from the publisher.
    /// </summary>
    /// <param name="storage"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<bool> TryUploadEndpointsAsync(Storage storage,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Retrieving endpoints from publisher on {Url}...",
                _httpClient.BaseAddress);

            // Stop and endpoint url to monitor if not set
            var configuration = await _httpClient.GetFromJsonAsync<JsonElement>(
                "v2/configuration?includeNodes=true",
                JsonSerializerOptions.Default, ct).ConfigureAwait(false);
            var pnJson = JsonSerializer.Serialize(configuration, Extensions.Indented);
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

            var pnJsonFile = Path.Combine(_folder, "pn.json");
            await File.WriteAllTextAsync(pnJsonFile, pnJson, ct).ConfigureAwait(false);
            await storage.UploadAsync(pnJsonFile, ct).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update endpoints from publisher.");
        }
        return false;
    }

    /// <summary>
    /// Upload session keys to storage from publisher diagnostics
    /// </summary>
    /// <param name="storage"></param>
    /// <param name="diagnostic"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task UploadSessionKeysFromDiagnosticsAsync(Storage storage,
        JsonElement diagnostic, CancellationToken ct = default)
    {
        var diagnosticJson = JsonSerializer.Serialize(diagnostic, Extensions.Indented);

        if (diagnostic.TryGetProperty("connection", out var conn) &&
            conn.TryGetProperty("endpoint", out var ep) &&
            ep.TryGetProperty("url", out var url) &&
            diagnostic.TryGetProperty("sessionCreated", out var sessionCreatedToken) &&
            sessionCreatedToken.TryGetDateTimeOffset(out var sessionCreated) &&
            diagnostic.TryGetProperty("remotePort", out var remotePortToken) &&
            remotePortToken.TryGetInt32(out var remotePort) &&
            diagnostic.TryGetProperty("sessionId", out var sessionId) &&
            sessionId.GetString() != null &&
            diagnostic.TryGetProperty("channelId", out var channelIdToken) &&
            channelIdToken.TryGetUInt32(out var channelId) &&
            diagnostic.TryGetProperty("tokenId", out var tokenIdToken) &&
            tokenIdToken.TryGetUInt32(out var tokenId) &&
            diagnostic.TryGetProperty("client", out var clientToken) &&
            clientToken.TryGetProperty("iv", out var clientIvToken) &&
            clientIvToken.TryGetBytes(out var clientIv) &&
            clientToken.TryGetProperty("key", out var clientKeyToken) &&
            clientKeyToken.TryGetBytes(out var clientKey) &&
            clientToken.TryGetProperty("sigLen", out var clientSigLenToken) &&
            clientSigLenToken.TryGetInt32(out var clientSigLen) &&
            diagnostic.TryGetProperty("server", out var serverToken) &&
            serverToken.TryGetProperty("iv", out var serverIvToken) &&
            serverIvToken.TryGetBytes(out var serverIv) &&
            serverToken.TryGetProperty("key", out var serverKeyToken) &&
            serverKeyToken.TryGetBytes(out var serverKey) &&
            serverToken.TryGetProperty("sigLen", out var serverSigLenToken) &&
            serverSigLenToken.TryGetInt32(out var serverSigLen))
        {
            // Add session keys to the endpoint capture
            var sid = sessionId.GetString()!;
            var name = Extensions.FixFileName(sid + sessionCreated);
            var filePath = Path.Combine(_folder, $"{remotePort}_{name}");

            var keyFile = filePath + ".txt";
            await AddSessionKeysAsync(keyFile, channelId,
                tokenId, clientIv, clientKey, clientSigLen, serverIv, serverKey,
                serverSigLen, ct).ConfigureAwait(false);
            await storage.UploadAsync(keyFile, ct).ConfigureAwait(false);

            static async ValueTask AddSessionKeysAsync(string fileName, uint channelId,
                 uint tokenId, byte[] clientIv, byte[] clientKey, int clientSigLen,
                 byte[] serverIv, byte[] serverKey, int serverSigLen,
                 CancellationToken ct = default)
            {
                var keysets = File.AppendText(fileName);
                await using (var _ = keysets.ConfigureAwait(false))
                {
                    await keysets.WriteLineAsync(
$"client_iv_{channelId}_{tokenId}: {Convert.ToHexString(clientIv)}").ConfigureAwait(false);
                    await keysets.WriteLineAsync(
$"client_key_{channelId}_{tokenId}: {Convert.ToHexString(clientKey)}").ConfigureAwait(false);
                    await keysets.WriteLineAsync(
$"client_siglen_{channelId}_{tokenId}: {clientSigLen}").ConfigureAwait(false);
                    await keysets.WriteLineAsync(
$"server_iv_{channelId}_{tokenId}: {Convert.ToHexString(serverIv)}").ConfigureAwait(false);
                    await keysets.WriteLineAsync(
$"server_key_{channelId}_{tokenId}: {Convert.ToHexString(serverKey)}").ConfigureAwait(false);
                    await keysets.WriteLineAsync(
$"server_siglen_{channelId}_{tokenId}: {serverSigLen}").ConfigureAwait(false);

                    await keysets.FlushAsync(ct).ConfigureAwait(false);
                }
            }

            var logFile = filePath + ".log";
            await File.AppendAllTextAsync(logFile, diagnosticJson, ct)
                .ConfigureAwait(false);
            await storage.UploadAsync(logFile, ct).ConfigureAwait(false);
        }
    }

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _folder;
}
