// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using SharpPcap;
using SharpPcap.LibPcap;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Net.Http.Json;

/// <summary>
/// Pcap capture
/// </summary>
internal sealed class Pcap : IDisposable
{
    /// <summary>
    /// Pcap configuration
    /// </summary>
    /// <param name="File"></param>
    /// <param name="InterfaceType"></param>
    /// <param name="Filter"></param>
    public sealed record class CaptureConfiguration(string File,
        InterfaceType InterfaceType, string? Filter = null);

    public enum InterfaceType
    {
        AnyIfAvailable,
        AllButAny,
        EthernetOnly
    }

    /// <summary>
    /// Start of capture
    /// </summary>
    public DateTimeOffset Start { get; private set; }

    /// <summary>
    /// End of capture
    /// </summary>
    public DateTimeOffset? End { get; private set; }

    /// <summary>
    /// File or handle
    /// </summary>
    public string File => _configuration.File;

    /// <summary>
    /// Pcap handle is capturing
    /// </summary>
    public bool Open => End == null;

    /// <summary>
    /// Remote capture
    /// </summary>
    public bool Remote => _remoteCapture != null;

    /// <summary>
    /// Handle
    /// </summary>
    public int Handle { get; }

    /// <summary>
    /// Create pcap
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    /// <param name="hostCaptureClient"></param>
    public Pcap(ILogger logger, CaptureConfiguration configuration,
        HttpClient? hostCaptureClient = null)
    {
        _configuration = configuration;
        _remoteCapture = hostCaptureClient == null ? null :
            new Client(hostCaptureClient, logger);
        _logger = logger;

        if (_remoteCapture != null)
        {
            Handle = _remoteCapture.Start(_configuration);
            Start = DateTimeOffset.UtcNow;
        }
        else
        {
            Handle = Interlocked.Increment(ref _handles);
            _logger.LogInformation(
                "Using SharpPcap {Version}", SharpPcap.Pcap.SharpPcapVersion);

            if (LibPcapLiveDeviceList.Instance.Count == 0)
            {
                throw new NetcapException("Cannot run capture without devices.");
            }

            _writer = new CaptureFileWriterDevice(File);
            _devices = LibPcapLiveDeviceList.New().ToList();
            LocalCaptureStart();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!Open)
        {
            return;
        }
        try
        {
            if (_remoteCapture != null)
            {
                End = _remoteCapture.Stop(Handle, File, out var start);
                Start = start;
            }
            else
            {
                End = DateTimeOffset.UtcNow;
                LocalCaptureStop();
            }
        }
        finally
        {
            _writer?.Dispose();
            _devices?.ForEach(d => d.Dispose());
        }
    }

