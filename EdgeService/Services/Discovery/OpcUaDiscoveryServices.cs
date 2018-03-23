// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.Devices.Edge;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Provides discovery services for the supervisor
    /// </summary>
    public class OpcUaDiscoveryServices : IOpcUaDiscoveryServices, IDisposable {

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        public TimeSpan? DiscoveryIdleTime { get; set; }

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
            _discovered = new SortedDictionary<DateTime, List<ApplicationModel>>();
            _lock = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Update discovery mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task SetDiscoveryModeAsync(DiscoveryMode mode) {
            try {
                await _lock.WaitAsync();
                if (Options.Mode != mode) {
                    Options.Mode = mode;
                    await RestartAsync();
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Update scan configuration
        /// </summary>
        /// <param name="addressRanges"></param>
        /// <param name="portRanges"></param>
        /// <param name="discoveryIdleTime"></param>
        /// <returns></returns>
        public async Task UpdateScanConfigurationAsync(string addressRanges,
            string portRanges, TimeSpan? discoveryIdleTime) {
            try {
                await _lock.WaitAsync();
                DiscoveryIdleTime = discoveryIdleTime;
                var restart = Options.Update(addressRanges, portRanges);
                if (restart) {
                    await RestartAsync();
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
        /// Start discovery
        /// </summary>
        /// <returns></returns>
        private async Task RestartAsync() {
            await StopAsync();
            if (Options.Mode != DiscoveryMode.Off) {
                _discovery = new CancellationTokenSource();
                _completed = _scheduler.Run(() =>
                    RunAsync(Options.Clone(), _discovery.Token));
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
        private async Task RunAsync(OpcUaDiscoveryOptions options, CancellationToken ct) {
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
                    try {
                        await _lock.WaitAsync();
                        _discovered.Add(timestamp, discovered);
                        if (_discovered.Count > 10) {
                            _discovered.Remove(_discovered.First().Key);
                        }
                    }
                    finally {
                        _lock.Release();
                    }

                    //
                    // Upload results
                    //
                    try {
                        var messages = discovered
                            .SelectMany(server => server.Endpoints
                                .Select(endpoint => new DiscoveryEventModel {
                                    Application = server.Application,
                                    Endpoint = endpoint,
                                    TimeStamp = timestamp
                                }))
                            .Append(new DiscoveryEventModel {
                                Endpoint = null, // last
                                TimeStamp = timestamp
                            })
                            .Select((discovery, i) => {
                                discovery.Index = i;
                                return discovery;
                            })
                            .Select(discovery => Encoding.UTF8.GetBytes(
                                JsonConvertEx.SerializeObject(discovery)));
                        await _events.SendAsync(messages, "application/x-discovery-v1-json");
                    }
                    catch (Exception ex) {
                        _logger.Error("Error during discovery upload", () => ex);
                    }
                }
                catch (TaskCanceledException) {
                    return;
                }
                catch (Exception ex) {
                    _logger.Error("Error during network discovery run", () => ex);
                }

                //
                // Delay next run
                //
                try {
                    if (!ct.IsCancellationRequested) {
                        GC.Collect();
                        await Task.Delay(DiscoveryIdleTime ?? TimeSpan.FromMinutes(3), ct);
                    }
                }
                catch (TaskCanceledException) {
                    return;
                }
            }
        }

        /// <summary>
        /// Run a network discovery
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<List<ApplicationModel>> DiscoverServersAsync(
            OpcUaDiscoveryOptions options, CancellationToken ct) {
            var discovered = new List<ApplicationModel>();
            if (options.Mode == DiscoveryMode.Off) {
                return discovered;
            }

            var watch = Stopwatch.StartNew();
            _logger.Info("Start discovery sweep...", () => { });

            //
            // Select ports to scan from opc ua range and unassigned
            //
            var portranges = new TransformManyBlock<PingReply, IEnumerable<IPEndPoint>>(
                reply => {
                    Console.WriteLine(reply.Address);
                    var ranges = PortRange.OpcUa;
                    if (options.Mode == DiscoveryMode.Scan) {
                        ranges = ranges.Concat(PortRange.Unassigned);
                    }
                    else if (options.PortRanges != null) {
                        ranges = ranges.Concat(options.PortRanges);
                    }
                    return ranges.SelectMany(x => x.GetEndpoints(reply.Address))
#if FALSE
                        .Distinct()
#endif
#if TRUE
                        .Batch(kPortScanBatchSize);
#else
                        .YieldReturn();
#endif
                }, new ExecutionDataflowBlockOptions {
                    MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                    BoundedCapacity = options.MaxDegreeOfParallelism,
                    CancellationToken = ct
                });

            //
            // Search discovery endpoint on endpoint address
            //
            var discovery = new ActionBlock<IPEndPoint>(
                async ep => {
                    // Get host
                    string host;
                    try {
                        var entry = await Dns.GetHostEntryAsync(ep.Address);
                        host = entry.HostName ?? ep.Address.ToString();
                    }
                    catch {
                      host = ep.Address.ToString();
                    }

                    // Connect to endpoint
                    var url = new Uri($"opc.tcp://{host}:{ep.Port}");
#if TRACE
                    _logger.Debug($"Try discovery on {url}...", () => { });
#endif
                    var results = await _client.DiscoverAsync(url, ct)
                        .ConfigureAwait(false);
                    if (results.Any()) {
                        _logger.Info($"Found {results.Count()} endpoints on {url}.",
                            () => { });
                    }

                    // Merge results...
                    foreach (var result in results) {
                        discovered.AddOrUpdate(result.ToServiceModel(_events.DeviceId));
                    }

                }, new ExecutionDataflowBlockOptions {
                    MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                    CancellationToken = ct
                });

            //
            // Set up scanner pipeline and start discovery
            //
            var local = options.Mode == DiscoveryMode.Local;
            var probe = new OpcUaServerProbe(_logger);
            using (var portscan = new PortScanner(_logger, portranges, discovery, probe, ct))
            using (var netscanner = new NetworkScanner(_logger, portranges, local,
                local ? null : options.AddressRanges, options.NetworkClass, ct)) {
                await discovery.Completion;
            }
            ct.ThrowIfCancellationRequested();
            _logger.Info($"Discovery took {watch.Elapsed} and found {discovered.Count} " +
                $"servers.", () => { });
            return discovered;
        }

        private CancellationTokenSource _discovery;
        private Task _completed;

        private readonly SortedDictionary<DateTime, List<ApplicationModel>> _discovered;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly IEventEmitter _events;
        private readonly ITaskScheduler _scheduler;
        private readonly IOpcUaClient _client;

        private const int kPortScanBatchSize = 10000;
    }
}
