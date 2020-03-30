// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Services {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport.Probe;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Provides discovery services
    /// </summary>
    public class DiscoveryServices : IDiscoveryServices, IScannerServices, IDisposable {

        /// <inheritdoc/>
        public DiscoveryMode Mode => _request.Mode;

        /// <inheritdoc/>
        public DiscoveryConfigModel Configuration => _request.Configuration;

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        /// <param name="serializer"></param>
        /// <param name="progress"></param>
        public DiscoveryServices(IEndpointDiscovery client, IEventEmitter events,
            IJsonSerializer serializer, ILogger logger, IDiscoveryProgress progress = null) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _progress = progress ?? new ProgressLogger(logger);

            _runner = Task.Run(() => ProcessDiscoveryRequestsAsync(_cts.Token));
            _timer = new Timer(_ => OnScanScheduling(), null,
                TimeSpan.FromSeconds(20), Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc/>
        public Task ConfigureAsync(DiscoveryMode mode, DiscoveryConfigModel config) {
            _request = new DiscoveryRequest(mode, config);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var task = new DiscoveryRequest(request);
            var scheduled = _queue.TryAdd(task);
            if (!scheduled) {
                task.Dispose();
                _logger.Error("Discovey request not scheduled, internal server error!");
                var ex = new ResourceExhaustionException("Failed to schedule task");
                _progress.OnDiscoveryError(request, ex);
                throw ex;
            }
            await _lock.WaitAsync();
            try {
                if (_pending.Count != 0) {
                    _progress.OnDiscoveryPending(task.Request, _pending.Count);
                }
                _pending.Add(task);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _lock.WaitAsync();
            try {
                foreach (var task in _pending.Where(r => r.Request.Id == request.Id)) {
                    // Cancel the task
                    task.Cancel();
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task ScanAsync() {

            // Fire timer now so that new request is scheduled
            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Async(StopDiscoveryRequestProcessingAsync).Wait();

            // Dispose
            _cts.Dispose();
            _timer.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// Scan timer expired
        /// </summary>
        private void OnScanScheduling() {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _lock.Wait();
            try {
                foreach (var task in _pending.Where(r => r.IsScan)) {
                    // Cancel any current scan tasks if any
                    task.Cancel();
                }

                // Add new discovery request
                if (Mode != DiscoveryMode.Off) {
                    // Push request
                    var task = _request.Clone();
                    if (_queue.TryAdd(task)) {
                        _pending.Add(task);
                    }
                    else {
                        task.Dispose();
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop discovery request processing
        /// </summary>
        /// <returns></returns>
        private async Task StopDiscoveryRequestProcessingAsync() {
            _queue.CompleteAdding();
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            await _lock.WaitAsync();
            try {
                // Cancel all requests first
                foreach (var request in _pending) {
                    request.Cancel();
                }
            }
            finally {
                _lock.Release();
            }

            // Try cancel discovery and wait for completion of runner
            Try.Op(() => _cts?.Cancel());
            try {
                await _runner;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected exception stopping processor thread.");
            }
        }

        /// <summary>
        /// Process discovery requests
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ProcessDiscoveryRequestsAsync(CancellationToken ct) {
            _logger.Information("Starting discovery processor...");
            // Process all discovery requests
            while (!ct.IsCancellationRequested) {
                try {
                    var request = _queue.Take(ct);
                    try {
                        // Update pending queue size
                        await ReportPendingRequestsAsync();
                        await ProcessDiscoveryRequestAsync(request);
                    }
                    finally {
                        // If the request is scan request, schedule next one
                        if (!ct.IsCancellationRequested && (request?.IsScan ?? false)) {
                            // Re-schedule another scan when idle time expired
                            _timer.Change(
                                request.Request.Configuration.IdleTimeBetweenScans ??
                                    TimeSpan.FromHours(1),
                                Timeout.InfiniteTimeSpan);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) {
                    _logger.Error(ex, "Discovery processor error occurred - continue...");
                }
            }
            // Send cancellation for all pending items
            await CancelPendingRequestsAsync();
            _logger.Information("Stopped discovery processor.");
        }

        /// <summary>
        /// Process the provided discovery request
        /// </summary>
        /// <param name="request"></param>
        private async Task ProcessDiscoveryRequestAsync(DiscoveryRequest request) {
            _logger.Debug("Processing discovery request...");
            _progress.OnDiscoveryStarted(request.Request);
            object diagnostics = null;

            //
            // Discover servers
            //
            List<ApplicationRegistrationModel> discovered;
            try {
                discovered = await DiscoverServersAsync(request);
                request.Token.ThrowIfCancellationRequested();
                //
                // Upload results
                //
                await SendDiscoveryResultsAsync(request, discovered, DateTime.UtcNow,
                    diagnostics, request.Token);

                _progress.OnDiscoveryFinished(request.Request);
            }
            catch (OperationCanceledException) {
                _progress.OnDiscoveryCancelled(request.Request);
            }
            catch (Exception ex) {
                _progress.OnDiscoveryError(request.Request, ex);
            }
            finally {
                if (request != null) {
                    await _lock.WaitAsync();
                    try {
                        _pending.Remove(request);
                        Try.Op(() => request.Dispose());
                    }
                    finally {
                        _lock.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Run a network discovery
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            DiscoveryRequest request) {

            var discoveryUrls = await GetDiscoveryUrlsAsync(request.DiscoveryUrls);
            if (request.Mode == DiscoveryMode.Off) {
                return await DiscoverServersAsync(request, discoveryUrls,
                    request.Configuration.Locales);
            }

            _logger.Information("Start {mode} discovery run...", request.Mode);
            var watch = Stopwatch.StartNew();

            //
            // Set up scanner pipeline and start discovery
            //
            var local = request.Mode == DiscoveryMode.Local;
#if !NO_WATCHDOG
            _counter = 0;
#endif
            var addresses = new List<IPAddress>();
            _progress.OnNetScanStarted(request.Request, 0, 0, request.TotalAddresses);
            using (var netscanner = new NetworkScanner(_logger, (scanner, reply) => {
                _progress.OnNetScanResult(request.Request, scanner.ActiveProbes,
                    scanner.ScanCount, request.TotalAddresses, addresses.Count, reply.Address);
                addresses.Add(reply.Address);
            }, local, local ? null : request.AddressRanges, request.NetworkClass,
                request.Configuration.MaxNetworkProbes, request.Configuration.NetworkProbeTimeout,
                request.Token)) {

                // Log progress
                using (var progress = new Timer(_ => ProgressTimer(
                    () => _progress.OnNetScanProgress(request.Request, netscanner.ActiveProbes,
                        netscanner.ScanCount, request.TotalAddresses, addresses.Count)),
                    null, kProgressInterval, kProgressInterval)) {
                    await netscanner.Completion;
                }
                _progress.OnNetScanFinished(request.Request, netscanner.ActiveProbes,
                    netscanner.ScanCount, request.TotalAddresses, addresses.Count);
            }
            request.Token.ThrowIfCancellationRequested();

            await AddLoopbackAddressesAsync(addresses);
            if (addresses.Count == 0) {
                return new List<ApplicationRegistrationModel>();
            }

            var ports = new List<IPEndPoint>();
            var totalPorts = request.TotalPorts * addresses.Count;
            var probe = new ServerProbe(_logger);
#if !NO_WATCHDOG
            _counter = 0;
#endif
            _progress.OnPortScanStart(request.Request, 0, 0, totalPorts);
            using (var portscan = new PortScanner(_logger,
                addresses.SelectMany(address => {
                    var ranges = request.PortRanges ?? PortRange.OpcUa;
                    return ranges.SelectMany(x => x.GetEndpoints(address));
                }), (scanner, ep) => {
                    _progress.OnPortScanResult(request.Request, scanner.ActiveProbes,
                        scanner.ScanCount, totalPorts, ports.Count, ep);
                    ports.Add(ep);
                }, probe, request.Configuration.MaxPortProbes,
                request.Configuration.MinPortProbesPercent,
                request.Configuration.PortProbeTimeout, request.Token)) {

                using (var progress = new Timer(_ => ProgressTimer(
                    () => _progress.OnPortScanProgress(request.Request, portscan.ActiveProbes,
                        portscan.ScanCount, totalPorts, ports.Count)),
                    null, kProgressInterval, kProgressInterval)) {
                    await portscan.Completion;
                }
                _progress.OnPortScanFinished(request.Request, portscan.ActiveProbes,
                    portscan.ScanCount, totalPorts, ports.Count);
            }
            request.Token.ThrowIfCancellationRequested();
            if (ports.Count == 0) {
                return new List<ApplicationRegistrationModel>();
            }

            //
            // Collect discovery urls
            //
            foreach (var ep in ports) {
                request.Token.ThrowIfCancellationRequested();
                var resolved = await ep.TryResolveAsync();
                var url = new Uri($"opc.tcp://" + resolved);
                discoveryUrls.Add(ep, url);
            }
            request.Token.ThrowIfCancellationRequested();

            //
            // Create application model list from discovered endpoints...
            //
            var discovered = await DiscoverServersAsync(request, discoveryUrls,
                request.Configuration.Locales);

            _logger.Information("Discovery took {elapsed} and found {count} servers.",
                watch.Elapsed, discovered.Count);
            return discovered;
        }

        /// <summary>
        /// Discover servers using opcua discovery and filter by optional locale
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discoveryUrls"></param>
        /// <param name="locales"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            DiscoveryRequest request, Dictionary<IPEndPoint, Uri> discoveryUrls,
            List<string> locales) {
            var discovered = new List<ApplicationRegistrationModel>();
            var count = 0;
            _progress.OnServerDiscoveryStarted(request.Request, 1, count, discoveryUrls.Count);
            foreach (var item in discoveryUrls) {
                request.Token.ThrowIfCancellationRequested();
                var url = item.Value;

                _progress.OnFindEndpointsStarted(request.Request, 1, count, discoveryUrls.Count,
                    discovered.Count, url, item.Key.Address);

                // Find endpoints at the real accessible ip address
                var eps = await _client.FindEndpointsAsync(new UriBuilder(url) {
                    Host = item.Key.Address.ToString()
                }.Uri, locales, request.Token).ConfigureAwait(false);

                count++;
                var endpoints = 0;
                foreach (var ep in eps) {
                    discovered.AddOrUpdate(ep.ToServiceModel(item.Key.ToString(),
                        _events.SiteId, _events.DeviceId, _events.ModuleId, _serializer));
                    endpoints++;
                }
                _progress.OnFindEndpointsFinished(request.Request, 1, count, discoveryUrls.Count,
                    discovered.Count, url, item.Key.Address, endpoints);
            }

            _progress.OnServerDiscoveryFinished(request.Request, 1, discoveryUrls.Count,
                discoveryUrls.Count, discovered.Count);
            request.Token.ThrowIfCancellationRequested();
            return discovered;
        }

        /// <summary>
        /// Get all reachable addresses from urls
        /// </summary>
        /// <param name="discoveryUrls"></param>
        /// <returns></returns>
        private async Task<Dictionary<IPEndPoint, Uri>> GetDiscoveryUrlsAsync(
            IEnumerable<Uri> discoveryUrls) {
            if (discoveryUrls?.Any() ?? false) {
                var results = await Task.WhenAll(discoveryUrls
                    .Select(GetHostEntryAsync)
                    .ToArray());
                return results
                    .SelectMany(v => v)
                    .Where(a => a.Item2 != null)
                    .ToDictionary(k => k.Item1, v => v.Item2);
            }
            return new Dictionary<IPEndPoint, Uri>();
        }

        /// <summary>
        /// Get a reachable host address from url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        private Task<List<Tuple<IPEndPoint, Uri>>> GetHostEntryAsync(
            Uri discoveryUrl) {
            return Try.Async(async () => {
                var host = discoveryUrl.DnsSafeHost;
                var list = new List<Tuple<IPEndPoint, Uri>>();

                // check first if host is an IP Address since the Dns.GetHostEntryAsync
                // throws a socket exception when called with an IP address
                try {
                    var hostIp = IPAddress.Parse(host);
                    var ep = new IPEndPoint(hostIp,
                            discoveryUrl.IsDefaultPort ? 4840 : discoveryUrl.Port);
                    list.Add(Tuple.Create(ep, discoveryUrl));
                    return list;
                }
                catch {
                    // Parsing failed, therefore not an IP address, continue with dns
                    // resolution
                }

                while (!string.IsNullOrEmpty(host)) {
                    try {
                        var entry = await Dns.GetHostEntryAsync(host);
                        // only pick-up the IPV4 addresses
                        var foundIpv4 = false;
                        foreach (var address in entry.AddressList
                            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)) {
                            var ep = new IPEndPoint(address,
                                discoveryUrl.IsDefaultPort ? 4840 : discoveryUrl.Port);
                            list.Add(Tuple.Create(ep, discoveryUrl));
                            foundIpv4 = true;
                        }
                        if (!foundIpv4) {
                            // if no IPV4 responsive, try IPV6 as fallback
                            foreach (var address in entry.AddressList
                                .Where(a => a.AddressFamily != AddressFamily.InterNetwork)) {
                                var ep = new IPEndPoint(address,
                                    discoveryUrl.IsDefaultPort ? 4840 : discoveryUrl.Port);
                                list.Add(Tuple.Create(ep, discoveryUrl));
                            }
                        }

                        // Check local host
                        if (host.EqualsIgnoreCase("localhost") &&
                            (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?
                                .EqualsIgnoreCase("true") ?? false)) {
                            // Also resolve docker internal since we are in a container
                            host = kDockerHostName;
                            continue;
                        }
                        break;
                    }
                    catch (Exception e) {
                        _logger.Warning(e, "Failed to resolve the host for {discoveryUrl}", discoveryUrl);
                        return list;
                    }
                }
                return list;
            });
        }

        /// <summary>
        /// Add localhost ip to list if not already in it.
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns></returns>
        private async Task AddLoopbackAddressesAsync(List<IPAddress> addresses) {
            // Check local host
            try {
                if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?
                    .EqualsIgnoreCase("true") ?? false) {
                    // Resolve docker host since we are running in a container
                    var entry = await Dns.GetHostEntryAsync(kDockerHostName);
                    foreach (var address in entry.AddressList
                                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                                .Where(a => !addresses.Any(b => a.Equals(b)))) {
                        _logger.Information("Including host address {address}", address);
                        addresses.Add(address);
                    }
                }
                else {
                    // Add loopback address
                    addresses.Add(IPAddress.Loopback);
                }
            }
            catch (Exception e) {
                _logger.Warning(e, "Failed to add local host address.");
            }
        }

        /// <summary>
        /// Upload results
        /// </summary>
        /// <param name="discovered"></param>
        /// <param name="request"></param>
        /// <param name="timestamp"></param>
        /// <param name="diagnostics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendDiscoveryResultsAsync(DiscoveryRequest request,
            List<ApplicationRegistrationModel> discovered, DateTime timestamp,
            object diagnostics, CancellationToken ct) {
            _logger.Information("Uploading {count} results...", discovered.Count);
            var messages = discovered
                .SelectMany(server => server.Endpoints
                    .Select(registration => new DiscoveryEventModel {
                        Application = server.Application,
                        Registration = registration,
                        TimeStamp = timestamp
                    }))
                .Append(new DiscoveryEventModel {
                    Registration = null, // last
                    Result = new DiscoveryResultModel {
                        DiscoveryConfig = request.Configuration,
                        Id = request.Request.Id,
                        Context = request.Request.Context,
                        RegisterOnly = request.Mode == DiscoveryMode.Off,
                        Diagnostics = diagnostics == null ? null :
                            _serializer.FromObject(diagnostics)
                    },
                    TimeStamp = timestamp
                })
                .Select((discovery, i) => {
                    discovery.Index = i;
                    return discovery;
                });
            await Task.Run(() => _events.SendEventAsync(
                messages.Select(message => _serializer.SerializeToBytes(message).ToArray()),
                    ContentMimeType.Json, Registry.Models.MessageSchemaTypes.DiscoveryEvents,
                        "utf-8"), ct);
            _logger.Information("{count} results uploaded.", discovered.Count);
        }

        /// <summary>
        /// Cancel all remaining pending requests
        /// </summary>
        /// <returns></returns>
        private async Task CancelPendingRequestsAsync() {
            _logger.Information("Cancelling all pending requests...");
            await _lock.WaitAsync();
            try {
                foreach (var request in _pending) {
                    _progress.OnDiscoveryCancelled(request.Request);
                    Try.Op(() => request.Dispose());
                }
                _pending.Clear();
            }
            finally {
                _lock.Release();
            }
            _logger.Information("Pending requests cancelled...");
        }

        /// <summary>
        /// Send pending queue size
        /// </summary>
        /// <returns></returns>
        private async Task ReportPendingRequestsAsync() {
            // Notify all listeners about the request's place in queue
            await _lock.WaitAsync();
            try {
                for (var pos = 0; pos < _pending.Count; pos++) {
                    var item = _pending[pos];
                    if (!item.Token.IsCancellationRequested) {
                        _progress.OnDiscoveryPending(item.Request, pos);
                    }
                }
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to send pending event");
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Called in intervals to check and log progress.
        /// </summary>
        /// <param name="log"></param>
        private void ProgressTimer(Action log) {
            if ((_counter % 3) == 0) {
                _logger.Information("GC Mem: {gcmem} kb, Working set / Private Mem: " +
                    "{privmem} kb / {privmemsize} kb, Handles: {handles}",
                    GC.GetTotalMemory(false) / 1024,
                    Process.GetCurrentProcess().WorkingSet64 / 1024,
                    Process.GetCurrentProcess().PrivateMemorySize64 / 1024,
                    Process.GetCurrentProcess().HandleCount);
            }
            ++_counter;
#if !NO_WATCHDOG
            if ((_counter % 200) == 0) {
                if (_counter >= 2000) {
                    throw new ThreadStateException("Stuck");
                }
            }
#endif
            log();
        }
#if !NO_WATCHDOG
        private int _counter;
#endif

        /// <summary> Progress reporting every 3 seconds </summary>
        private static readonly TimeSpan kProgressInterval = TimeSpan.FromSeconds(3);
        private const string kDockerHostName = "host.docker.internal";

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly IEventEmitter _events;
        private readonly IDiscoveryProgress _progress;
        private readonly IEndpointDiscovery _client;
        private readonly Task _runner;
        private readonly Timer _timer;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly List<DiscoveryRequest> _pending =
            new List<DiscoveryRequest>();
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private readonly BlockingCollection<DiscoveryRequest> _queue =
            new BlockingCollection<DiscoveryRequest>();
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private DiscoveryRequest _request = new DiscoveryRequest();
#pragma warning restore IDE0069 // Disposable fields should be disposed
    }
}
