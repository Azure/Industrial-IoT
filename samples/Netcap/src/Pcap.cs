// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using SharpPcap;
using SharpPcap.LibPcap;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Channels;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections;

internal abstract class Pcap
{
    /// <summary>
    /// Pcap configuration
    /// </summary>
    /// <param name="InterfaceType"></param>
    /// <param name="Filter"></param>
    public sealed record class CaptureConfiguration(
        InterfaceType InterfaceType, string? Filter = null);

    public enum InterfaceType
    {
        AnyIfAvailable,
        AllButAny,
        EthernetOnly
    }

    /// <summary>
    /// Local
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="storage"></param>
    /// <param name="logger"></param>
    /// <param name="httpClient"></param>
    /// <returns></returns>
    public static IDisposable Capture(CaptureConfiguration configuration, Storage storage,
        ILogger logger, HttpClient? httpClient = null)
    {
        if (httpClient == null)
        {
            return new Local(logger, configuration, storage);
        }
        return new Remote(logger, configuration, storage, httpClient);
    }

    /// <summary>
    /// Pcap capture
    /// </summary>
    internal abstract class Base : IDisposable
    {
        /// <summary>
        /// Handle
        /// </summary>
        public int Handle { get; }

        /// <summary>
        /// Reader
        /// </summary>
        protected ChannelReader<int> Reader => _channel.Reader;

        /// <summary>
        /// Create pcap
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        protected Base(ILogger logger, CaptureConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _channel = Channel.CreateUnbounded<int>();

            Handle = Interlocked.Increment(ref _handles);
            _logger.LogInformation(
                "Using SharpPcap {Version}", SharpPcap.Pcap.SharpPcapVersion);

            if (LibPcapLiveDeviceList.Instance.Count == 0)
            {
                throw new NetcapException("Cannot run capture without devices.");
            }

            _devices = LibPcapLiveDeviceList.New().ToList();

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
                // Base from all interfaces that are open
                linkType = PacketDotNet.LinkLayers.Null;
                capturing = Capture(open);
            }

            _linkType = linkType;
            _writer = CreateWriter(_index);

