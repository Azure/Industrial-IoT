// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using System;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

/// <summary>
/// Network capture bundle containing everythig to analyze
/// the OPC Publisher network traffic
/// </summary>
internal sealed class Bundle
{
    /// <summary>
    /// Start of capture
    /// </summary>
    public DateTimeOffset Start { get; private set; }

    /// <summary>
    /// End of capture
    /// </summary>
    public DateTimeOffset End { get; private set; }

    /// <summary>
    /// Create capture device
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
    /// <returns></returns>
    public IDisposable CaptureNetworkTraces(Publisher publisher, int index)
    {
        if (publisher.PnJson != null)
        {
            // Add pn.json
            File.WriteAllText(Path.Combine(_folder, "pn.json"), publisher.PnJson);
        }

        // Create filter
        // https://www.wireshark.org/docs/man-pages/pcap-filter.html
        // src or dst host 192.168.80.2
        // "ip and tcp and not port 80 and not port 25";

        var addresses = publisher.Addresses;
        var filter = "src or dst host " + ((addresses.Count == 1) ? addresses.First() :
            ("(" + string.Join(" or ", addresses.Select(a => $"{a}")) + ")"));
        return new Pcap(this, filter, index);
    }

    /// <summary>
    /// Get bundle file
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetBundleFile(string? name = null)
    {
        var zipFile = Path.Combine(Path.GetTempPath(), name ?? "capture-bundle.zip");
        ZipFile.CreateFromDirectory(_folder, zipFile);
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
        var diagnosticJson = JsonSerializer.Serialize(diagnostic, CmdLine.Indented);

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

    /// <summary>
    /// Tracing
    /// </summary>
    internal sealed class Pcap : IDisposable
    {
        /// <summary>
        /// Create pcap
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="filter"></param>
        /// <param name="index"></param>
        public Pcap(Bundle bundle, string filter, int index)
        {
            _bundle = bundle;
            _filter = filter;
            _logger = bundle._logger;

            _logger.LogInformation(
                "Using SharpPcap {Version}", SharpPcap.Pcap.SharpPcapVersion);

            if (LibPcapLiveDeviceList.Instance.Count == 0)
            {
                throw new NotSupportedException("Cannot run capture without devices.");
            }

            _writer = new CaptureFileWriterDevice(Path.Combine(_bundle._folder,
                $"capture{index}.pcap"));
            _devices = LibPcapLiveDeviceList.New().ToList();
            var capturing = new List<LibPcapLiveDevice>();
            foreach (var device in _devices)
            {
                try
                {
                    // Open the device for capturing
                    device.Open(mode: DeviceModes.None, 1000);
                    //if (!device.Loopback &&
                    //    device.LinkType != PacketDotNet.LinkLayers.Ethernet)
                    //{
                    //    continue;
                    //}
                    device.Filter = _filter;
                    _logger.LogInformation("Capture from {Description} ({Link})...",
                        device.Description, device.LinkType);
                    device.OnPacketArrival += (_, e) => _writer.Write(e.GetPacket());
                    capturing.Add(device);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to capture from device {Device} ({Description}).",
                        device.Name, device.Description);
                }
            }
            _writer.Open(new DeviceConfiguration
            {
                LinkLayerType = PacketDotNet.LinkLayers.Null //Ethernet
            });
            capturing.ForEach(d => d.StartCapture());
            _logger.LogInformation("    ... to {FileName} ({Filter}).",
                _bundle._folder, _filter);
            _bundle.Start = DateTimeOffset.UtcNow;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _bundle.End = DateTimeOffset.UtcNow;
                _devices.ForEach(d =>
                {
                    try
                    {
                        d.StopCapture();
                        _logger.LogInformation(
                            "Capturing {Description} completed. ({Statistics}).",
                            d.Description, d.Statistics.ToString());
                    }
                    catch { }
                });
            }
            finally
            {
                _writer.Dispose();
                _devices.ForEach(d => d.Dispose());
            }
        }

        private readonly List<LibPcapLiveDevice> _devices;
        private readonly CaptureFileWriterDevice _writer;
        private readonly Bundle _bundle;
        private readonly string _filter;
        private readonly ILogger _logger;
    }

    private readonly ILogger _logger;
    private readonly string _folder;
}
