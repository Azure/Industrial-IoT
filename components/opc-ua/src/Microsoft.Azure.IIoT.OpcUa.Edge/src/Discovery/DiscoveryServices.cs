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
            object diagnostics = null;

            //
            // Discover servers
            //
            List<ApplicationRegistrationModel> discovered;
            try {
                discovered = await DiscoverServersAsync(request, ct);
            }
            catch (Exception ex) {
                diagnostics = ex;
                discovered = new List<ApplicationRegistrationModel>();
            }

            //
            // Upload results
            //
            if (!ct.IsCancellationRequested) {
                await UploadResultsAsync(request, discovered, DateTime.UtcNow,
                    diagnostics, ct);
            }
            _logger.Debug("Discovery request ended");
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
                    // Upload results
                    //
                    await UploadResultsAsync(request, discovered, timestamp,
                        null, ct);
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error during discovery run - continue...");
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
                return await DiscoverServersAsync(discoveryUrls,
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
            using (var netscanner = new NetworkScanner(_logger, reply => {
                _logger.Verbose("{address} found.", reply.Address);
                addresses.Add(reply.Address);
            }, local, local ? null : request.AddressRanges, request.NetworkClass,
                request.Configuration.MaxNetworkProbes,
                request.Configuration.NetworkProbeTimeout, ct)) {

                // Log progress
                using (var progress = new Timer(_ => ProgressTimer(() =>
                    $"Scanned {netscanner.ScanCount} addresses " +
                    $"(Active probes: {netscanner.ActiveProbes})..."),
                    null, _progressInterval, _progressInterval)) {
                    await netscanner.Completion;
                }
            }
            ct.ThrowIfCancellationRequested();

            _logger.Information("Found {count} addresses took {elapsed}...",
                addresses.Count, watch.Elapsed);
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
                }), ports.Add, probe, request.Configuration.MaxPortProbes,
                request.Configuration.MinPortProbesPercent,
                request.Configuration.PortProbeTimeout, ct)) {

                // Log progress
                using (var progress = new Timer(_ => ProgressTimer(() =>
                    $"Scanned {portscan.ScanCount} ports " +
                    $"(Active probes: {portscan.ActiveProbes})..."),
                    null, _progressInterval, _progressInterval)) {
                    await portscan.Completion;
                }
            }
            ct.ThrowIfCancellationRequested();
            _logger.Information("Found {count} ports on servers took {elapsed}...",
                ports.Count, watch.Elapsed);
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
            var discovered = await DiscoverServersAsync(discoveryUrls,
                request.Configuration.Locales, ct);
            _logger.Information("Discovery took {elapsed} and found {count} servers.",
                watch.Elapsed, discovered.Count);
            return discovered;
        }

        /// <summary>
        /// Discover servers using opcua discovery and filter by optional locale
        /// </summary>
        /// <param name="discoveryUrls"></param>
        /// <param name="locales"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            Dictionary<IPEndPoint, Uri> discoveryUrls, List<string> locales, CancellationToken ct) {
            var discovered = new List<ApplicationRegistrationModel>();
            var supervisorId = SupervisorModelEx.CreateSupervisorId(_events.DeviceId,
                _events.ModuleId);
            foreach (var item in discoveryUrls) {
                ct.ThrowIfCancellationRequested();
                var url = item.Value;

                // Find endpoints at the real accessible ip address
                var eps = await _client.FindEndpointsAsync(new UriBuilder(url) {
                    Host = item.Key.Address.ToString()
                }.Uri, locales, ct).ConfigureAwait(false);

                if (!eps.Any()) {
                    _logger.Information("No endpoints found on {host}:{port} ({address}).",
                        eps.Count(), url.Host, url.Port, item.Key.Address);
                    continue;
                }
                _logger.Information("Found {count} endpoints on {host}:{port} ({address}).",
                    eps.Count(), url.Host, url.Port, item.Key.Address);
                // Merge results...
                foreach (var ep in eps) {
                    discovered.AddOrUpdate(ep.ToServiceModel(item.Key.ToString(),
                        _events.SiteId, supervisorId));
                }
            }
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
        private async Task UploadResultsAsync(DiscoveryRequest request,
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
                messages, ContentTypes.DiscoveryEvent), ct);
            _logger.Information("{count} results uploaded.", discovered.Count);
        }

        /// <summary>
        /// Called in intervals to check and log progress.
        /// </summary>
        /// <param name="message"></param>
        private void ProgressTimer(Func<string> message) {
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
            _logger.Information(message());
        }

#if !NO_SCHEDULER_DUMP
        private int _counter;
#endif
        private CancellationTokenSource _discovery;
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
