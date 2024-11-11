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
using System.Text.RegularExpressions;

internal abstract class Pcap
{
    /// <summary>
    /// Pcap configuration
    /// </summary>
    /// <param name="InterfaceType"></param>
    /// <param name="Filter"></param>
    /// <param name="MaxFileSize"></param>
    /// <param name="MaxDuration"></param>
    public sealed record class CaptureConfiguration(
        InterfaceType InterfaceType, string? Filter = null,
        int? MaxFileSize = null, TimeSpan? MaxDuration = null);

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
    public static IDisposable Capture(CaptureConfiguration configuration,
        Storage storage, ILogger logger, HttpClient? httpClient = null)
    {
        if (httpClient == null)
        {
            return new Local(logger, configuration, storage);
        }
        return new Remote(logger, configuration, storage, httpClient);
    }

    /// <summary>
    /// Merge files
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="outputFile"></param>
    public static void Merge(string folder, string outputFile)
    {
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }
        var files = Directory.GetFiles(folder, "*.pcap");
        if (files.Length == 0)
        {
            return;
        }
        using var writer = new CaptureFileWriterDevice(outputFile);
        foreach (var file in files.Order())
        {
            using var reader = new CaptureFileReaderDevice(file);
            reader.Open();
            if (!writer.Opened)
            {
                writer.Open(new DeviceConfiguration
                {
                    LinkLayerType = reader.LinkType
                });
            }
            while ((reader.GetNextPacket(out var packet))
                == GetPacketStatus.PacketRead)
            {
                writer.Write(packet.GetPacket());
            }
        }
    }

    /// <summary>
    /// Get file path
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private static string GetFilePath(string folder, int index)
    {
        return Path.Combine(folder, $"capture{index}.pcap");
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
            Directory.CreateDirectory(_folder);
            _maxPcapSize =  configuration.MaxFileSize ?? kMaxPcapSize;
            _maxPcapDuration = configuration.MaxDuration.HasValue ?
                (long)configuration.MaxDuration.Value.TotalMilliseconds : kMaxPcapDuration;
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
                _captureWatch.Start();
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
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    StopCapture();
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
        /// StopCapture
        /// </summary>
        protected void StopCapture()
        {
            if (_devices != null)
            {
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
                _devices.ForEach(d => d.Dispose());
                _logger.LogInformation("Completed capture.");
                _devices = null;

                if (_captureCount > 0)
                {
                    _writer.Close();
                    _captureCount = 0;

                    _channel.Writer.TryWrite(_index);
                    _channel.Writer.TryComplete();
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
            if (_captureCount >= _maxPcapSize ||
                _captureWatch.ElapsedMilliseconds > kMaxPcapDuration)
            {
                UpdateWriter();
            }
            _writer.Write(pkt);
        }

        /// <summary>
        /// Locked update of writer
        /// </summary>
        private void UpdateWriter()
        {
            lock (_lock)
            {
                if (_captureCount >= _maxPcapSize ||
                    _captureWatch.ElapsedMilliseconds > kMaxPcapDuration)
                {
                    var finished = _index;
                    _writer.Dispose();
                    _writer = CreateWriter(_index + 1);

                    _index++;
                    _captureCount = 0;
                    _captureWatch.Restart();

                    _channel.Writer.TryWrite(finished);
                }
            }
        }

        /// <summary>
        /// Create next writer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private CaptureFileWriterDevice CreateWriter(int index)
        {
            var writer = new CaptureFileWriterDevice(GetFilePath(_folder, index));
            writer.Open(new DeviceConfiguration
            {
                LinkLayerType = _linkType
            });
            return writer;
        }

        /// <summary> 100MB </summary>
        private const long kMaxPcapSize = 100 * 1024 * 1024;
        /// <summary> 5 minutes </summary>
        private const long kMaxPcapDuration = 5 * 60 * 1000;
        private static int _handles;
        private int _index;
        private long _captureCount;
        private readonly Stopwatch _captureWatch = new Stopwatch();
        private readonly object _lock = new();
        private readonly CaptureConfiguration _configuration;
        private List<LibPcapLiveDevice>? _devices;
        private CaptureFileWriterDevice _writer;
        private readonly long _maxPcapSize;
        private readonly long _maxPcapDuration;
        private readonly Channel<int> _channel;
        private readonly LinkLayers _linkType;
        protected readonly string _folder;
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
            _runner = RunAsync();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopCapture();
                try
                {
                    // Will exit after stop capture as channel is completed
                    _runner.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop capture.");
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Service the file handler
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct = default)
        {
            await foreach (var index in Reader.ReadAllAsync(ct))
            {
                var file = GetFilePath(_folder, index);
                await _storage.UploadAsync(file, ct).ConfigureAwait(false);
                File.Delete(file);
            }
        }

        private readonly Storage _storage;
        private readonly Task _runner;
    }

    /// <summary>
    /// Remote client
    /// </summary>
    private sealed class Remote : IDisposable
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
        {
            _logger = logger;
            _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
            _client = client;
            _storage = storage;
            _handle = StartAsync(configuration).GetAwaiter().GetResult();
            _cts = new CancellationTokenSource();
            _runner = RunAsync(_cts.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
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
                Directory.Delete(_folder, true);
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

                        var file = GetFilePath(_folder, index);
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
        private readonly ILogger _logger;
        private readonly string _folder;
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
            public IResult Download(int index)
            {
                if (index > 0)
                {
                    // Clean up previous file
                    File.Delete(GetFilePath(_folder, index - 1));
                }
                return Results.File(GetFilePath(_folder, index));
            }

            /// <summary>
            /// Read indexes
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct = default)
                => Reader.ReadAllAsync(ct);
        }

        private readonly ConcurrentDictionary<int, CaptureAdapter> _captures = new();
        private readonly ILogger _logger;
    }
}
