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
using System.Net;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

/// <summary>
/// Capture traffic from any ethernet device
/// </summary>
internal sealed class Pcap : IDisposable
{
    /// <summary>
    /// Create capture device
    /// </summary>
    private Pcap(ILogger logger, string folder, string? filter = null)
    {
        _logger = logger;

        _logger.LogInformation("Using SharpPcap {Version}",
            SharpPcap.Pcap.SharpPcapVersion);

        if (LibPcapLiveDeviceList.Instance.Count == 0)
        {
            throw new NotSupportedException("Cannot run capture without devices.");
        }

        _logger.LogInformation("Capturing to {FileName} with filter {Filter}.",
            folder, filter);

        _writer = new CaptureFileWriterDevice(Path.Combine(folder, "capture.pcap"));
        _path = folder;

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
                if (filter != null)
                {
                    device.Filter = filter;
                }

                _logger.LogInformation("Start capturing from {Description} ({Link}).",
                    device.Description, device.LinkType);
                device.OnPacketArrival += (_, e) => _writer.Write(e.GetPacket());
                capturing.Add(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to capture with device {Device} ({Description}).",
                    device.Name, device.Description);
            }
        }

        _writer.Open(new DeviceConfiguration
        {
            LinkLayerType = PacketDotNet.LinkLayers.Null //Ethernet
        });

        capturing.ForEach(d => d.StartCapture());
    }

    /// <summary>
    /// Start capturing
    /// </summary>
    public static Pcap Capture(ISet<IPAddress> addresses, string folder, ILogger logger)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
        Directory.CreateDirectory(folder);

        // Create filter
        // https://www.wireshark.org/docs/man-pages/pcap-filter.html
        // src or dst host 192.168.80.2
        // "ip and tcp and not port 80 and not port 25";

        var filter = "src or dst host " + ((addresses.Count == 1) ? addresses.First() :
            ("(" + string.Join(" or ", addresses.Select(a => $"{a}")) + ")"));

        return new Pcap(logger, folder, filter);
    }

    /// <summary>
    /// Writes the key log file
    /// </summary>
    public async ValueTask AddSessionKeysAsync(string fileName, uint channelId,
        uint tokenId, byte[] clientIv, byte[] clientKey, int clientSigLen,
        byte[] serverIv, byte[] serverKey, int serverSigLen)
    {
        var keysets = File.AppendText(Path.Combine(_path, fileName));
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
    /// Add session keys to capture bundle from publisher diagnostics
    /// </summary>
    /// <param name="publisher"></param>
    /// <param name="capture"></param>
    /// <param name="diagnostic"></param>
    /// <returns></returns>
    public async Task AddSessionKeysFromDiagnosticsAsync(JsonElement diagnostic,
        HashSet<string> endpointFilter)
    {
        var diagnosticJson = JsonSerializer.Serialize(diagnostic, Parameters.Indented);

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

            await File.AppendAllTextAsync(Path.Combine(_path, fileName + ".log"), diagnosticJson)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            _devices.ForEach(d =>
            {
                _logger.LogInformation(
                    "Stopped capturing {Device} ({Description}) to file ({Statistics}).",
                d.Name, d.Description, d.Statistics.ToString());
                try
                {
                    d.StopCapture();
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
    private readonly string _path;
    private readonly ILogger _logger;
}
