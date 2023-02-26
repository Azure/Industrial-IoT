// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Probe;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Scanner;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.Abstractions;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Module.Framework.Client;

    /// <summary>
    /// Provides discovery services
    /// </summary>
    public sealed class DiscoveryServices : IDiscoveryServices, IServerDiscovery, IDisposable
    {
        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="progress"></param>
        /// <param name="identity"></param>
        public DiscoveryServices(IEndpointDiscovery client, IClientAccessor events,
            IJsonSerializer serializer, ILogger logger, IDiscoveryProgress progress = null,
            IProcessInfo identity = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _progress = progress ?? new ProgressLogger(logger);
            _identity = identity;
            _runner = Task.Run(() => ProcessDiscoveryRequestsAsync(_cts.Token));
            _timer = new Timer(_ => OnScanScheduling(), null,
                TimeSpan.FromSeconds(20), Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel endpoint, CancellationToken ct)
        {
            if (endpoint?.DiscoveryUrl == null)
            {
                throw new ArgumentException("Discovery url missing", nameof(endpoint));
            }

            var discoveryUrl = new Uri(endpoint.DiscoveryUrl);

            // Find endpoints at the real accessible ip address
            var eps = await _client.FindEndpointsAsync(discoveryUrl, null,
                ct).ConfigureAwait(false);

            // Match endpoints
            foreach (var ep in eps)
            {
                if ((ep.Description.SecurityMode.ToServiceType() ?? SecurityMode.None)
                    != (endpoint.SecurityMode ?? SecurityMode.None))
                {
                    // no match
                    continue;
                }
                if (endpoint.SecurityPolicy != null &&
                    endpoint.SecurityPolicy != ep.Description.SecurityPolicyUri)
                {
                    // no match
                    continue;
                }
                if (endpoint.Certificate != null &&
                    endpoint.Certificate != ep.Description.ServerCertificate.ToThumbprint())
                {
                    // no match
                    continue;
                }
                return ep.ToServiceModel(discoveryUrl.Host,
                    _identity.SiteId, _identity.ProcessId, _identity.Id, _serializer);
            }
            throw new ResourceNotFoundException("Endpoints could not be found.");
        }

        /// <inheritdoc/>
        public Task RegisterAsync(ServerRegistrationRequestModel request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request, CancellationToken ct)
        {
            kDiscoverAsync.Inc();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var task = new DiscoveryRequest(request);
            var scheduled = _queue.TryAdd(task);
            if (!scheduled)
            {
                task.Dispose();
                _logger.LogError("Discovey request not scheduled, internal server error!");
                var ex = new ResourceExhaustionException("Failed to schedule task");
                _progress.OnDiscoveryError(request, ex);
                throw ex;
            }
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_pending.Count != 0)
                {
                    _progress.OnDiscoveryPending(task.Request, _pending.Count);
                }
                _pending.Add(task);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request, CancellationToken ct)
        {
            kCancelAsync.Inc();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                foreach (var task in _pending.Where(r => r.Request.Id == request.Id))
                {
                    // Cancel the task
                    task.Cancel();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Try.Async(StopDiscoveryRequestProcessingAsync).Wait();

            // Dispose
            _cts.Dispose();
            _timer.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// Scan timer expired
        /// </summary>
        private void OnScanScheduling()
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _lock.Wait();
            try
            {
                foreach (var task in _pending.Where(r => r.IsScan))
                {
                    // Cancel any current scan tasks if any
                    task.Cancel();
                }

                // Add new discovery request
                if (_request.Mode != DiscoveryMode.Off)
                {
                    // Push request
                    var task = _request.Clone();
                    if (_queue.TryAdd(task))
                    {
                        _pending.Add(task);
                    }
                    else
                    {
                        task.Dispose();
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop discovery request processing
        /// </summary>
        /// <returns></returns>
        private async Task StopDiscoveryRequestProcessingAsync()
        {
            _queue.CompleteAdding();
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Cancel all requests first
                foreach (var request in _pending)
                {
                    request.Cancel();
                }
            }
            finally
            {
                _lock.Release();
            }

            // Try cancel discovery and wait for completion of runner
            Try.Op(() => _cts?.Cancel());
            try
            {
                await _runner.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception stopping processor thread.");
            }
        }

        /// <summary>
        /// Process discovery requests
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ProcessDiscoveryRequestsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting discovery processor...");
            // Process all discovery requests
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var request = _queue.Take(ct);
                    try
                    {
                        // Update pending queue size
                        await ReportPendingRequestsAsync().ConfigureAwait(false);
                        await ProcessDiscoveryRequestAsync(request).ConfigureAwait(false);
                    }
                    finally
                    {
                        // If the request is scan request, schedule next one
                        if (!ct.IsCancellationRequested && (request?.IsScan ?? false))
                        {
                            // Re-schedule another scan when idle time expired
                            _timer.Change(
                                request.Request.Configuration.IdleTimeBetweenScans ??
                                    TimeSpan.FromHours(1),
                                Timeout.InfiniteTimeSpan);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Discovery processor error occurred - continue...");
                }
            }
            // Send cancellation for all pending items
            await CancelPendingRequestsAsync().ConfigureAwait(false);
            _logger.LogInformation("Stopped discovery processor.");
        }

        /// <summary>
        /// Process the provided discovery request
        /// </summary>
        /// <param name="request"></param>
        private async Task ProcessDiscoveryRequestAsync(DiscoveryRequest request)
        {
            _logger.LogDebug("Processing discovery request...");
            _progress.OnDiscoveryStarted(request.Request);
            object diagnostics = null;

            //
            // Discover servers
            //
            List<ApplicationRegistrationModel> discovered;
            try
            {
                discovered = await DiscoverServersAsync(request).ConfigureAwait(false);
                request.Token.ThrowIfCancellationRequested();
                //
                // Upload results
                //
                await SendDiscoveryResultsAsync(request, discovered, DateTime.UtcNow,
                    diagnostics, request.Token).ConfigureAwait(false);

                _progress.OnDiscoveryFinished(request.Request);
            }
            catch (OperationCanceledException)
            {
                _progress.OnDiscoveryCancelled(request.Request);
            }
            catch (Exception ex)
            {
                _progress.OnDiscoveryError(request.Request, ex);
            }
            finally
            {
                if (request != null)
                {
                    await _lock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        _pending.Remove(request);
                        Try.Op(() => request.Dispose());
                    }
                    finally
                    {
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
            DiscoveryRequest request)
        {
            var discoveryUrls = await GetDiscoveryUrlsAsync(request.DiscoveryUrls).ConfigureAwait(false);
            if (request.Mode == DiscoveryMode.Off)
            {
                return await DiscoverServersAsync(request, discoveryUrls,
                    request.Configuration.Locales).ConfigureAwait(false);
            }

            _logger.LogInformation("Start {Mode} discovery run...", request.Mode);
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
            using (var netscanner = new NetworkScanner(_logger, (scanner, reply) =>
            {
                _progress.OnNetScanResult(request.Request, scanner.ActiveProbes,
                    scanner.ScanCount, request.TotalAddresses, addresses.Count, reply.Address);
                addresses.Add(reply.Address);
            }, local, local ? null : request.AddressRanges, request.NetworkClass,
                request.Configuration.MaxNetworkProbes, request.Configuration.NetworkProbeTimeout,
                request.Token))
            {
                // Log progress
                using (var progress = new Timer(_ => ProgressTimer(
                    () => _progress.OnNetScanProgress(request.Request, netscanner.ActiveProbes,
                        netscanner.ScanCount, request.TotalAddresses, addresses.Count)),
                    null, kProgressInterval, kProgressInterval))
                {
                    await netscanner.WaitToCompleteAsync().ConfigureAwait(false);
                }
                _progress.OnNetScanFinished(request.Request, netscanner.ActiveProbes,
                    netscanner.ScanCount, request.TotalAddresses, addresses.Count);
            }
            request.Token.ThrowIfCancellationRequested();

            await AddLoopbackAddressesAsync(addresses).ConfigureAwait(false);
            if (addresses.Count == 0)
            {
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
                addresses.SelectMany(address =>
                {
                    var ranges = request.PortRanges ?? PortRange.OpcUa;
                    return ranges.SelectMany(x => x.GetEndpoints(address));
                }), (scanner, ep) =>
                {
                    _progress.OnPortScanResult(request.Request, scanner.ActiveProbes,
                        scanner.ScanCount, totalPorts, ports.Count, ep);
                    ports.Add(ep);
                }, probe, request.Configuration.MaxPortProbes,
                request.Configuration.MinPortProbesPercent,
                request.Configuration.PortProbeTimeout, request.Token))
            {
                using (var progress = new Timer(_ => ProgressTimer(
                    () => _progress.OnPortScanProgress(request.Request, portscan.ActiveProbes,
                        portscan.ScanCount, totalPorts, ports.Count)),
                    null, kProgressInterval, kProgressInterval))
                {
                    await portscan.WaitToCompleteAsync().ConfigureAwait(false);
                }
                _progress.OnPortScanFinished(request.Request, portscan.ActiveProbes,
                    portscan.ScanCount, totalPorts, ports.Count);
            }
            request.Token.ThrowIfCancellationRequested();
            if (ports.Count == 0)
            {
                return new List<ApplicationRegistrationModel>();
            }

            //
            // Collect discovery urls
            //
            foreach (var ep in ports)
            {
                request.Token.ThrowIfCancellationRequested();
                var resolved = await ep.TryResolveAsync().ConfigureAwait(false);
                var url = new Uri("opc.tcp://" + resolved);
                discoveryUrls.AddOrUpdate(ep, url);
            }
            request.Token.ThrowIfCancellationRequested();

            //
            // Create application model list from discovered endpoints...
            //
            var discovered = await DiscoverServersAsync(request, discoveryUrls,
                request.Configuration.Locales).ConfigureAwait(false);

            _logger.LogInformation("Discovery took {Elapsed} and found {Count} servers.",
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
            List<string> locales)
        {
            kDiscoverServersAsync.Inc();
            var discovered = new List<ApplicationRegistrationModel>();
            var count = 0;
            _progress.OnServerDiscoveryStarted(request.Request, 1, count, discoveryUrls.Count);
            foreach (var item in discoveryUrls)
            {
                request.Token.ThrowIfCancellationRequested();
                var url = item.Value;

                _progress.OnFindEndpointsStarted(request.Request, 1, count, discoveryUrls.Count,
                    discovered.Count, url, item.Key.Address);

                // Find endpoints at the real accessible ip address
                var eps = await _client.FindEndpointsAsync(new UriBuilder(url)
                {
                    Host = item.Key.Address.ToString()
                }.Uri, locales, request.Token).ConfigureAwait(false);

                count++;
                var endpoints = 0;
                foreach (var ep in eps)
                {
                    discovered.AddOrUpdate(ep.ToServiceModel(item.Key.ToString(),
                        _identity.SiteId, _identity.ProcessId, _identity.Id, _serializer));
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
            IEnumerable<Uri> discoveryUrls)
        {
            var result = new Dictionary<IPEndPoint, Uri>();
            if (discoveryUrls?.Any() ?? false)
            {
                var results = await Task.WhenAll(discoveryUrls
                    .Select(GetHostEntryAsync)
                    .ToArray()).ConfigureAwait(false);
                foreach (var entry in results
                    .SelectMany(v => v)
                    .Where(a => a.Item2 != null))
                {
                    result.AddOrUpdate(entry.Item1, entry.Item2);
                }
            }
            return result;
        }

        /// <summary>
        /// Get a reachable host address from url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        private Task<List<Tuple<IPEndPoint, Uri>>> GetHostEntryAsync(
            Uri discoveryUrl)
        {
            return Try.Async(async () =>
            {
                var host = discoveryUrl.DnsSafeHost;
                var list = new List<Tuple<IPEndPoint, Uri>>();

                // check first if host is an IP Address since the Dns.GetHostEntryAsync
                // throws a socket exception when called with an IP address
                try
                {
                    var hostIp = IPAddress.Parse(host);
                    var ep = new IPEndPoint(hostIp,
                            discoveryUrl.IsDefaultPort ? 4840 : discoveryUrl.Port);
                    list.Add(Tuple.Create(ep, discoveryUrl));
                    return list;
                }
                catch
                {
                    // Parsing failed, therefore not an IP address, continue with dns
                    // resolution
                }

                while (!string.IsNullOrEmpty(host))
                {
                    try
                    {
                        var entry = await Dns.GetHostEntryAsync(host).ConfigureAwait(false);
                        // only pick-up the IPV4 addresses
                        var foundIpv4 = false;
                        foreach (var address in entry.AddressList
                            .Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                        {
                            var ep = new IPEndPoint(address,
                                discoveryUrl.IsDefaultPort ? 4840 : discoveryUrl.Port);
                            list.Add(Tuple.Create(ep, discoveryUrl));
                            foundIpv4 = true;
                        }
                        if (!foundIpv4)
                        {
                            // if no IPV4 responsive, try IPV6 as fallback
                            foreach (var address in entry.AddressList
                                .Where(a => a.AddressFamily != AddressFamily.InterNetwork))
                            {
                                var ep = new IPEndPoint(address,
                                    discoveryUrl.IsDefaultPort ? 4840 : discoveryUrl.Port);
                                list.Add(Tuple.Create(ep, discoveryUrl));
                            }
                        }

                        // Check local host
                        if (host.EqualsIgnoreCase("localhost") && Host.IsContainer)
                        {
                            // Also resolve docker internal since we are in a container
                            host = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_GATEWAYHOSTNAME);
                            continue;
                        }
                        break;
                    }
                    catch (SocketException se)
                    {
                        _logger.LogWarning("Failed to resolve the host for {DiscoveryUrl} due to {Message}",
                            discoveryUrl, se.Message);
                        return list;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to resolve the host for {DiscoveryUrl}", discoveryUrl);
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
        private async Task AddLoopbackAddressesAsync(List<IPAddress> addresses)
        {
            // Check local host
            var hostName = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_GATEWAYHOSTNAME);
            try
            {
                if (Host.IsContainer)
                {
                    // Resolve docker host since we are running in a container
                    if (string.IsNullOrEmpty(hostName))
                    {
                        _logger.LogInformation("Gateway host name not set");
                        return;
                    }
                    _logger.LogDebug("Resolve IP for gateway host name: {Address}", hostName);
                    var entry = await Dns.GetHostEntryAsync(hostName).ConfigureAwait(false);
                    foreach (var address in entry.AddressList
                                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                                .Where(a => !addresses.Any(b => a.Equals(b))))
                    {
                        _logger.LogInformation("Including gateway host address {Address}", address);
                        addresses.Add(address);
                    }
                }
                else
                {
                    // Add loopback address
                    addresses.Add(IPAddress.Loopback);
                }
            }
            catch (SocketException se)
            {
                _logger.LogWarning("Failed to add address for gateway host {HostName} due to {Error}.",
                    hostName, se.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add address for gateway host {HostName}.", hostName);
            }
        }

        /// <summary>
        /// Upload results
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discovered"></param>
        /// <param name="timestamp"></param>
        /// <param name="diagnostics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendDiscoveryResultsAsync(DiscoveryRequest request,
            List<ApplicationRegistrationModel> discovered, DateTime timestamp,
            object diagnostics, CancellationToken ct)
        {
            var client = _events.Client;
            if (client == null)
            {
                return;
            }
            _logger.LogInformation("Uploading {Count} results...", discovered.Count);
            var messages = discovered
                .SelectMany(server => server.Endpoints
                    .Select(registration => new DiscoveryEventModel
                    {
                        Application = server.Application,
                        Registration = registration,
                        TimeStamp = timestamp
                    }))
                .Append(new DiscoveryEventModel
                {
                    Registration = null, // last
                    Result = new DiscoveryResultModel
                    {
                        DiscoveryConfig = request.Configuration,
                        Id = request.Request.Id,
                        Context = request.Request.Context,
                        RegisterOnly = request.Mode == DiscoveryMode.Off,
                        Diagnostics = diagnostics == null ? null :
                            _serializer.FromObject(diagnostics)
                    },
                    TimeStamp = timestamp
                })
                .Select((discovery, i) =>
                {
                    discovery.Index = i;
                    return _serializer.SerializeToMemory(discovery).ToArray();
                })
                .ToList();

            using var message = client.CreateMessage(messages, "utf-8", ContentMimeType.Json,
                MessageSchemaTypes.DiscoveryEvents);
            await client.SendEventAsync(message).ConfigureAwait(false);
            _logger.LogInformation("{Count} results uploaded.", discovered.Count);
        }

        /// <summary>
        /// Cancel all remaining pending requests
        /// </summary>
        /// <returns></returns>
        private async Task CancelPendingRequestsAsync()
        {
            _logger.LogInformation("Cancelling all pending requests...");
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var request in _pending)
                {
                    _progress.OnDiscoveryCancelled(request.Request);
                    Try.Op(() => request.Dispose());
                }
                _pending.Clear();
            }
            finally
            {
                _lock.Release();
            }
            _logger.LogInformation("Pending requests cancelled...");
        }

        /// <summary>
        /// Send pending queue size
        /// </summary>
        /// <returns></returns>
        private async Task ReportPendingRequestsAsync()
        {
            // Notify all listeners about the request's place in queue
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                for (var pos = 0; pos < _pending.Count; pos++)
                {
                    var item = _pending[pos];
                    if (!item.Token.IsCancellationRequested)
                    {
                        _progress.OnDiscoveryPending(item.Request, pos);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send pending event");
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Called in intervals to check and log progress.
        /// </summary>
        /// <param name="log"></param>
        /// <exception cref="ThreadStateException"></exception>
        private void ProgressTimer(Action log)
        {
            if ((_counter % 3) == 0)
            {
                _logger.LogInformation("GC Mem: {Gcmem} kb, Working set / Private Mem: " +
                    "{Privmem} kb / {Privmemsize} kb, Handles: {Handles}",
                    GC.GetTotalMemory(false) / 1024,
                    Process.GetCurrentProcess().WorkingSet64 / 1024,
                    Process.GetCurrentProcess().PrivateMemorySize64 / 1024,
                    Process.GetCurrentProcess().HandleCount);
            }
            ++_counter;
#if !NO_WATCHDOG
            if ((_counter % 200) == 0)
            {
                if (_counter >= 2000)
                {
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

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly IClientAccessor _events;
        private readonly IDiscoveryProgress _progress;
        private readonly IProcessInfo _identity;
        private readonly IEndpointDiscovery _client;
        private readonly Task _runner;
        private readonly Timer _timer;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly List<DiscoveryRequest> _pending =
            new();
        private readonly BlockingCollection<DiscoveryRequest> _queue =
            new();
        private readonly CancellationTokenSource _cts =
            new();
        private readonly DiscoveryRequest _request = new();

        private const string kDiscoveryMetricsPrefix = "iiot_edge_discovery_";
        private static readonly Counter kDiscoverAsync = Metrics
    .CreateCounter(kDiscoveryMetricsPrefix + "discover", "call to discover");
        private static readonly Counter kCancelAsync = Metrics
    .CreateCounter(kDiscoveryMetricsPrefix + "cancel", "call to cancel");
        private static readonly Counter kDiscoverServersAsync = Metrics
    .CreateCounter(kDiscoveryMetricsPrefix + "discover_servers", "call to discoverServersAsync");
    }
}
