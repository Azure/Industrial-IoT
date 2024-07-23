// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.IO.Compression;
using System.Text;
using System;

/// <summary>
/// Network capture bundle containing everythig to analyze
/// the OPC Publisher network traffic
/// </summary>
internal sealed class Bundle
{
    /// <summary>
    /// Put of capture
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// End of capture
    /// </summary>
    public DateTimeOffset End { get; private set; }

    /// <summary>
    /// CreateSidecarDeployment capture device
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="folder"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public Bundle(ILogger logger, string folder,
        DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        _logger = logger;
        _folder = folder;

        Start = start ?? DateTimeOffset.MinValue;
        End = end ?? DateTimeOffset.MaxValue;

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
        Directory.CreateDirectory(_folder);
    }

    /// <summary>
    /// Collect traces
    /// </summary>
    /// <param name="publisher"></param>
    /// <param name="index"></param>
    /// <param name="itf"></param>
    /// <param name="hostCaptureEndpointUrl"></param>
    /// <returns></returns>
    public Pcap AddPcap(Publisher publisher, int index,
        Pcap.InterfaceType itf = Pcap.InterfaceType.AnyIfAvailable,
        string? hostCaptureEndpointUrl = null)
    {
        if (publisher.PnJson != null)
        {
            // Add pn.json
            File.WriteAllText(Path.Combine(_folder, "pn.json"), publisher.PnJson);
        }

        var file = Path.Combine(_folder, $"capture{index}.pcap");

        // Capture filter
        // https://www.wireshark.org/docs/man-pages/pcap-filter.html
        // src or dst host 192.168.80.2
        // "ip and tcp and not port 80 and not port 25";
        // TODO: Filter on src/dst of publisher ip
        Pcap pcap;
        var addresses = publisher.Addresses;
        if (addresses.Count == 0)
        {
            pcap = new Pcap(_logger, new Pcap.CaptureConfiguration(file, itf, "ip and tcp"),
                hostCaptureEndpointUrl);
        }
        else
        {
            var filter = "src or dst host " + ((addresses.Count == 1) ? addresses.First() :
                ("(" + string.Join(" or ", addresses.Select(a => $"{a}")) + ")"));
            pcap = new Pcap(_logger, new Pcap.CaptureConfiguration(file, itf, filter),
                hostCaptureEndpointUrl);
        }
        _captures.Add(pcap);
        return pcap;
    }

    /// <summary>
    /// Add an extra pcap
    /// </summary>
    /// <param name="pcap"></param>
    public void AddExtraPcap(Pcap pcap)
    {
        _captures.Add(pcap);
    }

    /// <summary>
    /// GetAndStop bundle file
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetBundleFile(string? name = null)
    {
        foreach (var capture in _captures)
        {
            if (capture.Open)
            {
                capture.Dispose();
            }
            if (Path.GetDirectoryName(capture.File) != _folder)
            {
                var additionalcaptures = Path.Combine(_folder, "extra");
                if (!Directory.Exists(additionalcaptures))
                {
                    Directory.CreateDirectory(additionalcaptures);
                }
                File.Copy(capture.File, Path.Combine(additionalcaptures,
                    Path.GetFileName(capture.File)));
            }
            if (capture.End > End)
            {
                End = capture.End.Value;
            }
            if (capture.Start < Start)
            {
                End = capture.Start;
            }
        }
        var zipFile = Path.Combine(Path.GetTempPath(), name ?? "capture-bundle.zip");
        ZipFile.CreateFromDirectory(_folder, zipFile, CompressionLevel.SmallestSize,
            false, entryNameEncoding: Encoding.UTF8);
        return zipFile;
    }

