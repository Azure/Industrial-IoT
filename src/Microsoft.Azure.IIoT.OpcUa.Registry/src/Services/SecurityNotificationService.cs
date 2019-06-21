// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Linq;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Sending security notifications for unsecure endpoints 
    /// </summary>
    public sealed class SecurityNotificationService : IEndpointListener {

        /// <summary>
        /// create security notification service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public SecurityNotificationService(IIoTHubTelemetryServices client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task OnEndpointAddedAsync(EndpointRegistrationModel model) {
            Console.WriteLine($"Inside registry service.. endpointId = {model.Id}");
            var mode = model.Endpoint.SecurityMode ?? SecurityMode.None;
            var policy = model.Endpoint.SecurityPolicy ?? "None";

            var certEncoded = model.Certificate;
            var cert = new X509Certificate2(certEncoded);

            if (IsCertificateSelfSignedOrExpired(cert) | IsSecurityModeOrPolicyNone(mode, policy)) {
                if (IsCertificateSelfSignedOrExpired(cert)) {
                    var message = "Unsecured endpoint with self-signed or expired certificate found.";
                    await SendMessage(model, message);
                }

                if (IsSecurityModeOrPolicyNone(mode, policy)) {
                    var message = "Unsecured endpoint with security policy or security mode none.";
                    await SendMessage(model, message);
                }
            }
            else {
                _logger.Information("Endpoint is secured");
            }
        }

        private async Task SendMessage(EndpointRegistrationModel model, string message) {
            var settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            };
            var securityInfoModel = new EndpointSecurityInfoModel {
                Events = new List<SecurityEventModel> {
                        new SecurityEventModel {
                            Payload = new List<SecurityEventPayloadModel> {
                                new SecurityEventPayloadModel {
                                    Message = message,
                                    ExtraDetails = FlattenToDict(JsonConvert.SerializeObject(model, settings))
                                }
                            }
                        }
                    }
            };

            await SendSecurityMessage(securityInfoModel, model.SupervisorId);
            _logger.Information("{0} > Security message sent", DateTime.Now);
        }

        private static bool IsCertificateSelfSignedOrExpired(X509Certificate2 cert) {
            return cert.SubjectName.RawData.SequenceEqual(cert.IssuerName.RawData) | cert.NotAfter < DateTime.Now | cert.NotBefore > DateTime.Now;
        }
        private static bool IsSecurityModeOrPolicyNone(SecurityMode mode, string policy) {
            return mode.Equals(SecurityMode.None) || policy.Contains("None");
        }

        private Dictionary<string, string> FlattenToDict(string jsonString) {
            var jsonObject = JObject.Parse(jsonString);
            IEnumerable<JToken> jTokens = jsonObject.Descendants().Where(p => p.Count() == 0);
            Dictionary<string, string> results = jTokens.Aggregate(new Dictionary<string, string>(), (properties, jToken) => {
                properties.Add(jToken.Path, jToken.ToString());
                return properties;
            });
            return results;
        }

        private async Task SendSecurityMessage(EndpointSecurityInfoModel sim, string supervisorId) {
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            await _client.SendAsync(deviceId,
                new EventModel {
                    Properties = new Dictionary<string, string> {
                        ["iothub-interface-id"] = "http://security.azureiot.com/SecurityAgent/1.0.0"
                    },
                    Payload = JToken.FromObject(sim)
                });
        }

        private readonly IIoTHubTelemetryServices _client;
        private readonly ILogger _logger;
    }
}
