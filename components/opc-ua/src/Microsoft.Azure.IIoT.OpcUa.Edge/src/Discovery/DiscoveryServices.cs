// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport.Probe;
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
    /// Provides discovery services
    /// </summary>
    public class DiscoveryServices : IDiscoveryServices, IDisposable {

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="events"></param>
        /// <param name="processor"></param>
        /// <param name="listener"></param>
        /// <param name="logger"></param>
        public DiscoveryServices(IEndpointDiscovery client, IEventEmitter events,
            ITaskProcessor processor, ILogger logger, IDiscoveryListener listener = null) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _listener = listener ?? new DiscoveryLogger(_logger);
        }

        /// <inheritdoc/>
        public Task DiscoverAsync(DiscoveryRequestModel request, CancellationToken ct) {
            if (request == null) {
                return Task.FromException(new ArgumentNullException(nameof(request)));
            }
            var scheduled = _processor.TrySchedule(() =>
                ProcessDiscoveryRequestAsync(new DiscoveryRequest(request), ct));
            if (scheduled) {
                return Task.CompletedTask;
            }
            _logger.Error("Task not scheduled, internal server error!");
            return Task.FromException(new ResourceExhaustionException(
                "Failed to schedule task"));
        }

        /// <inheritdoc/>
        public void Dispose() {
            // No op
        }

        /// <summary>
        /// Process the provided discovery request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        private async Task ProcessDiscoveryRequestAsync(DiscoveryRequest request,
            CancellationToken ct) {
            _logger.Debug("Processing discovery request...");
            _listener.OnDiscoveryStarted(request.Request);
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
                _listener.OnDiscoveryFinished(request.Request);
            }
            catch (OperationCanceledException) {
                _listener.OnDiscoveryCancelled(request.Request);
            }
            catch (Exception ex) {
                _listener.OnDiscoveryError(request.Request, ex);
            }
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
                    _listener.OnNetScanResult(request.Request, scanner, reply.Address);
                addresses.Add(reply.Address);
            }, local, local ? null : request.AddressRanges, request.NetworkClass,
                request.Configuration.MaxNetworkProbes,
                request.Configuration.NetworkProbeTimeout, ct)) {

                // Log progress
                _listener.OnNetScanStarted(request.Request, netscanner);
                using (var progress = new Timer(_ => ProgressTimer(
                    () => _listener.OnNetScanProgress(request.Request, netscanner,
                        addresses.Count)),
                    null, _progressInterval, _progressInterval)) {
                    await netscanner.Completion;
                }
                _listener.OnNetScanFinished(request.Request, netscanner, addresses.Count);
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
                    _listener.OnPortScanResult(request.Request, scanner, ep);
                    ports.Add(ep);
                }, probe, request.Configuration.MaxPortProbes,
                request.Configuration.MinPortProbesPercent,
                request.Configuration.PortProbeTimeout, ct)) {

                _listener.OnPortScanStart(request.Request, portscan);
                using (var progress = new Timer(_ => ProgressTimer(
                    () => _listener.OnPortScanProgress(request.Request,
                        portscan, ports.Count)),
                    null, _progressInterval, _progressInterval)) {
                    await portscan.Completion;
                }
                _listener.OnPortScanFinished(request.Request, portscan, ports.Count);
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

            _listener.OnServerDiscoveryStarted(request.Request, discoveryUrls);

            foreach (var item in discoveryUrls) {
                ct.ThrowIfCancellationRequested();
                var url = item.Value;

                _listener.OnFindEndpointsStarted(request.Request, url, item.Key.Address);

                // Find endpoints at the real accessible ip address
                var eps = await _client.FindEndpointsAsync(new UriBuilder(url) {
                    Host = item.Key.Address.ToString()
                }.Uri, locales, ct).ConfigureAwait(false);

                var endpoints = eps.Count();
                _listener.OnFindEndpointsFinished(request.Request, url, item.Key.Address,
                    endpoints);
                if (endpoints == 0) {
                    continue;
                }

                // Merge results...
                foreach (var ep in eps) {
                    discovered.AddOrUpdate(ep.ToServiceModel(item.Key.ToString(),
                        _events.SiteId, supervisorId));
                }
            }

            _listener.OnServerDiscoveryFinished(request.Request, discovered.Count);
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
        private readonly ILogger _logger;
        private readonly TimeSpan _progressInterval = TimeSpan.FromSeconds(5);
        private readonly IEventEmitter _events;
        private readonly IDiscoveryListener _listener;
        private readonly ITaskProcessor _processor;
        private readonly IEndpointDiscovery _client;
    }
}
