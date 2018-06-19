// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.Net.Models;

    /// <summary>
    /// Provides discovery services for the supervisor
    /// </summary>
    public class OpcUaDiscoveryServices : IOpcUaDiscoveryServices, IDisposable {

        /// <summary>
        /// Current discovery mode
        /// </summary>
        public DiscoveryMode Mode {
            get => Options.Mode;
            set => Options.Mode = value;
        }

        /// <summary>
        /// Current configuration
        /// </summary>
        public DiscoveryConfigModel Configuration {
            get => Options.Configuration;
            set => Options.UpdateFromModel(Options.Mode, value);
        }

        /// <summary>
        /// Discovery configuration
        /// </summary>
        public OpcUaDiscoveryOptions Options { get; set; } = OpcUaDiscoveryOptions.Default;

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="logger"></param>
        public OpcUaDiscoveryServices(IOpcUaClient client, IEventEmitter events,
            ITaskScheduler scheduler, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _discovered = new SortedDictionary<DateTime, List<ApplicationRegistrationModel>>();
            _setupDelay = TimeSpan.FromSeconds(10);
            _lock = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Update discovery mode
        /// </summary>
        /// <returns></returns>
        public async Task ScanAsync() {
            try {
                await _lock.WaitAsync();
                await StopAsync();
                if (Mode != DiscoveryMode.Off) {
                    _discovery = new CancellationTokenSource();
                    _completed = _scheduler.Run(() =>
                        RunAsync(Options.Clone(), _setupDelay, _discovery.Token));
                    _setupDelay = null;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Dispose discovery services
        /// </summary>
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
        /// Scan and discover in continuous loop
        /// </summary>
        /// <param name="ct"></param>
        private async Task RunAsync(OpcUaDiscoveryOptions options, TimeSpan? delay,
            CancellationToken ct) {

            if (delay != null) {
                try {
                    _logger.Debug($"Delaying for {delay}...", () => { });
                    await Task.Delay((TimeSpan)delay, ct);
                }
                catch (OperationCanceledException) {
                    return;
                }
            }

            // Run scans until cancelled
            while (!ct.IsCancellationRequested) {
                try {
                    //
                    // Discover
                    //
                    var discovered = await DiscoverServersAsync(options, ct);
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
                    await UploadResultsAsync(discovered, timestamp, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Debug("Cancelled discovery run.", () => { });
                    return;
                }
                catch (Exception ex) {
                    _logger.Error("Error during discovery run.", () => ex);
                }

                //
                // Delay next run
                //
                try {
                    if (!ct.IsCancellationRequested) {
                        GC.Collect();
                        var idle = options.Configuration.IdleTimeBetweenScans ??
                            TimeSpan.FromMinutes(3);
                        if (idle.Ticks != 0) {
                            _logger.Debug($"Idle for {idle}...", () => { });
                            await Task.Delay(idle, ct);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    return;
                }
            }
        }

        /// <summary>
        /// Run a network discovery
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<List<ApplicationRegistrationModel>> DiscoverServersAsync(
            OpcUaDiscoveryOptions options, CancellationToken ct) {
            var discoveryUrls = new ConcurrentQueue<Tuple<Uri, IPEndPoint>>();
            if (options.Mode == DiscoveryMode.Off) {
                return new List<ApplicationRegistrationModel>();
            }

            var watch = Stopwatch.StartNew();
            _logger.Info("Start discovery run...", () => { });

            //
            // Set up scanner pipeline and start discovery
            //
            var local = options.Mode == DiscoveryMode.Local;
#if !NO_SCHEDULER_DUMP
            _counter = 0;
#endif
            var addresses = new List<IPAddress>();
            using (var netscanner = new NetworkScanner(_logger, reply => {
                _logger.Debug($"{reply.Address} found.", () => { });
                addresses.Add(reply.Address);
            }, local, local ? null : options.AddressRanges, options.NetworkClass,
                options.Configuration.MaxNetworkProbes,
                options.Configuration.NetworkProbeTimeout, ct))
            using (var progress = new Timer(_ => ProgressTimer(() =>
                $"Scanned {netscanner.ScanCount} addresses " +
                $"(Active probes: {netscanner.ActiveProbes})..."),
                null, _progressInterval, _progressInterval)) {
                await netscanner.Completion;
            }
            ct.ThrowIfCancellationRequested();

            _logger.Info(
                $"Finding {addresses.Count} addresses took {watch.Elapsed}...",
                    () => { });
            if (addresses.Count == 0) {
                return new List<ApplicationRegistrationModel>();
            }

            var ports = new List<IPEndPoint>();
            var probe = new OpcUaServerProbe(_logger);
#if !NO_SCHEDULER_DUMP
            _counter = 0;
#endif
            using (var portscan = new PortScanner(_logger,
                addresses.SelectMany(address => {
                    var ranges = options.PortRanges;
                    if (ranges == null) {
                        if (options.Mode == DiscoveryMode.Local) {
                            ranges = PortRange.All;
                        }
                        else {
                            ranges = PortRange.OpcUa;
                        }
                        if (options.Mode == DiscoveryMode.Scan) {
                            ranges = ranges.Concat(PortRange.Unassigned);
                        }
                    }
                    return ranges.SelectMany(x => x.GetEndpoints(address));
                }), ports.Add, probe, options.Configuration.MaxPortProbes,
                options.Configuration.MinPortProbesPercent,
                options.Configuration.PortProbeTimeout, ct))
            using (var progress = new Timer(_ => ProgressTimer(() =>
                $"Scanned {portscan.ScanCount} ports " +
                $"(Active probes: {portscan.ActiveProbes})..."),
                null, _progressInterval, _progressInterval)) {
                await portscan.Completion;
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

                // Get host
                string host;
                try {
                    // TODO: Currently stack has issues with ipv4/6 use, specify ipv4
                    // address...
                    // var entry = await Dns.GetHostEntryAsync(ep.Address);
                    // host = entry.HostName ?? ep.Address.ToString();
                    host = ep.Address.ToString();
                }
                catch {
                    host = ep.Address.ToString();
                }
                var url = new Uri($"opc.tcp://{host}:{ep.Port}");
                discoveryUrls.Enqueue(Tuple.Create(url, ep));
            }
            ct.ThrowIfCancellationRequested();

            //
            // Create application model list from discovered endpoints...
            //
            var discovered = new List<ApplicationRegistrationModel>();
            var supervisorId = SupervisorModelEx.CreateSupervisorId(_events.DeviceId,
                _events.ModuleId);
            foreach (var item in discoveryUrls) {
                ct.ThrowIfCancellationRequested();
                var url = item.Item1;
                var eps = await _client.DiscoverAsync(url, ct).ConfigureAwait(false);
                if (!eps.Any()) {
                    continue;
                }
                _logger.Info($"Found {eps.Count()} endpoints on {url.Host}:{url.Port}.",
                    () => { });
                // Merge results...
                foreach (var ep in eps) {
                    discovered.AddOrUpdate(ep.ToServiceModel(item.Item2.ToString(),
                        _events.SiteId, supervisorId));
                }
            }
            ct.ThrowIfCancellationRequested();
            _logger.Info($"Discovery took {watch.Elapsed} and found " +
                $"{discovered.Count} servers.", () => { });
            return discovered;
        }

        /// <summary>
        /// Upload results
        /// </summary>
        /// <param name="discovered"></param>
        /// <param name="timestamp"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task UploadResultsAsync(List<ApplicationRegistrationModel> discovered,
            DateTime timestamp, CancellationToken ct) {
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
                    TimeStamp = timestamp
                })
                .Select((discovery, i) => {
                    discovery.Index = i;
                    return discovery;
                })
                .Select(discovery => Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(discovery)));
            await Task.Run(() => _events.SendAsync(
                messages, "application/x-discovery-v1-json"), ct);
            _logger.Info($"{discovered.Count} results uploaded.", () => { });
        }

        /// <summary>
        /// Called in intervals to check and log progress.
        /// </summary>
        /// <param name="message"></param>
        private void ProgressTimer(Func<string> message) {
#if !NO_SCHEDULER_DUMP
            if ((++_counter % 500) == 0) {
                _scheduler.Dump(_logger);
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
        private readonly ITaskScheduler _scheduler;
        private readonly IOpcUaClient _client;

        private const int kPortScanBatchSize = 10000;
    }
}