            if (capturing.Length != 0)
            {
                foreach (var device in capturing)
                {
                    device.StartCapture();
                    _logger.LogInformation("Capturing {Device} ({Description})...",
                        device.Name, device.Description);
                }
                _logger.LogInformation("    ... with {Filter}.",
                    _configuration.Filter ?? "No filter");
            }
            else
            {
                _logger.LogWarning("No capture devices found to capture from.");
            }

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
                        device.OnPacketArrival += WritePacket;
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get file path
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetFilePath(int index)
        {
            return Path.Combine(_folder, $"capture{index}.pcap");
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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

                    _logger.LogInformation("Completed capture.");

                    CompleteWriter(_index);
                    _devices?.ForEach(d => d.Dispose());
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to complete capture.");
                }
                finally
                {
                    _writer.Dispose();
                    Directory.Delete(_folder, true);
                }
            }
        }

        /// <summary>
        /// Callback to write packet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WritePacket(object sender, PacketCapture e)
        {
            var pkt = e.GetPacket();
            _captureCount += pkt.PacketLength;
            if (_captureCount >= kMaxPcapSize)
            {
                _captureCount = 0;
                var next = Interlocked.Increment(ref _index);
                CompleteWriter(next - 1);
                _writer = CreateWriter(next);
            }
            _writer.Write(pkt);
        }

        /// <summary>
        /// Complete and signal upload of file
        /// </summary>
        /// <param name="index"></param>
        private void CompleteWriter(int index)
        {
            _writer.Dispose();
            _channel.Writer.TryWrite(index);
        }

        /// <summary>
        /// Create next writer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private CaptureFileWriterDevice CreateWriter(int index)
        {
            var writer = new CaptureFileWriterDevice(GetFilePath(index));
            writer.Open(new DeviceConfiguration
            {
                LinkLayerType = _linkType
            });
            return writer;
        }

        /// <summary> 10MB - make configurable </summary>
        private const long kMaxPcapSize = 10 * 1024 * 1024;
        private static int _handles;
        private int _index;
        private long _captureCount;
        private readonly CaptureConfiguration _configuration;
        private readonly List<LibPcapLiveDevice>? _devices;
        private CaptureFileWriterDevice _writer;
        private readonly string _folder;
        private readonly Channel<int> _channel;
        private readonly LinkLayers _linkType;
        protected readonly ILogger _logger;
    }

    /// <summary>
    /// Local capture
    /// </summary>
    internal sealed class Local : Base
    {
        /// <summary>
        /// Handle local capture
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="storage"></param>
        public Local(ILogger logger, CaptureConfiguration configuration,
            Storage storage)
            : base(logger, configuration)
        {
            _storage = storage;
            _cts = new CancellationTokenSource();
            _runner = RunAsync(_cts.Token);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try
                {
                    _cts.Cancel();
                    _runner.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop capture.");
                }
                finally
                {
                    _cts.Dispose();
                }
            }
        }

        /// <summary>
        /// Service the file handler
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct)
        {
            await foreach (var index in Reader.ReadAllAsync(ct))
            {
                var file = GetFilePath(index);
                await _storage.UploadAsync(file, ct).ConfigureAwait(false);
                File.Delete(file);
            }
        }

        private readonly CancellationTokenSource _cts;
        private readonly Storage _storage;
        private readonly Task _runner;
    }

    /// <summary>
    /// Remote client
    /// </summary>
    private sealed class Remote : Base
    {
        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="storage"></param>
        /// <param name="client"></param>
        public Remote(ILogger logger, CaptureConfiguration configuration,
            Storage storage, HttpClient client)
            : base(logger, configuration)
        {
            _client = client;
            _storage = storage;
            _handle = StartAsync(configuration).GetAwaiter().GetResult();
            _cts = new CancellationTokenSource();
            _runner = RunAsync(_cts.Token);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); // Does not throw
            if (disposing)
            {
                try
                {
                    StopAsync().GetAwaiter().GetResult();

                    _cts.Cancel();
                    _runner.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop");
                }
                finally
                {
                    _cts.Dispose();
                }
            }
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="ct"></param>
        private async Task<int> StartAsync(CaptureConfiguration configuration,
            CancellationToken ct = default)
        {
            var response = await _client.PutAsJsonAsync("/", configuration,
                ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Service the file handler
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await foreach (var index in _client
                        .GetFromJsonAsAsyncEnumerable<int>(new Uri($"/{_handle}"), ct))
                    {
                        var s = await _client.GetStreamAsync(new Uri($"/{_handle}/{index}"),
                            ct).ConfigureAwait(false);

                        var file = GetFilePath(index);
                        var f = System.IO.File.Create(file);
                        await using var sd = s.ConfigureAwait(false);
                        await using var fd = f.ConfigureAwait(false);
                        await s.CopyToAsync(f, ct).ConfigureAwait(false);

                        await _storage.UploadAsync(file, ct).ConfigureAwait(false);
                        File.Delete(file);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download.");
                }
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken ct = default)
        {
            var response = await _client.DeleteAsync(new Uri($"/{_handle}"),
                ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        private readonly int _handle;
        private readonly HttpClient _client;
        private readonly CancellationTokenSource _cts;
        private readonly Storage _storage;
        private readonly Task _runner;
    }

    /// <summary>
    /// Remote capture server
    /// </summary>
    public sealed record Server : IDisposable
    {
        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="app"></param>
        /// <param name="logger"></param>
        public Server(WebApplication app, ILogger logger)
        {
            _logger = logger;

            app.MapPut("/", Start)
                .RequireAuthorization(nameof(App.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
            app.MapGet("/{handle}", WaitAsync)
                .RequireAuthorization(nameof(App.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
            app.MapGet("/{handle}/{index}", Download)
                .RequireAuthorization(nameof(App.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
            app.MapDelete("/{handle}", Stop)
                .RequireAuthorization(nameof(App.ApiKeyProvider.ApiKey))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var capture in _captures.Values)
            {
                capture.Dispose();
            }
            _captures.Clear();
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="configuration"></param>
        internal int Start(CaptureConfiguration configuration)
        {
            var pcap = new CaptureAdapter(_logger, configuration);
            _captures.TryAdd(pcap.Handle, pcap);
            return pcap.Handle;
        }

        /// <summary>
        /// Read next index to download
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NetcapException"></exception>
        internal IAsyncEnumerable<int> WaitAsync(int handle,
            CancellationToken ct = default)
        {
            if (!_captures.TryGetValue(handle, out var capture))
            {
                throw new NetcapException("Capture not found");
            }
            return capture.ReadAllAsync(ct);
        }

        /// <summary>
        /// Download
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="NetcapException"></exception>
        internal IResult Download(int handle, int index)
        {
            if (!_captures.TryGetValue(handle, out var capture))
            {
                throw new NetcapException("Capture not found");
            }
            if (index > 0)
            {
                // Clean up previous file
                File.Delete(capture.GetFilePath(index - 1));
            }
            return capture.Download(index);
        }

        /// <summary>
        /// get metadata and cleanup
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <exception cref="NetcapException"></exception>
        internal void Stop(int handle)
        {
            if (!_captures.TryRemove(handle, out var capture))
            {
                throw new NetcapException("Capture not found");
            }
            capture.Dispose();
        }

        /// <summary>
        /// Remote capture
        /// </summary>
        private sealed class CaptureAdapter : Base
        {
            public CaptureAdapter(ILogger logger, CaptureConfiguration configuration) :
                base(logger, configuration)
            {
            }

            /// <summary>
            /// Download
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public IResult Download(int index) => Results.File(GetFilePath(index));

            /// <summary>
            /// Read indexes
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            public IAsyncEnumerable<int> ReadAllAsync(
                CancellationToken ct = default) => Reader.ReadAllAsync(ct);
        }

        private readonly ConcurrentDictionary<int, CaptureAdapter> _captures = new();
        private readonly ILogger _logger;
    }
}
