// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services using the IoT Hub twin services for endpoint
    /// identity registration/retrieval.
    /// </summary>
    public class OpcUaTwinServices : IOpcUaEndpointServices {

        /// <summary>
        /// Create using iot hub twin registray service client
        /// </summary>
        /// <param name="registry"></param>
        public OpcUaTwinServices(IIoTHubTwinServices registry,
            IOpcUaValidationServices validator, ILogger logger) {

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Find twin and read endpoint from it
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ServerEndpointModel> GetAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var device = await _registry.GetAsync(id);
            if (device.Tags.ContainsKey(k_endpointTag)) {
                var tag = device.Tags[k_endpointTag].ToObject<OpcUaEndpointTag>();
                return tag.ToServiceModel();
            }
            return null;
        }

        /// <summary>
        /// List all endpoints
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public async Task<ServerRegistrationListModel> ListAsync(string continuation) {
            // Find all devices where endopint url is set
            var query = $"SELECT * FROM devices WHERE IS_OBJECT(tags.{k_endpointTag})";
            var devices = await _registry.QueryAsync(query, continuation);
            return new ServerRegistrationListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items.Select(TwinToRegistrationModel).ToList()
            };
        }

        /// <summary>
        /// Add endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ServerRegistrationResultModel> RegisterAsync(
            ServerRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Endpoint == null) {
                throw new ArgumentNullException(nameof(request.Endpoint));
            }
            if (string.IsNullOrEmpty(request.Endpoint.Url)) {
                throw new ArgumentException(nameof(request.Endpoint.Url));
            }

            var id = request.Id;
            var endpointTag = new OpcUaEndpointTag(request.Endpoint);

            // If id was passed, look up id
            DeviceTwinModel twin = null;
            if (!string.IsNullOrEmpty(id)) {
                try {
                    twin = await _registry.GetAsync(id);
                    var result = IsRegisteredWithTwin(endpointTag, twin);
                    if (result != null) {
                        _logger.Debug($"Endpoint already registered!", () => twin);
                        return result;
                    }
                }
                catch(ResourceNotFoundException) {
                    // Otherwise, try update this one
                }
            }

            // Validate the endpoint and update request details filling out missing information.
            if (request.Validate ?? true) {
                var validated = await _validator.ValidateAsync(request);
                if (twin != null && validated.Id != twin.Id) {
                    // Device id was updated to something else - including null - add new device
                    _logger.Info($"Validated device id is different from requested {twin.Id}",
                        () => new {
                            request,
                            validated
                        });
                    twin = null;
                }
                endpointTag = new OpcUaEndpointTag(validated.Endpoint);
                id = validated.Id;
            }

            if (twin == null) {
                if (string.IsNullOrEmpty(id)) {
                    id = null;
                    // Try one last pass to see if we find the endpoint model in any twin entry.
                    var results = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                        $"tags.{k_endpointTag}.EndpointId = '{endpointTag.Id}'");
                    foreach (var item in results) {
                        var result = IsRegisteredWithTwin(endpointTag, item);
                        if (result != null) {
                            _logger.Info($"Endpoint already registered under device {item.Id}",
                                () => new {
                                    item,
                                    endpointTag
                                });
                            // Device already registered with endpoint information, return it
                            return result;
                        }
                    }
                }
                // Add new device
                twin = new DeviceTwinModel { Id = id ?? $"opc_{Guid.NewGuid()}" };
                _logger.Debug($"Register new device", () => twin);
            }

            RegisterWithTwin(endpointTag, twin);
            twin = await _registry.CreateOrUpdateAsync(twin);
            return new ServerRegistrationResultModel {
                Id = twin.Id,
                ConnectionString = null
            };
        }

        /// <summary>
        /// Update endpoint
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public async Task PatchAsync(ServerRegistrationModel registration) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (registration.Endpoint == null) {
                throw new ArgumentNullException(nameof(registration.Endpoint));
            }
            if (string.IsNullOrEmpty(registration.Id)) {
                throw new ArgumentException(nameof(registration.Id));
            }
            if (string.IsNullOrEmpty(registration.Endpoint.Url)) {
                throw new ArgumentException(nameof(registration.Endpoint.Url));
            }
            var twin = await _registry.GetAsync(registration.Id);
            var endpointTag = new OpcUaEndpointTag(registration.Endpoint);
            RegisterWithTwin(endpointTag, twin);
            twin = await _registry.CreateOrUpdateAsync(twin);
        }

        /// <summary>
        /// Delete endpoint
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            await _registry.DeleteAsync(id);
        }

        /// <summary>
        /// Check whether endpoint is already registered in twin and return
        /// registration result reflecting that.
        /// </summary>
        /// <returns></returns>
        private static ServerRegistrationResultModel IsRegisteredWithTwin(
            OpcUaEndpointTag endpoint, DeviceTwinModel twin) {
            if (twin.Tags.ContainsKey(k_endpointTag)) {
                var endpointTag = twin.Tags[k_endpointTag].ToObject<OpcUaEndpointTag>();
                if (endpointTag.Equals(endpoint)) {
                    return new ServerRegistrationResultModel {
                        Id = twin.Id,
                        ConnectionString = null
                    };
                }
            }
            return null;
        }

        /// <summary>
        /// Add tag to twin
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="twin"></param>
        private static void RegisterWithTwin(OpcUaEndpointTag endpoint, 
            DeviceTwinModel twin) {
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }
            var json = JToken.FromObject(endpoint);
            if (twin.Tags.ContainsKey(k_endpointTag)) {
                twin.Tags[k_endpointTag] = json;
            }
            else {
                twin.Tags.Add(k_endpointTag, json);
            }
        }

        /// <summary>
        /// Convert device twin to registration model
        /// </summary>
        /// <returns></returns>
        private static ServerRegistrationModel TwinToRegistrationModel(
            DeviceTwinModel twin) {
            if (twin.Tags.ContainsKey(k_endpointTag)) {
                var endpoint = twin.Tags[k_endpointTag].ToObject<OpcUaEndpointTag>();
                return new ServerRegistrationModel {
                    Id = twin.Id,
                    Endpoint = endpoint.ToServiceModel()
                };
            }
            return null;
        }

        private const string k_endpointTag = "endpoint";
        private readonly IIoTHubTwinServices _registry;
        private readonly IOpcUaValidationServices _validator;
        private readonly ILogger _logger;
    }
}
