// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Security.Services {
    using Microsoft.Azure.IIoT.OpcUa.Security.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Sending security notifications for unsecure endpoints
    /// </summary>
    public sealed class EndpointSecurityAlerter : IEndpointRegistryListener,
        IApplicationRegistryListener {

        /// <summary>
        /// create security notification service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="metrics"></param>
        /// <param name="logger"></param>
        public EndpointSecurityAlerter(IIoTHubTelemetryServices client,
            IMetricsLogger metrics, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        /// <inheritdoc/>
        public Task OnEndpointActivatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointDeactivatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointDisabledAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointEnabledAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointUpdatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint, bool isPatch) {
            return isPatch ? Task.CompletedTask : CheckEndpointInfoAsync(endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointDeletedAsync(RegistryOperationContextModel context,
            string endpointId, EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointNewAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return CheckEndpointInfoAsync(endpoint);
        }

        /// <inheritdoc/>
        public Task OnApplicationNewAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return CheckApplicationInfoAsync(application);
        }

        /// <inheritdoc/>
        public Task OnApplicationUpdatedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application, bool isPatch) {
            return isPatch ? Task.CompletedTask : CheckApplicationInfoAsync(application);
        }

        /// <inheritdoc/>
        public Task OnApplicationEnabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationDisabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationDeletedAsync(RegistryOperationContextModel context,
            string applicationId, ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Check endpoint info
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private async Task CheckEndpointInfoAsync(EndpointInfoModel endpoint) {
            using (_metrics.TrackDuration(nameof(CheckEndpointInfoAsync))) {
                var mode = endpoint.Registration.Endpoint.SecurityMode ?? SecurityMode.None;
                var policy = endpoint.Registration.Endpoint.SecurityPolicy ?? "None";

                var unsecure = mode.Equals(SecurityMode.None) || policy.Contains("None");
                if (unsecure) {
                    await SendEndpointAlertAsync(endpoint, "Unsecured endpoint found.",
                        $"SecurityMode: {mode}, SecurityProfile: {policy}");
                    _metrics.TrackEvent("endpointSecurityPolicyNone");
                }

                // TODO Retrieve certificate from edge.
#if FALSE
                // Test endpoint certificate
                var certEncoded = endpoint.Registration.Endpoint.Certificate;
                if (certEncoded == null && !unsecure) {
                    await SendEndpointAlertAsync(endpoint,
                        "Secure endpoint without certificate found.", "No Certificate");
                    _metrics.TrackEvent("endpointWithoutCertificate");
                }
                else {
                    using (var cert = new X509Certificate2(certEncoded)) {
                        if (cert.SubjectName.RawData.SequenceEqual(cert.IssuerName.RawData)) {
                            await SendEndpointAlertAsync(endpoint,
                                "Endpoint with self-signed certificate found.",
                                $"Certificate is self signed by {cert.SubjectName.Name}");
                        }
                        else if (cert.NotAfter < DateTime.UtcNow) {
                            await SendEndpointAlertAsync(endpoint,
                                "Endpoint has expired certificate.",
                                $"Certificate expired {cert.NotAfter}");
                        }
                        else if (cert.NotBefore > DateTime.UtcNow) {
                            await SendEndpointAlertAsync(endpoint,
                                "Endpoint has certificate that is not yet valid.",
                                $"Certificate is valid from {cert.NotBefore}");
                        }
                        else {
                            _logger.Verbose("Application certificate is valid.");
                        }
                    }
                }
#endif
            }
        }

        /// <summary>
        /// Check application info
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task CheckApplicationInfoAsync(ApplicationInfoModel application) {
            using (_metrics.TrackDuration(nameof(CheckApplicationInfoAsync))) {
                // TODO Retrieve certificate from edge.
#if FALSE
                // Test application certificate
                var certEncoded = application.Certificate;
                if (certEncoded == null) {
                    await SendApplicationAlertAsync(application,
                        "Application without certificate found.", "No Certificate");
                }
                else {
                    using (var cert = new X509Certificate2(certEncoded)) {
                        if (cert.SubjectName.RawData.SequenceEqual(cert.IssuerName.RawData)) {
                            await SendApplicationAlertAsync(application,
                                "Application with self-signed certificate found.",
                                $"Certificate is self signed by {cert.SubjectName.Name}");
                        }
                        else if (cert.NotAfter < DateTime.UtcNow) {
                            await SendApplicationAlertAsync(application,
                                "Application has expired certificate.",
                                $"Certificate expired {cert.NotAfter}");
                        }
                        else if (cert.NotBefore > DateTime.UtcNow) {
                            await SendApplicationAlertAsync(application,
                                "Application has certificate that is not yet valid.",
                                $"Certificate is valid from {cert.NotBefore}");
                        }
                        else {
                            _logger.Verbose("Application certificate is valid.");
                        }
                    }
                }
#else
                await Task.CompletedTask;
#endif
            }
        }

        /// <summary>
        /// Send endpoint alert
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="message"></param>
        /// <param name="usedConfiguration"></param>
        /// <returns></returns>
        private Task SendEndpointAlertAsync(EndpointInfoModel endpoint,
            string message, string usedConfiguration) {
#if USE_DISCOVERER_IDENTITY
            var deviceId = DiscovererModelEx.ParseDeviceId(
                endpoint.Registration.DiscovererId, out var moduleId);
#else
            var deviceId = endpoint.Registration.Id;
            var moduleId = (string)null;
#endif
            return SendAlertAsync(deviceId, moduleId, message, usedConfiguration,
                FlattenToDict(endpoint.Registration));
        }

        /// <summary>
        /// Send application alert
        /// </summary>
        /// <param name="application"></param>
        /// <param name="message"></param>
        /// <param name="usedConfiguration"></param>
        /// <returns></returns>
        private Task SendApplicationAlertAsync(ApplicationInfoModel application,
            string message, string usedConfiguration) {
#if !USE_APPLICATION_IDENTITY
            var deviceId = DiscovererModelEx.ParseDeviceId(
                application.DiscovererId, out var moduleId);
#else
            var deviceId = application.ApplicationId;
            var moduleId = (string)null;
#endif
            return SendAlertAsync(deviceId, moduleId, message, usedConfiguration,
                FlattenToDict(application));
        }

        /// <summary>
        /// Send alert
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="message"></param>
        /// <param name="usedConfiguration"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private async Task SendAlertAsync(string deviceId, string moduleId,
            string message, string usedConfiguration, IDictionary<string, string> properties) {
            var alert = new SecurityAgentMessageModel {
                AgentVersion  = "0.0.1",
                AgentId= "e00dc5f5-feac-4c3e-87e2-93c16f010c00",
                MessageSchemaVersion = "1.0",
                Events = new List<SecurityEventModel> {
                    new SecurityEventModel {
                        EventType = "Operational",
                        Category = "Triggered",
                        Name = "ConfigurationError",
                        IsEmpty = false,
                        PayloadSchemaVersion = "1.0",
                        Id = "1111db0b-44fe-42e9-9cff-bbb2d8fd0000",
                        TimestampLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ssZ"),
                        TimestampUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ"),
                        Payload = new List<SecurityEventPayloadModel> {
                            new SecurityEventPayloadModel {
                                ConfigurationName = "EndpointSecurity",
                                ErrorType = "NotOptimal",
                                UsedConfiguration = usedConfiguration,
                                Message = message,
                                ExtraDetails = properties
                            }
                        }
                    }
                }
            };
            // Send endpoint information as if it was coming from the endpoint
            await _client.SendAsync(deviceId, moduleId,
                new EventModel {
                    Properties = new Dictionary<string, string> {
                        [SystemProperties.InterfaceId] =
                            "http://security.azureiot.com/SecurityAgent/1.0.0"
                    },
                    Payload = JToken.FromObject(alert)
                });
            _logger.Verbose("Security Agent Message {@alert} sent.", alert);
        }

        /// <summary>
        /// Flatten properties
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private Dictionary<string, string> FlattenToDict<T>(T model) {
            var settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            };
            var jsonObject = JObject.FromObject(model, JsonSerializer.Create(settings));
            var jTokens = jsonObject.Descendants().Where(p => p.Count() == 0);
            var results = jTokens.Aggregate(new Dictionary<string, string>(),
                (properties, jToken) => {
                    properties.Add(jToken.Path, jToken.ToString());
                    return properties;
                });
            return results;
        }

        private readonly IIoTHubTelemetryServices _client;
        private readonly ILogger _logger;
        private readonly IMetricsLogger _metrics;
    }
}
