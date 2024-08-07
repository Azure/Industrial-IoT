// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Azure.IoT.Edge;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public sealed class RuntimeStateReporter : IRuntimeStateReporter,
        IApiKeyProvider, ISslCertProvider, IDisposable
    {
        /// <inheritdoc/>
        public string? ApiKey { get; private set; }

        /// <inheritdoc/>
        public X509Certificate2? Certificate { get; private set; }

        /// <summary>
        /// Constructor for runtime state reporter.
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="stores"></param>
        /// <param name="options"></param>
        /// <param name="collector"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <param name="timeProvider"></param>
        /// <param name="identity"></param>
        /// <param name="workload"></param>
        public RuntimeStateReporter(IEnumerable<IEventClient> events,
            IJsonSerializer serializer, IEnumerable<IKeyValueStore> stores,
            IOptions<PublisherOptions> options, IDiagnosticCollector collector,
            ILogger<RuntimeStateReporter> logger, IMetricsContext? metrics = null,
            TimeProvider? timeProvider = null, IIoTEdgeDeviceIdentity? identity = null,
            IIoTEdgeWorkloadApi? workload = null)
        {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _collector = collector ??
                throw new ArgumentNullException(nameof(collector));
            _metrics = metrics ?? IMetricsContext.Empty;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _workload = workload;
            _identity = identity;
            _renewalTimer = _timeProvider.CreateTimer(OnRenewExpiredCertificateAsync,
                null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            ArgumentNullException.ThrowIfNull(stores);
            ArgumentNullException.ThrowIfNull(events);

            _events = events.Reverse().ToList();
            _stores = stores.Reverse().ToList();
            if (_stores.Count == 0)
            {
                throw new ArgumentException("No key value stores configured.",
                    nameof(stores));
            }
            _runtimeState = RuntimeStateEventType.RestartAnnouncement;
            _topicCache = new ConcurrentDictionary<string, string>();

            _diagnosticInterval = options.Value.DiagnosticsInterval ?? TimeSpan.Zero;
            _diagnostics = options.Value.DiagnosticsTarget
                ?? PublisherDiagnosticTargetType.Logger;
            if (_diagnosticInterval == TimeSpan.Zero)
            {
                _diagnosticInterval = Timeout.InfiniteTimeSpan;
            }

            _cts = new CancellationTokenSource();
            _diagnosticsOutputTimer = new PeriodicTimer(_diagnosticInterval);
            _publisher = DiagnosticsOutputTimerAsync(_cts.Token);

            InitializeMetrics();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _runtimeState = RuntimeStateEventType.Stopped;
                _cts.Cancel();
                _publisher.GetAwaiter().GetResult();
                Certificate?.Dispose();
            }
            finally
            {
                _renewalTimer.Dispose();
                _meter.Dispose();
                _diagnosticsOutputTimer.Dispose();
                _publisher = Task.CompletedTask;
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public async ValueTask SendRestartAnnouncementAsync(CancellationToken ct)
        {
            var hostAddresses = await GetHostAddressesAsync(ct).ConfigureAwait(false);

            // Set runtime state in state stores
            foreach (var store in _stores)
            {
                store.State[OpcUa.Constants.TwinPropertySiteKey] =
                    _options.Value.SiteId;
                store.State[OpcUa.Constants.TwinPropertyTypeKey] =
                    OpcUa.Constants.EntityTypePublisher;
                store.State[OpcUa.Constants.TwinPropertyVersionKey] =
                    GetType().Assembly.GetReleaseVersion().ToString();
                store.State[OpcUa.Constants.TwinPropertyFullVersionKey] =
                    PublisherConfig.Version;
                store.State[OpcUa.Constants.TwinPropertyIpAddressesKey] =
                    hostAddresses;

                if (_options.Value.HttpServerPort.HasValue)
                {
                    store.State[OpcUa.Constants.TwinPropertySchemeKey] =
                        "https";
                    store.State[OpcUa.Constants.TwinPropertyHostnameKey] =
                        Dns.GetHostName();
                    store.State[OpcUa.Constants.TwinPropertyPortKey] =
                        _options.Value.HttpServerPort;
                }
                else
                {
                    store.State[OpcUa.Constants.TwinPropertySchemeKey] =
                        VariantValue.Null;
                    store.State[OpcUa.Constants.TwinPropertyHostnameKey] =
                        VariantValue.Null;
                    store.State[OpcUa.Constants.TwinPropertyPortKey] =
                        VariantValue.Null;
                }
            }

            await UpdateApiKeyAndCertificateAsync().ConfigureAwait(false);

            if (_options.Value.EnableRuntimeStateReporting ?? false)
            {
                var body = new RuntimeStateEventModel
                {
                    TimestampUtc = _timeProvider.GetUtcNow(),
                    MessageVersion = 1,
                    MessageType = RuntimeStateEventType.RestartAnnouncement,
                    PublisherId = _options.Value.PublisherId,
                    SemVer = GetType().Assembly.GetReleaseVersion().ToString(),
                    Version = PublisherConfig.Version,
                    Site = _options.Value.SiteId,
                    DeviceId = _identity?.DeviceId,
                    ModuleId = _identity?.ModuleId
                };

                await SendRuntimeStateEvent(body, ct).ConfigureAwait(false);
                _logger.LogInformation("Restart announcement sent successfully.");
            }

            _runtimeState = RuntimeStateEventType.Running;
        }

        /// <summary>
        /// Get comma seperated host addresses
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<VariantValue> GetHostAddressesAsync(CancellationToken ct)
        {
            try
            {
                var host = await Dns.GetHostEntryAsync(Dns.GetHostName(),
                    ct).ConfigureAwait(false);
                return host.AddressList.Select(ip => ip.ToString())
                    .Aggregate((a, b) => a + ", " + b);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve hostname.");
                return VariantValue.Null;
            }
        }

        /// <summary>
        /// Update cached api key
        /// </summary>
        private async Task UpdateApiKeyAndCertificateAsync()
        {
            var apiKeyStore = _stores.Find(s => s.State.TryGetValue(
                OpcUa.Constants.TwinPropertyApiKeyKey, out var key) && key.IsString);
            if (apiKeyStore != null)
            {
                ApiKey = (string?)apiKeyStore.State[OpcUa.Constants.TwinPropertyApiKeyKey];
                _logger.LogInformation("Api Key exists in {Store} store...", apiKeyStore.Name);
            }

            if (!string.IsNullOrWhiteSpace(_options.Value.ApiKeyOverride) &&
                ApiKey != _options.Value.ApiKeyOverride)
            {
                Debug.Assert(_stores.Count > 0);
                _logger.LogInformation("Using Api Key provided in configuration...");
                ApiKey = _options.Value.ApiKeyOverride;

                _stores[0].State.Add(OpcUa.Constants.TwinPropertyApiKeyKey, ApiKey);
            }

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                Debug.Assert(_stores.Count > 0);

                _logger.LogInformation("Generating new Api Key in {Store} store...",
                    _stores[0].Name);
                ApiKey = RandomNumberGenerator.GetBytes(20).ToBase64String();
                _stores[0].State.Add(OpcUa.Constants.TwinPropertyApiKeyKey, ApiKey);
            }

            var dnsName = Dns.GetHostName();

            // The certificate must be in the same store as the api key or else we generate a new one.
            if (!(_options.Value.RenewTlsCertificateOnStartup ?? false) &&
                apiKeyStore != null &&
                apiKeyStore.State.TryGetValue(OpcUa.Constants.TwinPropertyCertificateKey,
                    out var cert) && cert.IsBytes)
            {
                try
                {
                    // Load certificate
                    Certificate?.Dispose();
                    Certificate = new X509Certificate2((byte[])cert!, ApiKey);
                    var now = _timeProvider.GetUtcNow().AddDays(1);
                    if (now < Certificate.NotAfter && Certificate.HasPrivateKey &&
                        Certificate.SubjectName.EnumerateRelativeDistinguishedNames()
                            .Any(a => a.GetSingleElementValue() == dnsName))
                    {
                        var renewalAfter = Certificate.NotAfter - now;
                        _logger.LogInformation(
                    "Using valid Certificate found in {Store} store (renewal in {Duration})...",
                            apiKeyStore.Name, renewalAfter);
                        _renewalTimer.Change(renewalAfter, Timeout.InfiniteTimeSpan);
                        // Done
                        return;
                    }
                    _logger.LogInformation(
                        "Certificate found in {Store} store has expired. Generate new...",
                        apiKeyStore.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provided Certificate invalid.");
                }
            }

            // Create new certificate
            var nowOffset = _timeProvider.GetUtcNow();
            var expiration = nowOffset.AddDays(kCertificateLifetimeDays);

            Certificate?.Dispose();
            Certificate = null;
            if (_workload != null)
            {
                try
                {
                    var certificates = await _workload.CreateServerCertificateAsync(
                        dnsName, expiration.Date).ConfigureAwait(false);

                    Debug.Assert(certificates.Count > 0);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        using (var certificate = certificates[0])
                        {
                            //
                            // https://github.com/dotnet/runtime/issues/45680)
                            // On Windows the certificate in 'result' gives an error
                            // when used with kestrel: "No credentials are available"
                            //
                            Certificate = new X509Certificate2(
                                certificate.Export(X509ContentType.Pkcs12));
                        }
                    }
                    else
                    {
                        Certificate = certificates[0];
                    }

                    if (!Certificate.HasPrivateKey)
                    {
                        Certificate.Dispose();
                        Certificate = null;
                        _logger.LogWarning(
                            "Failed to get certificate with private key using workload API.");
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Using server certificate with private key from workload API...");
                    }
                }
                catch (NotSupportedException nse)
                {
                    _logger.LogWarning("Not supported: {Message}. " +
                        "Unable to use workload API to obtain the certificate!", nse.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create certificate using workload API.");
                }
            }
            if (Certificate == null)
            {
                using var ecdsa = ECDsa.Create();
                var req = new CertificateRequest("DC=" + dnsName, ecdsa, HashAlgorithmName.SHA256);
                var san = new SubjectAlternativeNameBuilder();
                san.AddDnsName(dnsName);
                var altDns = _identity?.ModuleId ?? _identity?.DeviceId;
                if (!string.IsNullOrEmpty(altDns) &&
                    !string.Equals(altDns, dnsName, StringComparison.OrdinalIgnoreCase))
                {
                    san.AddDnsName(altDns);
                }
                req.CertificateExtensions.Add(san.Build());
                Certificate = req.CreateSelfSigned(DateTimeOffset.Now, expiration);
                Debug.Assert(Certificate.HasPrivateKey);
                _logger.LogInformation("Created self-signed ECC server certificate...");
            }

            Debug.Assert(_stores.Count > 0);
            Debug.Assert(ApiKey != null);
            apiKeyStore ??= _stores[0];

            var pfxCertificate = Certificate.Export(X509ContentType.Pfx, ApiKey);
            apiKeyStore.State.AddOrUpdate(OpcUa.Constants.TwinPropertyCertificateKey, pfxCertificate);

            var renewalDuration = Certificate.NotAfter - nowOffset.Date - TimeSpan.FromDays(1);
            _renewalTimer.Change(renewalDuration, Timeout.InfiniteTimeSpan);

            _logger.LogInformation(
                "Stored new Certificate in {Store} store (and scheduled renewal after {Duration}).",
                apiKeyStore.Name, renewalDuration);
            _certificateRenewals++;
        }

        /// <summary>
        /// Renew certificate
        /// </summary>
        /// <param name="state"></param>
        private async void OnRenewExpiredCertificateAsync(object? state)
        {
            try
            {
                await UpdateApiKeyAndCertificateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Retry
                _logger.LogCritical(ex, "Failed to renew certificate - retrying in 1 hour...");
                _renewalTimer.Change(TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Send runtime state events
        /// </summary>
        /// <param name="runtimeStateEvent"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendRuntimeStateEvent(RuntimeStateEventModel runtimeStateEvent,
            CancellationToken ct)
        {
            await Task.WhenAll(_events.Select(SendEventAsync)).ConfigureAwait(false);

            async Task SendEventAsync(IEventClient events)
            {
                try
                {
                    await events.SendEventAsync(new TopicBuilder(_options.Value).EventsTopic,
                        _serializer.SerializeToMemory(runtimeStateEvent), _serializer.MimeType,
                        Encoding.UTF8.WebName, configure: eventMessage =>
                        {
                            eventMessage
                                .SetRetain(true)
                                .AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                    MessageSchemaTypes.RuntimeStateMessage);
                            if (_options.Value.RuntimeStateRoutingInfo != null)
                            {
                                eventMessage.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                    _options.Value.RuntimeStateRoutingInfo);
                            }
                        }, ct).ConfigureAwait(false);

                    _logger.LogInformation("{Event} sent via {Transport}.", runtimeStateEvent,
                        events.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed sending {MessageType} runtime state event through {Transport}.",
                        runtimeStateEvent.MessageType, events.Name);
                }
            }
        }

        /// <summary>
        /// ChannelDiagnostics timer to dump out all diagnostics
        /// </summary>
        /// <param name="ct"></param>
        private async Task DiagnosticsOutputTimerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _diagnosticsOutputTimer.WaitForNextTickAsync(ct).ConfigureAwait(false);
                    var diagnostics = _collector.EnumerateDiagnostics();

                    switch (_diagnostics)
                    {
                        case PublisherDiagnosticTargetType.Events:
                            await SendDiagnosticsAsync(diagnostics, ct).ConfigureAwait(false);
                            break;
                        // TODO: case PublisherDiagnosticTargetType.PubSub:
                        // TODO:     break;
                        default:
                            WriteDiagnosticsToConsole(diagnostics, _options.Value.DisableResourceMonitoring != true);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during diagnostics processing.");
                }
            }
        }

        /// <summary>
        /// Send diagnostics
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask SendDiagnosticsAsync(
            IEnumerable<(string, WriterGroupDiagnosticModel)> diagnostics, CancellationToken ct)
        {
            foreach (var (writerGroupId, info) in diagnostics)
            {
                var diagnosticsTopic = _topicCache.GetOrAdd(writerGroupId,
                    id => new TopicBuilder(_options.Value, variables: new Dictionary<string, string>
                    {
                        [PublisherConfig.DataSetWriterGroupVariableName] =
                            id ?? Constants.DefaultWriterGroupName,
                        [PublisherConfig.WriterGroupVariableName] =
                            id ?? Constants.DefaultWriterGroupName
                        // ...
                    }).DiagnosticsTopic);

                await Task.WhenAll(_events.Select(SendEventAsync)).ConfigureAwait(false);

                async Task SendEventAsync(IEventClient events)
                {
                    try
                    {
                        await events.SendEventAsync(diagnosticsTopic,
                            _serializer.SerializeToMemory(info), _serializer.MimeType,
                            Encoding.UTF8.WebName, configure: eventMessage =>
                            {
                                eventMessage
                                    .SetRetain(true)
                                    .SetTtl(_diagnosticInterval + TimeSpan.FromSeconds(10))
                                    .AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                        MessageSchemaTypes.WriterGroupDiagnosticsMessage);
                                if (_options.Value.RuntimeStateRoutingInfo != null)
                                {
                                    eventMessage.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                        _options.Value.RuntimeStateRoutingInfo);
                                }
                            }, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed sending Diagnostics event through {Transport}.", events.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Format diagnostics to console
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="includeResourceInfo"></param>
        private static void WriteDiagnosticsToConsole(
            IEnumerable<(string, WriterGroupDiagnosticModel)> diagnostics, bool includeResourceInfo)
        {
            var builder = new StringBuilder();
            foreach (var (writerGroupId, info) in diagnostics)
            {
                builder = Append(builder, writerGroupId, info, includeResourceInfo);
            }

            if (builder.Length > 0)
            {
                Console.Out.WriteLine(builder.ToString());
            }

            static StringBuilder Append(StringBuilder builder, string writerGroupId,
                WriterGroupDiagnosticModel info, bool includeResourceInfo)
            {
                var s = info.IngestionDuration.TotalSeconds == 0 ? 1 : info.IngestionDuration.TotalSeconds;
                var min = info.IngestionDuration.TotalMinutes == 0 ? 1 : info.IngestionDuration.TotalMinutes;

                var eventsPerSec = info.IngressEvents / s;
                var eventNotificationsPerSec = info.IngressEventNotifications / s;

                var sentMessagesPerSecFormatted = info.OutgressIoTMessageCount > 0 ? $"({info.SentMessagesPerSec:n2}/s)"
                    : string.Empty;
                var keepAliveChangesPerSecFormatted = info.IngressKeepAliveNotifications > 0 ?
                        $"(All time ~{info.IngressKeepAliveNotifications / min:n2}/min)"
                    : string.Empty;

                var dataChangesPerSecFormatted =
                    Format(info.IngressDataChanges, info.IngressDataChangesInLastMinute, s);
                var valueChangesPerSecFormatted =
                    Format(info.IngressValueChanges, info.IngressValueChangesInLastMinute, s);
                var eventsPerSecFormatted =
                    Format(info.IngressEvents, info.IngressEventsInLastMinute, s);
                var eventNotificationsPerSecFormatted =
                    Format(info.IngressEventNotifications, info.IngressEventNotificationsInLastMinute, s);
                var heartbeatsPerSecFormatted =
                    Format(info.IngressHeartbeats, info.IngressHeartbeatsInLastMinute, s);
                var cyclicReadsPerSecFormatted =
                    Format(info.IngressCyclicReads, info.IngressCyclicReadsInLastMinute, s);
                var sampledValuesPerSecFormatted =
                    Format(info.IngressSampledValues, info.IngressSampledValuesInLastMinute, s);
                var modelChangesPerSecFormatted =
                    Format(info.IngressModelChanges, info.IngressModelChangesInLastMinute, s);
                var serverQueueOverflowsPerSecFormatted =
                    Format(info.ServerQueueOverflows, info.ServerQueueOverflowsInLastMinute, s);

                static string Format(long changes, long lastMinute, double s)
                {
                    var dataChangesPerSecLastMin = lastMinute / Math.Min(s, 60d);
                    return changes > 0 ?
                        $"(All time ~{changes / s:n2}/s; {lastMinute:n0} in last 60s ~{dataChangesPerSecLastMin:n2}/s)"
                            : string.Empty;
                }

                var chunkUsageFormatted = Math.Round(info.EncoderAvgIoTChunkUsage, 2) > 0 ?
                    $"(Avg Chunk (4 KB) usage {info.EncoderAvgIoTChunkUsage:n2}; {info.EstimatedIoTChunksPerDay:n1}/day estimated)"
                        : string.Empty;
                var connectivityState = info.NumberOfConnectedEndpoints > 0 ? (info.NumberOfDisconnectedEndpoints > 0 ?
                    "(Partially Connected)" : "(Connected)") : "(Disconnected)";

                var sb = builder.AppendLine()
                    .Append("  DIAGNOSTICS INFORMATION for          : ")
                        .Append(info.WriterGroupName ?? Constants.DefaultWriterGroupName)
                        .Append(" (")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", writerGroupId)
                        .AppendLine(")")
                    .Append("  # OPC Publisher Version (Runtime)    : ")
                        .AppendLine(info.PublisherVersion)
                        ;
                if (includeResourceInfo)
                {
                    sb = sb
                    .Append("  # Cpu/Memory max                     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n2}", info.MaximumCpuUnits)
                        .Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.MaximumMemoryInBytes / 1000d)
                        .AppendLine(" KB")
                    .Append("  # Cpu/Memory available               : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n2}", info.GuaranteedCpuUnits)
                        .Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.GuaranteedMemoryInBytes / 1000d)
                        .AppendLine(" KB")
                    .Append("  # Cpu/Memory % used (window/total)   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:p2}", info.CpuUsedPercentage)
                        .Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:p2}", info.MemoryUsedPercentage)
                        .Append(" (")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.MemoryUsedInBytes / 1000d)
                        .AppendLine(" kb)")
                        ;
                }
                return sb
                    .Append("  # Ingest duration (dd:hh:mm:ss)/Time : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:dd\\:hh\\:mm\\:ss}", info.IngestionDuration)
                        .Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:O}", info.Timestamp)
                        .AppendLine()
                    .Append("  # Endpoints connected/disconnected   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.NumberOfConnectedEndpoints).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", info.NumberOfDisconnectedEndpoints).Append(' ')
                        .AppendLine(connectivityState)
                    .Append("  # Connection retries                 : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.ConnectionRetries)
                        .AppendLine()
                    .Append("  # Subscriptions count                : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.NumberOfSubscriptions)
                        .AppendLine()
                    .Append("  # Good/Bad Monitored Items (Late)    : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.MonitoredOpcNodesSucceededCount).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.MonitoredOpcNodesFailedCount).Append(" (")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.MonitoredOpcNodesLateCount)
                        .AppendLine(")")
                    .Append("  # Queued/Minimum request count       : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.PublishRequestsRatio).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0.##}", info.MinPublishRequestsRatio)
                        .AppendLine()
                    .Append("  # Good/Bad Publish request count     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.GoodPublishRequestsRatio).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0.##}", info.BadPublishRequestsRatio)
                        .AppendLine()
                    .Append("  # Heartbeats/Condition items active  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.ActiveHeartbeatCount).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.ActiveConditionCount)
                        .AppendLine()
                    .Append("  # Ingress value changes              : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressValueChanges).Append(' ')
                        .AppendLine(valueChangesPerSecFormatted)
                    .Append("  # Ingress sampled values             : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressSampledValues).Append(' ')
                        .AppendLine(sampledValuesPerSecFormatted)
                    .Append("  # Ingress events                     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressEvents).Append(' ')
                        .AppendLine(eventsPerSecFormatted)
                    .Append("  # Ingress values/events unassignable : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressUnassignedChanges)
                        .AppendLine()
                    .Append("  # Server queue overflows             : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.ServerQueueOverflows).Append(' ')
                        .AppendLine(serverQueueOverflowsPerSecFormatted)
                    .Append("  # Received Data Change Notifications : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressDataChanges).Append(' ')
                        .AppendLine(dataChangesPerSecFormatted)
                    .Append("  # Received Event Notifications       : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressEventNotifications).Append(' ')
                        .AppendLine(eventNotificationsPerSecFormatted)
                    .Append("  # Received Keep Alive Notifications  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressKeepAliveNotifications).Append(' ')
                        .AppendLine(keepAliveChangesPerSecFormatted)
                    .Append("  # Received Cyclic read Notifications : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressCyclicReads).Append(' ')
                        .AppendLine(cyclicReadsPerSecFormatted)
                    .Append("  # Generated Heartbeat Notifications  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressHeartbeats).Append(' ')
                        .AppendLine(heartbeatsPerSecFormatted)
                    .Append("  # Generated Model Changes            : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressModelChanges).Append(' ')
                        .AppendLine(modelChangesPerSecFormatted)
                    .Append("  # Publish queue partitions/active    : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.TotalPublishQueuePartitions).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.ActivePublishQueuePartitions)
                        .AppendLine()
                    .Append("  # Notifications buffered/dropped     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressBatchBlockBufferSize).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.IngressNotificationsDropped)
                        .AppendLine()
                    .Append("  # Encoder input buffer size          : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncodingBlockInputSize)
                        .AppendLine()
                    .Append("  # Encoder Notif. processed/dropped   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderNotificationsProcessed).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.EncoderNotificationsDropped)
                        .AppendLine()
                    .Append("  # Encoder Network Messages produced  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderIoTMessagesProcessed)
                        .AppendLine()
                    .Append("  # Encoder avg Notifications/Message  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderAvgNotificationsMessage)
                        .AppendLine()
                    .Append("  # Encoder worst Message split ratio  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.EncoderMaxMessageSplitRatio)
                        .AppendLine()
                    .Append("  # Encoder avg Message body size      : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.EncoderAvgIoTMessageBodySize).Append(' ')
                        .AppendLine(chunkUsageFormatted)
                    .Append("  # Encoder output buffer size         : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncodingBlockOutputSize)
                        .AppendLine()
                    .Append("  # Egress Messages queued/dropped     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressInputBufferCount).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:n0}", info.OutgressInputBufferDropped)
                        .AppendLine()
                    .Append("  # Egress Message send failures       : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressIoTMessageFailedCount)
                        .AppendLine()
                    .Append("  # Egress Messages successfully sent  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressIoTMessageCount)
                        .Append(' ')
                        .AppendLine(sentMessagesPerSecFormatted)
                    ;
            }
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableGauge("iiot_edge_publisher_module_start",
                () => new Measurement<int>(_runtimeState == RuntimeStateEventType.RestartAnnouncement ? 0 : 1,
                _metrics.TagList), description: "Publisher module started.");
            _meter.CreateObservableGauge("iiot_edge_publisher_module_state",
                () => new Measurement<int>((int)_runtimeState,
                _metrics.TagList), description: "Publisher module runtime state.");
            _meter.CreateObservableCounter("iiot_edge_publisher_certificate_renewal_count",
                () => new Measurement<int>(_certificateRenewals,
                _metrics.TagList), description: "Publisher certificate renewals.");
        }

        private const int kCertificateLifetimeDays = 30;
        private readonly ConcurrentDictionary<string, string> _topicCache;
        private readonly ILogger _logger;
        private readonly IIoTEdgeDeviceIdentity? _identity;
        private readonly IDiagnosticCollector _collector;
        private readonly IIoTEdgeWorkloadApi? _workload;
        private readonly ITimer _renewalTimer;
        private readonly TimeProvider _timeProvider;
        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly List<IEventClient> _events;
        private readonly List<IKeyValueStore> _stores;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly IMetricsContext _metrics;
        private readonly CancellationTokenSource _cts;
        private readonly PeriodicTimer _diagnosticsOutputTimer;
        private readonly TimeSpan _diagnosticInterval;
        private readonly PublisherDiagnosticTargetType _diagnostics;
        private RuntimeStateEventType _runtimeState;
        private Task _publisher;
        private int _certificateRenewals;
    }
}