    /// <summary>
    /// Writes the key log file
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="channelId"></param>
    /// <param name="tokenId"></param>
    /// <param name="clientIv"></param>
    /// <param name="clientKey"></param>
    /// <param name="clientSigLen"></param>
    /// <param name="serverIv"></param>
    /// <param name="serverKey"></param>
    /// <param name="serverSigLen"></param>
    public async ValueTask AddSessionKeysAsync(string fileName, uint channelId,
        uint tokenId, byte[] clientIv, byte[] clientKey, int clientSigLen,
        byte[] serverIv, byte[] serverKey, int serverSigLen)
    {
        var keysets = File.AppendText(Path.Combine(_folder, fileName));
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

            await keysets.FlushAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Add published nodes json configuration
    /// </summary>
    /// <param name="pnJson"></param>
    /// <returns></returns>
    public async Task AddPublishedNodesConfigurationAsync(string pnJson)
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "pn.json"), pnJson)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Add session keys to capture bundle from publisher diagnostics
    /// </summary>
    /// <param name="diagnostic"></param>
    /// <param name="endpointFilter"></param>
    /// <returns></returns>
    public async Task AddSessionKeysFromDiagnosticsAsync(JsonElement diagnostic,
        HashSet<string> endpointFilter)
    {
        var diagnosticJson = JsonSerializer.Serialize(diagnostic, Main.Indented);

        if (diagnostic.TryGetProperty("connection", out var conn) &&
            conn.TryGetProperty("endpoint", out var ep) &&
            ep.TryGetProperty("url", out var url) &&
            diagnostic.TryGetProperty("sessionCreated", out var sessionCreatedToken) &&
            sessionCreatedToken.TryGetDateTimeOffset(out var sessionCreated) &&
            diagnostic.TryGetProperty("remotePort", out var remotePortToken) &&
            remotePortToken.TryGetInt32(out var remotePort) &&
            diagnostic.TryGetProperty("sessionId", out var sessionId) &&
            sessionId.GetString() != null &&
            diagnostic.TryGetProperty("channelDiagnostics", out var channel) &&
            channel.TryGetProperty("channelId", out var channelIdToken) &&
            channelIdToken.TryGetUInt32(out var channelId) &&
            channel.TryGetProperty("tokenId", out var tokenIdToken) &&
            tokenIdToken.TryGetUInt32(out var tokenId) &&
            channel.TryGetProperty("client", out var clientToken) &&
            clientToken.TryGetProperty("iv", out var clientIvToken) &&
            clientIvToken.TryGetBytes(out var clientIv) &&
            clientToken.TryGetProperty("key", out var clientKeyToken) &&
            clientKeyToken.TryGetBytes(out var clientKey) &&
            clientToken.TryGetProperty("sigLen", out var clientSigLenToken) &&
            clientSigLenToken.TryGetInt32(out var clientSigLen) &&
            channel.TryGetProperty("server", out var serverToken) &&
            serverToken.TryGetProperty("iv", out var serverIvToken) &&
            serverIvToken.TryGetBytes(out var serverIv) &&
            serverToken.TryGetProperty("key", out var serverKeyToken) &&
            serverKeyToken.TryGetBytes(out var serverKey) &&
            serverToken.TryGetProperty("sigLen", out var serverSigLenToken) &&
            serverSigLenToken.TryGetInt32(out var serverSigLen))
        {
            if (!endpointFilter.Contains(url.GetString() ?? string.Empty))
            {
                return;
            }
            // Add session keys to the endpoint capture
            var sid = sessionId.GetString()!;
            var fileName = $"{remotePort}_{string.Join(
                '_', (sid + sessionCreated).Split(Path.GetInvalidFileNameChars(),
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))}";
            await AddSessionKeysAsync(fileName + ".txt", channelId,
                tokenId, clientIv, clientKey, clientSigLen, serverIv, serverKey,
                serverSigLen).ConfigureAwait(false);

            await File.AppendAllTextAsync(Path.Combine(_folder, fileName + ".log"), diagnosticJson)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Delete bundle
    /// </summary>
    public void Delete()
    {
        Directory.Delete(_folder, true);
    }

    private readonly List<Pcap> _captures = new();
    private readonly ILogger _logger;
    private readonly string _folder;
}
