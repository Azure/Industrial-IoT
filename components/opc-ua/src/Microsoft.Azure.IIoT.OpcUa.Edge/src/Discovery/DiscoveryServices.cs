// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Provides discovery services for the supervisor
    /// </summary>
    public class DiscoveryServices : IScannerServices,
        IDiscoveryServices, IDisposable {

        /// <inheritdoc/>
        public DiscoveryMode Mode {
            get => Request.Mode;
            set => Request = new DiscoveryRequest(value, Request.Configuration);
        }

        /// <inheritdoc/>
        public DiscoveryConfigModel Configuration {
            get => Request.Configuration;
            set => Request = new DiscoveryRequest(Request.Mode, value);
        }

        /// <summary>
        /// Current discovery options
        /// </summary>
        internal DiscoveryRequest Request { get; set; } = new DiscoveryRequest();

        /// <summary>
        /// Default idle time is 6 hours
        /// </summary>
        internal static TimeSpan DefaultIdleTime { get; set; } = TimeSpan.FromHours(6);

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="events"></param>
        /// <param name="processor"></param>
        /// <param name="logger"></param>
        public DiscoveryServices(IEndpointDiscovery client, IEventEmitter events,
            ITaskProcessor processor, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _discovered = new SortedDictionary<DateTime, List<ApplicationRegistrationModel>>();
            _setupDelay = TimeSpan.FromSeconds(10);
            _lock = new SemaphoreSlim(1);
        }

        /// <inheritdoc/>
        public async Task ScanAsync() {
            await _lock.WaitAsync();
            try {
                await StopAsync();

                if (Mode != DiscoveryMode.Off) {
                    _discovery = new CancellationTokenSource();
                    _completed = _processor.Scheduler.Run(() =>
                        RunContinuouslyAsync(Request.Clone(), _setupDelay, _discovery.Token));
                    _setupDelay = null;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task DiscoverAsync(DiscoveryRequestModel request, CancellationToken ct) {
            if (request == null) {
                return Task.FromException(new ArgumentNullException(nameof(request)));
            }
            var scheduled = _processor.TrySchedule(() =>
                RunOnceAsync(new DiscoveryRequest(request), ct));
            if (scheduled) {
                return Task.CompletedTask;
            }
            _logger.Error("Task not scheduled, internal server error!");
            return Task.FromException(new ResourceExhaustionException(
                "Failed to schedule task"));
        }

        /// <inheritdoc/>
        public void Dispose() {
            _lock.Wait();
            try {
                StopAsync().Wait();
            }
            finally {
                _discovery?.Dispose();
                _discovery = null;
                _lock.Release();
                _lock.Dispose();
            }
        }

        /// <summary>
        /// Stop discovery
        /// </summary>
        /// <returns></returns>
        private async Task StopAsync() {
            Debug.Assert(_lock.CurrentCount == 0);

            // Try cancel discovery
            Try.Op(() => _discovery?.Cancel());
            // Wait for completion
            try {
                var completed = _completed;
                _completed = null;
                if (completed != null) {
                    await completed;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected exception stopping current discover thread.");
            }
            finally {
                Try.Op(() => _discovery?.Dispose());
                _discovery = null;
                _discovered.Clear();
            }
        }

        /// <summary>
        /// Scan and discover one time
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        private async Task RunOnceAsync(DiscoveryRequest request,
            CancellationToken ct) {
            _logger.Debug("Processing discovery request...");
            OnDiscoveryStarted(request);
            object diagnostics = null;

            //
            // Discover servers
            //
            List<ApplicationRegistrationModel> discovered;
            try {
                discovered = await DiscoverServersAsync(request, ct);
                ct.ThrowIfCancellationRequested();
                //
                // Upload results
                //
                await SendDiscoveryResultsAsync(request, discovered, DateTime.UtcNow,
                    diagnostics, ct);
                OnDiscoveryComplete(request);
            }
            catch (OperationCanceledException) {
                OnDiscoveryCancelled(request);
            }
            catch (Exception ex) {
                OnDiscoveryError(request, ex);
            }
        }

        /// <summary>
        /// Scan and discover in continuous loop
        /// </summary>
        /// <param name="request"></param>
        /// <param name="delay"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunContinuouslyAsync(DiscoveryRequest request,
            TimeSpan? delay, CancellationToken ct) {

            if (delay != null) {
                try {
                    _logger.Debug("Delaying for {delay}...", delay);
                    await Task.Delay((TimeSpan)delay, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Debug("Cancelled discovery start.");
                    return;
                }
            }

            _logger.Information("Starting discovery...");

            // Run scans until cancelled
            while (!ct.IsCancellationRequested) {
                OnDiscoveryStarted(request);
                try {
                    //
                    // Discover
                    //
                    var discovered = await DiscoverServersAsync(request, ct);
                    var timestamp = DateTime.UtcNow;

                    //
                    // Update cache
                    //
                    lock (_discovered) {
                        _discovered.Add(timestamp, discovered);
                        if (_discovered.Count > 10) {
                            _discovered.Remove(_discovered.First().Key);
                        }
                    }

                    ct.ThrowIfCancellationRequested();

                    //
                    // Send results
                    //
                    await SendDiscoveryResultsAsync(request, discovered, timestamp,
                        null, ct);
                    OnDiscoveryComplete(request);
                }
                catch (OperationCanceledException) {
                    OnDiscoveryCancelled(request);
                    break;
                }
                catch (Exception ex) {
                    OnDiscoveryError(request, ex);
                }

                //
                // Delay next processing
                //
                try {
                    if (!ct.IsCancellationRequested) {
                        GC.Collect();
                        var idle = request.Configuration.IdleTimeBetweenScans ??
                            DefaultIdleTime;
                        if (idle.Ticks != 0) {
                            _logger.Debug("Idle for {idle}...", idle);
                            await Task.Delay(idle, ct);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
            }

            _logger.Information("Cancelled discovery.");
        }

        /// <summary>
        /// Run a network discovery
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            DiscoveryRequest request, CancellationToken ct) {

            var discoveryUrls = await GetDiscoveryUrlsAsync(request.DiscoveryUrls);
            if (request.Mode == DiscoveryMode.Off) {
                return await DiscoverServersAsync(request, discoveryUrls,
                    request.Configuration.Locales, ct);
            }

            _logger.Information("Start {mode} discovery run...", request.Mode);
            var watch = Stopwatch.StartNew();

            //
            // Set up scanner pipeline and start discovery
            //
            var local = request.Mode == DiscoveryMode.Local;
#if !NO_SCHEDULER_DUMP
            _counter = 0;
#endif
            var addresses = new List<IPAddress>();
            using (var netscanner = new NetworkScanner(_logger,
                (scanner, reply) => {
                OnNetScanResult(request, scanner, reply.Address);
                addresses.Add(reply.Address);
            }, local, local ? null : request.AddressRanges, request.NetworkClass,
                request.Configuration.MaxNetworkProbes,
                request.Configuration.NetworkProbeTimeout, ct)) {

                // Log progress
                OnNetScanStarted(request, netscanner);
                using (var progress = new Timer(_ => ProgressTimer(
                    () => OnNetScanProgress(request, netscanner, addresses)),
                    null, _progressInterval, _progressInterval)) {
                    await netscanner.Completion;
                }
                OnNetScanComplete(request, netscanner, addresses, watch.Elapsed);
            }
            ct.ThrowIfCancellationRequested();
            if (addresses.Count == 0) {
                return new List<ApplicationRegistrationModel>();
            }

            var ports = new List<IPEndPoint>();
            var probe = new ServerProbe(_logger);
#if !NO_SCHEDULER_DUMP
            _counter = 0;
#endif
            using (var portscan = new PortScanner(_logger,
                addresses.SelectMany(address => {
                    var ranges = request.PortRanges ?? PortRange.OpcUa;
                    return ranges.SelectMany(x => x.GetEndpoints(address));
                }), (scanner, ep) => {
                    OnPortScanResult(request, scanner, ep);
                    ports.Add(ep);
                }, probe, request.Configuration.MaxPortProbes,
                request.Configuration.MinPortProbesPercent,
                request.Configuration.PortProbeTimeout, ct)) {

                OnPortScanStart(request, portscan);
                using (var progress = new Timer(_ => ProgressTimer(
                    () => OnPortScanProgress(request, portscan, ports)),
                    null, _progressInterval, _progressInterval)) {
                    await portscan.Completion;
                }
                OnPortScanComplete(request, portscan, ports, watch.Elapsed);
            }
            ct.ThrowIfCancellationRequested();
            if (ports.Count == 0) {
                return new List<ApplicationRegistrationModel>();
            }

            //
            // Collect discovery urls
            //
            foreach (var ep in ports) {
                ct.ThrowIfCancellationRequested();
                var resolved = await ep.TryResolveAsync();
                var url = new Uri($"opc.tcp://" + resolved);
                discoveryUrls.Add(ep, url);
            }
            ct.ThrowIfCancellationRequested();

            //
            // Create application model list from discovered endpoints...
            //
            var discovered = await DiscoverServersAsync(request, discoveryUrls,
                request.Configuration.Locales, ct);

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
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            DiscoveryRequest request, Dictionary<IPEndPoint, Uri> discoveryUrls,
            List<string> locales, CancellationToken ct) {
            var discovered = new List<ApplicationRegistrationModel>();
            var supervisorId = SupervisorModelEx.CreateSupervisorId(_events.DeviceId,
                _events.ModuleId);

            OnServerDiscoveryStarted(request, discoveryUrls);

            foreach (var item in discoveryUrls) {
                ct.ThrowIfCancellationRequested();
                var url = item.Value;

                OnFindEndpointsStarted(request, url, item.Key.Address);

                // Find endpoints at the real accessible ip address
                var eps = await _client.FindEndpointsAsync(new UriBuilder(url) {
                    Host = item.Key.Address.ToString()
                }.Uri, locales, ct).ConfigureAwait(false);

                var endpoints = eps.ToList();

                OnFindEndpointsComplete(request, url, item.Key.Address, endpoints);
                if (endpoints.Count == 0) {
                    continue;
                }

                // Merge results...
                foreach (var ep in eps) {
                    discovered.AddOrUpdate(ep.ToServiceModel(item.Key.ToString(),
                        _events.SiteId, supervisorId));
                }
            }

            OnServerDiscoveryComplete(request, discovered);
            ct.ThrowIfCancellationRequested();
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
        private Task<(IPEndPoint, Uri)> GetHostEntryAsync(
            Uri discoveryUrl) {
            return Try.Async(async () => {
                var entry = await Dns.GetHostEntryAsync(discoveryUrl.DnsSafeHost);
                foreach (var address in entry.AddressList) {
                    var reply = await new Ping().SendPingAsync(address);
                    if (reply.Status == IPStatus.Success) {
                        var port = discoveryUrl.Port;
                        return (new IPEndPoint(address,
                            port == 0 ? 4840 : port), discoveryUrl);
                    }
                }
                return (null, null);
            });
        }

        /// <summary>
        /// Upload results
        /// </summary>
        /// <param name="discovered"></param>
        /// <param name="timestamp"></param>
        /// <param name="request"></param>
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
                            JToken.FromObject(diagnostics)
                    },
                    TimeStamp = timestamp
                })
                .Select((discovery, i) => {
                    discovery.Index = i;
                    return discovery;
                })
                .Select(discovery => Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(discovery)));
            await Task.Run(() => _events.SendAsync(
                messages, ContentTypes.DiscoveryEvents), ct);
            _logger.Information("{count} results uploaded.", discovered.Count);
        }

        /// <summary>
        /// Discovery started
        /// </summary>
        /// <param name="request"></param>
        private void OnDiscoveryStarted(DiscoveryRequest request) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Discovery operation started.",
                request.Request.Id);
        }

        /// <summary>
        /// Discovery cancelled
        /// </summary>
        /// <param name="request"></param>
        private void OnDiscoveryCancelled(DiscoveryRequest request) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Discovery operation cancelled.",
                request.Request.Id);
        }

        /// <summary>
        /// Discovery error
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ex"></param>
        private void OnDiscoveryError(DiscoveryRequest request, Exception ex) {
            // TODO: Send telemetry
            _logger.Error(ex, "{request}: Error during discovery run...",
                request.Request.Id);
        }

        /// <summary>
        /// Discovery completed
        /// </summary>
        /// <param name="request"></param>
        private void OnDiscoveryComplete(DiscoveryRequest request) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Discovery operation completed.",
                request.Request.Id);
        }

        /// <summary>
        /// Network scanning started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        private void OnNetScanStarted(DiscoveryRequest request,
            NetworkScanner netscanner) {
            // TODO: Send telemetry
            _logger.Information(
                "{request}: Starting network scan ({active} probes active)...",
                request.Request.Id, netscanner.ActiveProbes);
        }

        /// <summary>
        /// Network scan result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="address"></param>
        private void OnNetScanResult(DiscoveryRequest request,
            NetworkScanner netscanner, IPAddress address) {
            // TODO: Send telemetry
            _logger.Verbose("{request}: Found address {address} ({scanned} scanned)...",
                request.Request.Id, address, netscanner.ScanCount);
        }

        /// <summary>
        /// Network scan progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="addresses"></param>
        private void OnNetScanProgress(DiscoveryRequest request,
            NetworkScanner netscanner, List<IPAddress> addresses) {
            // TODO: Send telemetry
            _logger.Information("{request}: {scanned} addresses scanned - {found} " +
                "found ({active} probes active)...", request.Request.Id,
                netscanner.ScanCount, addresses.Count, netscanner.ActiveProbes);
        }

        /// <summary>
        /// Network scan complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="addresses"></param>
        /// <param name="elapsed"></param>
        private void OnNetScanComplete(DiscoveryRequest request,
            NetworkScanner netscanner, List<IPAddress> addresses,
            TimeSpan elapsed) {
            // TODO: Send telemetry
            _logger.Information("{request}: Found {count} addresses took {elapsed} " +
                "({scanned} scanned)...", request.Request.Id,
                addresses.Count, elapsed, netscanner.ScanCount);
        }

        /// <summary>
        /// Port scan complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        private void OnPortScanStart(DiscoveryRequest request, PortScanner portscan) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Starting port scanning ({active} probes active)...",
                request.Request.Id, portscan.ActiveProbes);
        }

        /// <summary>
        /// Port scan progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ports"></param>
        private void OnPortScanProgress(DiscoveryRequest request,
            PortScanner portscan, List<IPEndPoint> ports) {
            // TODO: Send telemetry
            _logger.Information("{request}: {scanned} ports scanned - {found} found" +
                " ({active} probes active)...", request.Request.Id,
                portscan.ScanCount, ports.Count, portscan.ActiveProbes);
        }

        /// <summary>
        /// Port scan result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ep"></param>
        private void OnPortScanResult(DiscoveryRequest request,
            PortScanner portscan, IPEndPoint ep) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Found server {endpoint} ({scanned} scanned)...",
                request.Request.Id, ep, portscan.ScanCount);
        }

        /// <summary>
        /// Port scan complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ports"></param>
        /// <param name="elapsed"></param>
        private void OnPortScanComplete(DiscoveryRequest request,
            PortScanner portscan, List<IPEndPoint> ports,
            TimeSpan elapsed) {
            // TODO: Send telemetry
            _logger.Information("{request}: Found {count} ports on servers " +
                "took {elapsed} ({scanned} scanned)...",
                request.Request.Id, ports.Count, elapsed, portscan.ScanCount);
        }

        /// <summary>
        /// Discovery started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discoveryUrls"></param>
        private void OnServerDiscoveryStarted(DiscoveryRequest request,
            Dictionary<IPEndPoint, Uri> discoveryUrls) {
            // TODO: Send telemetry
            _logger.Information(
                "{request}: Searching {count} discovery urls for endpoints...",
                request.Request.Id, discoveryUrls.Count);
        }

        /// <summary>
        /// Find endpoints started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        private void OnFindEndpointsStarted(DiscoveryRequest request,
            Uri url, IPAddress address) {
            // TODO: Send telemetry
            _logger.Information(
                "{request}: Trying to find endpoints on {host}:{port} ({address})...",
                request.Request.Id, url.Host, url.Port, address);
        }

        /// <summary>
        /// Find endpoints completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        /// <param name="eps"></param>
        private void OnFindEndpointsComplete(DiscoveryRequest request,
            Uri url, IPAddress address,
            List<DiscoveredEndpointModel> eps) {
            // TODO: Send telemetry
            if (eps.Count == 0) {
                // TODO: Send telemetry
                _logger.Information(
                    "{request}: No endpoints found on {host}:{port} ({address}).",
                    request.Request.Id, url.Host, url.Port, address);
            }
            _logger.Information(
                "{request}: Found {count} endpoints on {host}:{port} ({address}).",
                request.Request.Id, eps.Count(), url.Host, url.Port, address);
        }

        /// <summary>
        /// Endpoint Discovery complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discovered"></param>
        private void OnServerDiscoveryComplete(DiscoveryRequest request,
            List<ApplicationRegistrationModel> discovered) {
            // TODO: Send telemetry
            _logger.Information("{request}: Found total of {count} servers ...",
                request.Request.Id, discovered.Count);
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
#if !NO_SCHEDULER_DUMP
            if ((_counter % 200) == 0) {
                _logger.Debug("Dumping tasks...");
                _logger.Debug("-------------------------");
                _processor.Scheduler.Dump(task => _logger.Debug("{Task}", task));
                _logger.Debug("-------------------------");
                _logger.Debug("... completed");
                if (_counter >= 2000) {
                    throw new ThreadStateException("Stuck");
                }
            }
#endif
            log();
        }

#if !NO_SCHEDULER_DUMP
        private int _counter;
#endif
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private CancellationTokenSource _discovery;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        private Task _completed;
        private TimeSpan? _setupDelay;

        private readonly SortedDictionary<DateTime, List<ApplicationRegistrationModel>> _discovered;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly TimeSpan _progressInterval = TimeSpan.FromSeconds(10);
        private readonly IEventEmitter _events;
        private readonly ITaskProcessor _processor;
        private readonly IEndpointDiscovery _client;
    }
}