    /// <summary>
    /// Stop local capture
    /// </summary>
    private void LocalCaptureStop()
    {
        try
        {
            _devices?.ForEach(d =>
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
            if (_writer != null)
            {
                _logger.LogInformation("Completed capture ({Statistics}).",
                    _writer.Statistics);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete capture.");
            throw;
        }
    }

    /// <summary>
    /// Start local capture
    /// </summary>
    private void LocalCaptureStart()
    {
        Debug.Assert(_devices != null);
        Debug.Assert(_writer != null);
        // Open devices
        var open = _devices
            .Where(d =>
            {
                try
                {
                    _logger.LogInformation("Opening {Device} in promiscuous mode...", d);
                    d.Open(mode: DeviceModes.Promiscuous, 1000);
                }
                catch
                {
                    try
                    {
                        _logger.LogInformation("Fall back to normal mode...");
                        d.Open(mode: DeviceModes.None, 1000);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogInformation(
                            "Failed to open {Device} ({Description}): {Message}",
                            d.Name, d.Description, ex2.Message);
                    }
                }
                return d.Opened;
            })
            .ToList();

        var capturing = Array.Empty<LibPcapLiveDevice>();
        var linkType = PacketDotNet.LinkLayers.Null;
        var itf = _configuration.InterfaceType;
        if (itf == InterfaceType.AnyIfAvailable)
        {
            // Try to capture from cooked mode (https://wiki.wireshark.org/SLL)
            linkType = PacketDotNet.LinkLayers.LinuxSll;
            capturing = Capture(open.Where(d => d.LinkType == linkType));
            if (capturing.Length == 0)
            {
                itf = InterfaceType.AllButAny;
            }
        }
        if (itf == InterfaceType.EthernetOnly)
        {
            linkType = PacketDotNet.LinkLayers.Ethernet;
            capturing = Capture(open.Where(d => d.LinkType != linkType));
            if (capturing.Length == 0)
            {
                itf = InterfaceType.AllButAny;
            }
        }
        if (itf == InterfaceType.AllButAny)
        {
            linkType = PacketDotNet.LinkLayers.Null;
            capturing = Capture(open.Where(d =>
                d.LinkType != PacketDotNet.LinkLayers.LinuxSll));
        }

        if (capturing.Length == 0)
        {
            // Capture from all interfaces that are open
            linkType = PacketDotNet.LinkLayers.Null;
            capturing = Capture(open);
        }

        _writer.Open(new DeviceConfiguration
        {
            LinkLayerType = linkType
        });

        if (capturing.Length != 0)
        {
            foreach (var device in capturing)
            {
                device.StartCapture();
                _logger.LogInformation("Capturing {Device} ({Description})...",
                    device.Name, device.Description);
            }
            _logger.LogInformation("    ... to {FileName} ({Filter}).",
                File, _configuration.Filter ?? "No filter");
        }
        else
        {
            _logger.LogWarning("No capture devices found to capture from.");
        }
        Start = DateTimeOffset.UtcNow;

        LibPcapLiveDevice[] Capture(IEnumerable<LibPcapLiveDevice> candidates)
        {
            var capturing = new List<LibPcapLiveDevice>();
            foreach (var device in candidates)
            {
                try
                {
                    // Open the device for capturing
                    Debug.Assert(device.Opened);
                    if (_configuration.Filter != null)
                    {
                        device.Filter = _configuration.Filter;
                    }
                    device.OnPacketArrival += (_, e) => _writer.Write(e.GetPacket());

                    capturing.Add(device);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "Failed to capture {Device} ({Description}): {Message}",
                        device.Name, device.Description, ex.Message);
                }
            }
            return capturing.ToArray();
        }
    }

    /// <summary>
    /// Remote client
    /// </summary>
    public sealed record Client
    {
        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public Client(HttpClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="configuration"></param>
        public int Start(CaptureConfiguration configuration)
        {
            return StartAsync(configuration).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="ct"></param>
        public async Task<int> StartAsync(CaptureConfiguration configuration,
            CancellationToken ct = default)
        {
            var response = await _client.PutAsJsonAsync("/", configuration,
                ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="file"></param>
        /// <param name="start"></param>
        /// <exception cref="NetcapException"></exception>
        public DateTimeOffset Stop(int handle, string file, out DateTimeOffset start)
        {
            var result = StopAsync(handle, file).GetAwaiter().GetResult();
            if (result == null)
            {
                throw new NetcapException("Failed to stop capture.");
            }
            start = result.Start;
            return result.End;
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="file"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<CaptureResult?> StopAsync(int handle, string file,
            CancellationToken ct = default)
        {
            var s = await _client.GetStreamAsync(new Uri($"/{handle}"),
                ct).ConfigureAwait(false);
            var f = System.IO.File.Create(file);
            await using var sd = s.ConfigureAwait(false);
            await using var fd = f.ConfigureAwait(false);
            await s.CopyToAsync(f, ct).ConfigureAwait(false);

            var response = await _client.PostAsJsonAsync(
                $"/", handle, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CaptureResult>(
                ct).ConfigureAwait(false);
        }

        private readonly HttpClient _client;
        private readonly ILogger _logger;
    }

    /// <summary>
    /// Remote server
    /// </summary>
    public sealed record Server
    {
        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="app"></param>
        /// <param name="logger"></param>
        public Server(WebApplication app, ILogger logger)
        {
            _logger = logger;

            app.MapPut("/", CreateAndStart)
                .RequireAuthorization(nameof(Main.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
            app.MapGet("/{handle}", GetAndStop)
                .RequireAuthorization(nameof(Main.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
            app.MapPost("/", Cleanup)
                .RequireAuthorization(nameof(Main.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="configuration"></param>
        internal int CreateAndStart(CaptureConfiguration configuration)
        {
            var pcap = new Pcap(_logger, configuration);
            _captures.TryAdd(pcap.Handle, pcap);
            return pcap.Handle;
        }

        /// <summary>
        /// Get file and stop
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <exception cref="NetcapException"></exception>
        internal IResult GetAndStop(int handle)
        {
            if (!_captures.TryGetValue(handle, out var capture))
            {
                throw new NetcapException("Capture not found");
            }
            capture.Dispose();
            return Results.File(capture.File);
        }

        /// <summary>
        /// get metadata and cleanup
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <exception cref="NetcapException"></exception>
        internal CaptureResult Cleanup(int handle)
        {
            if (!_captures.TryRemove(handle, out var capture))
            {
                throw new NetcapException("Capture not found");
            }
            capture.Dispose();
            Debug.Assert(capture.End.HasValue);
            System.IO.File.Delete(capture.File);
            return new CaptureResult(capture.Start, capture.End.Value);
        }

        private readonly ConcurrentDictionary<int, Pcap> _captures = new();
        private readonly ILogger _logger;
    }

    /// <summary>
    /// Capture result
    /// </summary>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    public sealed record class CaptureResult(DateTimeOffset Start,
        DateTimeOffset End);

    private static int _handles;
    private readonly CaptureConfiguration _configuration;
    private readonly Client? _remoteCapture;
    private readonly List<LibPcapLiveDevice>? _devices;
    private readonly CaptureFileWriterDevice? _writer;
    private readonly ILogger _logger;
}
