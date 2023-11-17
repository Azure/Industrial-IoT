// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics.Metrics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public sealed class RuntimeStateReporter : IRuntimeStateReporter, IApiKeyProvider,
        ISslCertProvider, IDisposable
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
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <param name="workload"></param>
        public RuntimeStateReporter(IEnumerable<IEventClient> events,
            IJsonSerializer serializer, IEnumerable<IKeyValueStore> stores,
            IOptions<PublisherOptions> options, ILogger<RuntimeStateReporter> logger,
            IMetricsContext? metrics = null, IoTEdgeWorkloadApi? workload = null)
        {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _metrics = metrics ?? IMetricsContext.Empty;
            _workload = workload;
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

            InitializeMetrics();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _runtimeState = RuntimeStateEventType.Stopped;
            Certificate?.Dispose();
            _renewalTimer.Dispose();
            _meter.Dispose();
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncementAsync(CancellationToken ct)
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
            }

            await UpdateApiKeyAndCertificateAsync().ConfigureAwait(false);

            _runtimeState = RuntimeStateEventType.RestartAnnouncement;

            if (_options.Value.EnableRuntimeStateReporting ?? false)
            {
                var body = new RuntimeStateEventModel
                {
                    Timestamp = DateTime.UtcNow,
                    MessageVersion = 1,
                    MessageType = _runtimeState
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

            // The certificate must be in the same store as the api key or else we generate a new one.
            if (apiKeyStore != null &&
                apiKeyStore.State.TryGetValue(OpcUa.Constants.TwinPropertyCertificateKey,
                    out var cert) && cert.IsBytes)
            {
                try
                {
                    // Load certificate
                    Certificate?.Dispose();
                    Certificate = new X509Certificate2((byte[])cert!, ApiKey);
                    var now = DateTime.UtcNow.AddDays(1);
                    if (now < Certificate.NotAfter && Certificate.HasPrivateKey)
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
            var dnsName = Dns.GetHostName();

            Certificate?.Dispose();
            Certificate = null;
            if (_workload != null)
            {
                try
                {
                    var certificates = await _workload.CreateServerCertificateAsync(
                        dnsName, expiration.Date, default).ConfigureAwait(false);

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
                        _logger.LogInformation(
                            "Failed to get certificate with private key using workload API.");
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Using server certificate with private key from workload API.");
                    }
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
                Certificate = req.CreateSelfSigned(DateTimeOffset.Now, expiration);

                Debug.Assert(Certificate.HasPrivateKey);
                _logger.LogDebug("Created self-signed ECC server certificate.");
            }

            Debug.Assert(_stores.Count > 0);
            Debug.Assert(ApiKey != null);
            apiKeyStore ??= _stores[0];

            var pfxCertificate = Certificate.Export(X509ContentType.Pfx, ApiKey);
            apiKeyStore.State.AddOrUpdate(OpcUa.Constants.TwinPropertyCertificateKey, pfxCertificate);

            var renewalDuration = Certificate.NotAfter - nowOffset.Date - TimeSpan.FromDays(1);
            _renewalTimer.Change(renewalDuration, Timeout.InfiniteTimeSpan);

            _logger.LogInformation(
                "Created new Certificate in {Store} store (renewal in {Duration})...",
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
                        Encoding.UTF8.WebName, configure: e =>
                        {
                            e.AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                MessageSchemaTypes.RuntimeStateMessage);
                            if (_options.Value.RuntimeStateRoutingInfo != null)
                            {
                                e.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                    _options.Value.RuntimeStateRoutingInfo);
                            }
                        }, ct).ConfigureAwait(false);
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
        private readonly ILogger _logger;
        private readonly IoTEdgeWorkloadApi? _workload;
        private readonly Timer _renewalTimer;
        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly List<IEventClient> _events;
        private readonly List<IKeyValueStore> _stores;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly IMetricsContext _metrics;
        private RuntimeStateEventType _runtimeState;
        private int _certificateRenewals;
    }
}
