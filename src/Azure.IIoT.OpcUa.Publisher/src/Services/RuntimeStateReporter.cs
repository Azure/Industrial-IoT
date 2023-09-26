// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Storage;
    using Furly.Azure.IoT.Edge.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using System.Net;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public class RuntimeStateReporter : IRuntimeStateReporter, IApiKeyProvider,
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
        /// <param name="workload"></param>
        public RuntimeStateReporter(IEnumerable<IEventClient> events,
            IJsonSerializer serializer, IEnumerable<IKeyValueStore> stores,
            IOptions<PublisherOptions> options, ILogger<RuntimeStateReporter> logger,
            IoTEdgeWorkloadApi? workload = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workload = workload;

            ArgumentNullException.ThrowIfNull(stores);
            ArgumentNullException.ThrowIfNull(events);

            _events = events.Reverse().ToList();
            _stores = stores.Reverse().ToList();
            if (_stores.Count == 0)
            {
                throw new ArgumentException("No key value stores configured.",
                    nameof(stores));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Certificate?.Dispose();
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

            if (_options.Value.EnableRuntimeStateReporting ?? false)
            {
                var body = new RuntimeStateEventModel
                {
                    MessageType = RuntimeStateEventType.RestartAnnouncement,
                    MessageVersion = 1
                };

                await SendRuntimeStateEvent(body, ct).ConfigureAwait(false);
                _logger.LogInformation("Restart announcement sent successfully.");
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
                    Certificate = new X509Certificate2((byte[])cert!, ApiKey);
                    if (Certificate.NotAfter >= DateTime.UtcNow)
                    {
                        _logger.LogInformation("Using valid Certificate found in {Store} store...",
                            apiKeyStore.Name);
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

            Debug.Assert(_stores.Count > 0);
            Debug.Assert(ApiKey != null);
            apiKeyStore ??= _stores[0];
            _logger.LogInformation("Generating new Certificate in {Store} store...",
                apiKeyStore.Name);
            var pfxCertificate = await UpdateCertificateAsync(ApiKey).ConfigureAwait(false);
            apiKeyStore.State.Add(OpcUa.Constants.TwinPropertyCertificateKey, pfxCertificate);

            async Task<byte[]> UpdateCertificateAsync(string password)
            {
                var dnsName = Dns.GetHostName();
                if (_workload != null)
                {
                    try
                    {
                        var expiration = DateTime.UtcNow + TimeSpan.FromDays(30);
                        var certificates = await _workload.CreateServerCertificateAsync(
                            dnsName, expiration, default).ConfigureAwait(false);

                        Certificate = certificates[0];
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create certificate using workload API.");
                    }
                }
                if (Certificate == null)
                {
                    var ecdsa = ECDsa.Create();
                    var req = new CertificateRequest("DC=" + dnsName, ecdsa, HashAlgorithmName.SHA256);
                    Certificate = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(30));
                }
                return Certificate.Export(X509ContentType.Cert, password);
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

        private readonly ILogger _logger;
        private readonly IoTEdgeWorkloadApi? _workload;
        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly List<IEventClient> _events;
        private readonly List<IKeyValueStore> _stores;
    }
}
