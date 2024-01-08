﻿// ------------------------------------------------------------
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
        /// <param name="identity"></param>
        /// <param name="workload"></param>
        public RuntimeStateReporter(IEnumerable<IEventClient> events,
            IJsonSerializer serializer, IEnumerable<IKeyValueStore> stores,
            IOptions<PublisherOptions> options, IDiagnosticCollector collector,
            ILogger<RuntimeStateReporter> logger, IMetricsContext? metrics = null,
            IIoTEdgeDeviceIdentity? identity = null, IIoTEdgeWorkloadApi? workload = null)
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
            _workload = workload;
            _identity = identity;
            _renewalTimer = new Timer(OnRenewExpiredCertificateAsync);

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
            _diagnostics = options.Value.DiagnosticsTarget ?? PublisherDiagnosticTargetType.Logger;
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
            // Set runtime state in state stores
            foreach (var store in _stores)
            {
                store.State[OpcUa.Constants.TwinPropertySiteKey] =
                    _options.Value.Site;
                store.State[OpcUa.Constants.TwinPropertyTypeKey] =
                    OpcUa.Constants.EntityTypePublisher;
                store.State[OpcUa.Constants.TwinPropertyVersionKey] =
                    GetType().Assembly.GetReleaseVersion().ToString();
                store.State[OpcUa.Constants.TwinPropertyFullVersionKey] =
                    PublisherConfig.Version;
            }

            await UpdateApiKeyAndCertificateAsync().ConfigureAwait(false);

            if (_options.Value.EnableRuntimeStateReporting ?? false)
            {
                var body = new RuntimeStateEventModel
                {
                    TimestampUtc = DateTime.UtcNow,
                    MessageVersion = 1,
                    MessageType = RuntimeStateEventType.RestartAnnouncement,
                    PublisherId = _options.Value.PublisherId,
                    SemVer = GetType().Assembly.GetReleaseVersion().ToString(),
                    Version = PublisherConfig.Version,
                    Site = _options.Value.Site,
                    DeviceId = _identity?.DeviceId,
                    ModuleId = _identity?.ModuleId
                };

                await SendRuntimeStateEvent(body, ct).ConfigureAwait(false);
                _logger.LogInformation("Restart announcement sent successfully.");
            }

            _runtimeState = RuntimeStateEventType.Running;
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
            else
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
                    var now = DateTime.UtcNow.AddDays(1);
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
            var nowOffset = DateTimeOffset.UtcNow;
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
        /// Renew certifiate
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
                    await events.SendEventAsync(new TopicBuilder(_options).EventsTopic,
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
        /// Diagnostics timer to dump out all diagnostics
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
                            WriteDiagnosticsToConsole(diagnostics);
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
                    id => new TopicBuilder(_options, new Dictionary<string, string>
                    {
                        [PublisherConfig.DataSetWriterGroupVariableName] =
                            id ?? Constants.DefaultWriterGroupId
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
        private static void WriteDiagnosticsToConsole(IEnumerable<(string, WriterGroupDiagnosticModel)> diagnostics)
        {
            var builder = new StringBuilder();
            foreach (var (writerGroupId, info) in diagnostics)
            {
                builder = Append(builder, writerGroupId, info);
            }

            if (builder.Length > 0)
            {
                Console.Out.WriteLine(builder.ToString());
            }

            StringBuilder Append(StringBuilder builder, string writerGroupId,
                WriterGroupDiagnosticModel info)
            {
                var s = info.IngestionDuration.TotalSeconds == 0 ? 1 : info.IngestionDuration.TotalSeconds;
                var min = info.IngestionDuration.TotalMinutes == 0 ? 1 : info.IngestionDuration.TotalMinutes;

                var eventsPerSec = info.IngressEvents / s;
                var eventNotificationsPerSec = info.IngressEventNotifications / s;

                var dataChangesPerSecLastMin = info.IngressDataChangesInLastMinute / Math.Min(s, 60d);
                var dataChangesPerSecFormatted = info.IngressDataChanges > 0
    ? $"(All time ~{info.IngressDataChanges / s:0.##}/s; {info.IngressDataChangesInLastMinute} in last 60s ~{dataChangesPerSecLastMin:0.##}/s)"
                    : string.Empty;
                var valueChangesPerSecLastMin = info.IngressValueChangesInLastMinute / Math.Min(s, 60d);
                var valueChangesPerSecFormatted = info.IngressValueChanges > 0
    ? $"(All time ~{info.IngressValueChanges / s:0.##}/s; {info.IngressValueChangesInLastMinute} in last 60s ~{valueChangesPerSecLastMin:0.##}/s)"
                    : string.Empty;
                var sentMessagesPerSecFormatted = info.OutgressIoTMessageCount > 0
    ? $"({info.SentMessagesPerSec:0.##}/s)"
                    : string.Empty;
                var keepAliveChangesPerSecFormatted = info.IngressKeepAliveNotifications > 0
    ? $"(All time ~{info.IngressKeepAliveNotifications / min:0.##}/min)"
                    : string.Empty;
                var eventsPerSecFormatted = info.IngressEventNotifications > 0
    ? $"(All time ~{info.IngressEventNotifications / s:0.##}/s)"
                    : string.Empty;
                var eventNotificationsPerSecFormatted = info.IngressEventNotifications > 0
    ? $"(All time ~{info.IngressEventNotifications / s:0.##}/s)"
                    : string.Empty;
                var connectivityState = info.NumberOfConnectedEndpoints > 0 ? (info.NumberOfDisconnectedEndpoints > 0
                    ? "(Partially Connected)" : "(Connected)") : "(Disconnected)";

                return builder.AppendLine()
                    .Append("  DIAGNOSTICS INFORMATION for          : ")
                        .AppendLine(writerGroupId)
                    .Append("  # OPC Publisher Version (Runtime)    : ")
                        .AppendLine(info.PublisherVersion)
                    .Append("  # Time                               : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:O}", info.Timestamp)
                        .AppendLine()
                    .Append("  # Ingestion duration                 : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:dd\\:hh\\:mm\\:ss}", info.IngestionDuration)
                        .AppendLine(" (dd:hh:mm:ss)")
                    .Append("  # Endpoints connected/disconnected   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.NumberOfConnectedEndpoints).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", info.NumberOfDisconnectedEndpoints).Append(' ')
                        .AppendLine(connectivityState)
                    .Append("  # Connection retries                 : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.ConnectionRetries)
                        .AppendLine()
                    .Append("  # Monitored Opc nodes succeeded count: ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.MonitoredOpcNodesSucceededCount)
                        .AppendLine()
                    .Append("  # Monitored Opc nodes failed count   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.MonitoredOpcNodesFailedCount)
                        .AppendLine()
                    .Append("  # Subscriptions count                : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.NumberOfSubscriptions)
                        .AppendLine()
                    .Append("  # Queued/Minimum request count       : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.PublishRequestsRatio).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0.##}", info.MinPublishRequestsRatio)
                        .AppendLine()
                    .Append("  # Good/Bad Publish request count     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.GoodPublishRequestsRatio).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0.##}", info.BadPublishRequestsRatio)
                        .AppendLine()
                    .Append("  # Ingress value changes              : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressValueChanges).Append(' ')
                        .AppendLine(valueChangesPerSecFormatted)
                    .Append("  # Ingress events                     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressEvents).Append(' ')
                        .AppendLine(eventsPerSecFormatted)
                    .Append("  # Ingress values/events unassignable : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressUnassignedChanges)
                        .AppendLine()
                    .Append("  # Received Data Change Notifications : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressDataChanges).Append(' ')
                        .AppendLine(dataChangesPerSecFormatted)
                    .Append("  # Received Event Notifications       : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressEventNotifications).Append(' ')
                        .AppendLine(eventNotificationsPerSecFormatted)
                    .Append("  # Received Keep Alive Notifications  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressKeepAliveNotifications).Append(' ')
                        .AppendLine(keepAliveChangesPerSecFormatted)
                    .Append("  # Generated Cyclic read Notifications: ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressCyclicReads)
                        .AppendLine()
                    .Append("  # Generated Heartbeat Notifications  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressHeartbeats)
                        .AppendLine()
                    .Append("  # Notification batch buffer size     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.IngressBatchBlockBufferSize)
                        .AppendLine()
                    .Append("  # Encoder input/output buffer size   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.EncodingBlockInputSize).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", info.EncodingBlockOutputSize)
                        .AppendLine()
                    .Append("  # Encoder Notif. processed/dropped   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderNotificationsProcessed).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", info.EncoderNotificationsDropped)
                        .AppendLine()
                    .Append("  # Encoder Network Messages produced  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderIoTMessagesProcessed)
                        .AppendLine()
                    .Append("  # Encoder avg Notifications/Message  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.EncoderAvgNotificationsMessage)
                        .AppendLine()
                    .Append("  # Encoder worst Message split ratio  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.##}", info.EncoderMaxMessageSplitRatio)
                        .AppendLine()
                    .Append("  # Encoder avg Message body size      : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderAvgIoTMessageBodySize)
                        .AppendLine()
                    .Append("  # Encoder avg Chunk (4 KB) usage     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.#}", info.EncoderAvgIoTChunkUsage)
                        .AppendLine()
                    .Append("  # Estimated Chunks (4 KB) per day    : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EstimatedIoTChunksPerDay)
                        .AppendLine()
                    .Append("  # Egress Messages queued/dropped     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressInputBufferCount).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", info.OutgressInputBufferDropped)
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
                _metrics.TagList), "Count", "Publisher module started.");
            _meter.CreateObservableGauge("iiot_edge_publisher_module_state",
                () => new Measurement<int>((int)_runtimeState,
                _metrics.TagList), "State", "Publisher module runtime state.");
            _meter.CreateObservableCounter("iiot_edge_publisher_certificate_renewal_count",
                () => new Measurement<int>(_certificateRenewals,
                _metrics.TagList), "Count", "Publisher certificate renewals.");
        }

        private const int kCertificateLifetimeDays = 30;
        private readonly ConcurrentDictionary<string, string> _topicCache;
        private readonly ILogger _logger;
        private readonly IIoTEdgeDeviceIdentity? _identity;
        private readonly IDiagnosticCollector _collector;
        private readonly IIoTEdgeWorkloadApi? _workload;
        private readonly Timer _renewalTimer;
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
