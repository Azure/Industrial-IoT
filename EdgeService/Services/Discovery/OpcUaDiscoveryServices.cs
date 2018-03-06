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
    using System.Net.Sockets;
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
        public TimeSpan DiscoverIdleTime { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Max parallel threads to execute discovery process
        /// </summary>
        public int MaxDegreeOfParallism { get; set; } = Environment.ProcessorCount / 4;

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="logger"></param>
        public OpcUaDiscoveryServices(IOpcUaClient client, IEventEmitter events,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _discovered = new SortedDictionary<DateTime, List<ServerModel>>();
            _lock = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Start discovery
        /// </summary>
        /// <returns></returns>
        public async Task StartDiscoveryAsync() {
            if (_completed != null) {
                return;
            }
            try {
                await _lock.WaitAsync();
                if (_completed == null) {
                    _discovery = new CancellationTokenSource();
                    _completed = Task.Run(() => RunAsync(_discovery.Token));
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop discovery
        /// </summary>
        /// <returns></returns>
        public async Task StopDiscoveryAsync() {
            if (_completed == null) {
                return;
            }
            Task completed;
            try {
                await _lock.WaitAsync();
                if (_completed == null) {
                    return;
                }
                completed = _completed;
                _discovery.Cancel();
                _completed = null;
                _discovery = null;
                _discovered.Clear();
            }
            finally {
                _lock.Release();
            }
            await completed;
        }

        /// <summary>
        /// Dispose discovery services
        /// </summary>
        public void Dispose() {
            StopDiscoveryAsync().Wait();
        }

        /// <summary>
        /// Scan and discover in continuous loop
        /// </summary>
        /// <param name="ct"></param>
        private async Task RunAsync(CancellationToken ct) {
            // Run scans until cancelled
            while (!ct.IsCancellationRequested) {
                try {
                    var timestamp = DateTime.UtcNow;

                    //
                    // Discover
                    //
                    var discovered = await RunNetworkDiscoveryAsync(ct);

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
                                .Select(endpoint => new ServerEndpointDiscoveryModel {
                                    ServerEndpoint = new ServerEndpointModel {
                                        Server = server.Server,
                                        Endpoint = endpoint
                                    },
                                    TimeStamp = timestamp
                                }))
                            .Append(new ServerEndpointDiscoveryModel {
                                ServerEndpoint = null, // last
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
                        await Task.Delay(DiscoverIdleTime, ct);
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
        private async Task<List<ServerModel>> RunNetworkDiscoveryAsync(
            CancellationToken ct) {
            var discovered = new List<ServerModel>();
            var watch = Stopwatch.StartNew();

            //
            // Scan for well known opc ports
            //
            var portscan = new TransformManyBlock<PingReply, Uri>(async ping => {
                var args = WellKnownEndpoints(ping.Address)
                    .Select(ep => {
                        var tcs = new TaskCompletionSource<IPEndPoint>();
                        var arg = new SocketAsyncEventArgs { RemoteEndPoint = ep };
                        arg.Completed += (s, e) => {
                            if (e.SocketError == SocketError.Success) {
                                tcs.TrySetResult((IPEndPoint)e.RemoteEndPoint);
                            }
                            else {
                                tcs.TrySetResult(null);
                            }
                        };
                        if (!Socket.ConnectAsync(SocketType.Stream,
                            ProtocolType.IP, arg)) {
                            tcs.TrySetResult(null);
                        }
                        return Tuple.Create(arg, tcs.Task);
                    });
                var cts = new CancellationTokenSource(3000);
                cts.Token.Register(() => {
                    foreach (var arg in args.Where(t => !t.Item2.IsCompleted)) {
                        Socket.CancelConnectAsync(arg.Item1);
                    }
                });
                var results = await Task.WhenAll(args.Select(t => t.Item2));
                cts.Dispose();
                foreach (var arg in args) { arg.Item1.ConnectSocket?.Dispose(); }
                var ports = results.Where(ep => ep != null).Select(ep => ep.Port);
                if (!ports.Any()) {
                    // No ports open
                    return Enumerable.Empty<Uri>();
                }
                // Get host
                string host;
                try {
                    var entry = await Dns.GetHostEntryAsync(ping.Address);
                    host = entry.HostName ?? ping.Address.ToString();
                }
                catch {
                    host = ping.Address.ToString();
                }
                return ports.Select(port => new Uri($"opc.tcp://{host}:{port}"));
            }, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = MaxDegreeOfParallism,
                CancellationToken = ct
            });

            //
            // Discover on discovery url
            //
            var discover = new ActionBlock<Uri>(async url => {
                var results = await _client.DiscoverAsync(url, ct);
                if (results.Any()) {
                    _logger.Info($"Found {results.Count()} endpoints on {url}.",
                        () => { });
                }

                // Merge results...
                foreach (var result in results) {
                    discovered.AddOrUpdate(result.ToServiceModel(_events.DeviceId));
                }

            }, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = MaxDegreeOfParallism,
                CancellationToken = ct
            });

            //
            // Set up scanner pipeline and start discovery
            //
            portscan.LinkTo(discover, new DataflowLinkOptions { PropagateCompletion = true });
            using (var scanner = new NetworkScanner(_logger, portscan, 3000, ct)) {
                await discover.Completion;
            }
            ct.ThrowIfCancellationRequested();
            _logger.Info($"Discovery took {watch.Elapsed} and " +
                $"found {discovered.Count} servers.", () => { });
            return discovered;
        }

        /// <summary>
        /// Helper to yield well known ports
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static IEnumerable<IPEndPoint> WellKnownEndpoints(
            IPAddress address) {
            yield return new IPEndPoint(address, 4840);
            yield return new IPEndPoint(address, 4841);
            yield return new IPEndPoint(address, 51210);
            yield return new IPEndPoint(address, 61210);
            yield return new IPEndPoint(address, 443);
        }

        private CancellationTokenSource _discovery;
        private SortedDictionary<DateTime, List<ServerModel>> _discovered;
        private Task _completed;

        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly IEventEmitter _events;
        private readonly IOpcUaClient _client;
    }
}
