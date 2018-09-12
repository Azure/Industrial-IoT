// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Module.Framework;
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
            try {
                await _lock.WaitAsync();
                await StopAsync();
                if (Mode != DiscoveryMode.Off) {
                    _discovery = new CancellationTokenSource();
                    _completed = _processor.Scheduler.Run(() =>
                        RunContinouslyAsync(Request.Clone(), _setupDelay, _discovery.Token));
                    _setupDelay = null;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                return Task.FromException(new ArgumentNullException(nameof(request)));
            }
            var scheduled = _processor.TrySchedule(() =>
                RunOnceAsync(new DiscoveryRequest(request), CancellationToken.None));
            if (scheduled) {
                return Task.CompletedTask;
            }
            _logger.Error("Task not scheduled, internal server error!", () => { });
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
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop discovery
        /// </summary>
        /// <returns></returns>
        private async Task StopAsync() {
            if (_completed == null) {
                return;
            }
            var completed = _completed;
            _discovery.Cancel();
            _completed = null;
            _discovery = null;
            _discovered.Clear();
            await completed;
        }

        /// <summary>
        /// Scan and discover one time
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        private async Task RunOnceAsync(DiscoveryRequest request,
            CancellationToken ct) {
            _logger.Debug("Processing discovery request...", () => { });
            object diagnostics = null;

            //
            // Discover servers
            //
            List<ApplicationRegistrationModel> discovered;
            try {
                discovered = await DiscoverServersAsync(request, ct);
                if (discovered.Count == 0 && request.Mode == DiscoveryMode.Off) {
                    // Optimize
                    return;
                }
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
        }

        /// <summary>
        /// Scan and discover in continuous loop
        /// </summary>
        /// <param name="request"></param>
        /// <param name="delay"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunContinouslyAsync(DiscoveryRequest request,
            TimeSpan? delay, CancellationToken ct) {

            if (delay != null) {
                try {
                    _logger.Debug($"Delaying for {delay}...", () => { });
                    await Task.Delay((TimeSpan)delay, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Debug("Cancelled discovery start.", () => { });
                    return;
                }
            }

            _logger.Info("Starting discovery...", () => { });

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
                    _logger.Error("Error during discovery run - continue...",
                        () => ex);
                }

                //
                // Delay next processing
                //
                try {
                    if (!ct.IsCancellationRequested) {
                        GC.Collect();
                        var idle = request.Configuration.IdleTimeBetweenScans ??
                            TimeSpan.FromMinutes(3);
                        if (idle.Ticks != 0) {
                            _logger.Debug($"Idle for {idle}...", () => { });
                            await Task.Delay(idle, ct);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
            }

            _logger.Info("Cancelled discovery.", () => { });
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
                return await DiscoverServersAsync(discoveryUrls, ct);
            }

            _logger.Info("Start discovery run...", () => { });
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
                _logger.Debug($"{reply.Address} found.", () => { });
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

            _logger.Info(
                $"Finding {addresses.Count} addresses took {watch.Elapsed}...",
                    () => { });
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
                    var ranges = request.PortRanges;
                    if (ranges == null) {
                        if (request.Mode == DiscoveryMode.Local) {
                            ranges = PortRange.All;
                        }
                        else {
                            ranges = PortRange.OpcUa;
                        }
                        if (request.Mode == DiscoveryMode.Scan) {
                            ranges = ranges.Concat(PortRange.Unassigned);
                        }
                    }
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
            _logger.Info(
                $"Finding {ports.Count} ports on servers (elapsed:{watch.Elapsed})...",
                    () => { });
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
            var discovered = await DiscoverServersAsync(discoveryUrls, ct);
            _logger.Info($"Discovery took {watch.Elapsed} and found " +
                $"{discovered.Count} servers.", () => { });
            return discovered;
        }

        /// <summary>
        /// Discover servers using opcua discovery
        /// </summary>
        /// <param name="discoveryUrls"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            Dictionary<IPEndPoint, Uri> discoveryUrls, CancellationToken ct) {
            var discovered = new List<ApplicationRegistrationModel>();
            var supervisorId = SupervisorModelEx.CreateSupervisorId(_events.DeviceId,
                _events.ModuleId);
            foreach (var item in discoveryUrls) {
                ct.ThrowIfCancellationRequested();
                var url = item.Value;
                var eps = await _client.FindEndpointsAsync(url, ct).ConfigureAwait(false);
                if (!eps.Any()) {
                    continue;
                }
                _logger.Info($"Found {eps.Count()} endpoints on {url.Host}:{url.Port}.",
                    () => { });
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
                    .Where(a => a != null)
                    .ToDictionary(k => k.Item1, v => v.Item2);
            }
            return new Dictionary<IPEndPoint, Uri>();
        }

        /// <summary>
        /// Get a reachable host address from url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        private Task<Tuple<IPEndPoint, Uri>> GetHostEntryAsync(
            Uri discoveryUrl) {
            return Try.Async(async () => {
                var entry = await Dns.GetHostEntryAsync(discoveryUrl.DnsSafeHost);
                foreach (var address in entry.AddressList) {
                    var reply = await new Ping().SendPingAsync(address);
                    if (reply.Status == IPStatus.Success) {
                        var port = discoveryUrl.Port;
                        return Tuple.Create(new IPEndPoint(address,
                            port == 0 ? 4840 : port), discoveryUrl);
                    }
                }
                return null;
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
            _logger.Info($"Uploading {discovered.Count} results...", () => { });
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
            _logger.Info($"{discovered.Count} results uploaded.", () => { });
        }

        /// <summary>
        /// Called in intervals to check and log progress.
        /// </summary>
        /// <param name="message"></param>
        private void ProgressTimer(Func<string> message) {
#if !NO_SCHEDULER_DUMP
            if ((++_counter % 500) == 0) {
                _processor.Scheduler.Dump(_logger);
                if (_counter >= 2000) {
                    throw new ThreadStateException("Stuck");
                }
            }
#endif
            _logger.Info(message(), () => { });
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
        private readonly TimeSpan _progressInterval = TimeSpan.FromSeconds(3);
        private readonly IEventEmitter _events;
        private readonly ITaskProcessor _processor;
        private readonly IEndpointDiscovery _client;

        private const int kPortScanBatchSize = 10000;
    }
}
