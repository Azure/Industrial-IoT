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
            if (mode.Equals(SecurityMode.None) || policy.Contains("None")) {
                var settings = new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore
                };
                var securityInfoModel = new EndpointSecurityInfoModel {
                    Events = new List<SecurityEventModel> {
                        new SecurityEventModel {
                            Payload = new List<SecurityEventPayloadModel> {
                                new SecurityEventPayloadModel {
                                    Message = "Unsecured endpoint found.",
                                    ExtraDetails = FlattenToDict(JsonConvert.SerializeObject(model, settings))
                                }
                            }
                        }
                    }
                };

                await SendSecurityMessage(securityInfoModel, model.SupervisorId);
                Console.WriteLine("{0} > Security message sent", DateTime.Now);
            }
            else {
                Console.WriteLine("Endpoint is secured");
            }
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
