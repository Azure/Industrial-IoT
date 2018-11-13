// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Clients {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Module discovery client using twin services
    /// </summary>
    public class IoTHubModuleDiscoveryClient : IModuleDiscovery {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public IoTHubModuleDiscoveryClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<List<DiscoveredModuleModel>> GetModulesAsync(
            string deviceId) {
            // First try to get edge agent twin
            try {
                var agent = await _twin.GetAsync(deviceId, "$edgeAgent");
                var reported = agent.Properties?.Reported;
                var result = new List<DiscoveredModuleModel>();
                if (reported != null) {
                    if (reported.TryGetValue("systemModules", out var systemModules)) {
                        result.AddRange(ToDiscoveredModuleModels(systemModules));
                    }
                    if (reported.TryGetValue("modules", out var modules)) {
                        result.AddRange(ToDiscoveredModuleModels(modules));
                    }
                }
                return result;
            }
            catch (ResourceNotFoundException) {
                // Fall back to find all modules under the device using query
                var modules = await _twin.QueryDeviceTwinsAsync(
                     $"SELECT * FROM devices.modules WHERE deviceId = '{deviceId}'");
                return modules.Select(ToDiscoveredModuleModel).ToList();
            }
        }

        /// <summary>
        /// Convert twin to module model
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static DiscoveredModuleModel ToDiscoveredModuleModel(DeviceTwinModel t) {
            if (t == null) {
                return null;
            }
            var model = new DiscoveredModuleModel {
                Id = t.ModuleId,
                ImageName = t.ModuleId,
                ImageHash = null,
                Version = t.Version?.ToString(),
                Status = t.Status
            };
            return model;
        }

        /// <summary>
        /// Convert modules result to discovered modules models
        /// </summary>
        /// <param name="modules"></param>
        /// <returns></returns>
        private static IEnumerable<DiscoveredModuleModel> ToDiscoveredModuleModels(
            JToken modules) {
            var mod = modules.ToObject<Dictionary<string, EdgeAgentModuleModel>>();
            return mod.Select(kv => new DiscoveredModuleModel {
                Id = kv.Key,
                ImageName = kv.Value.Settings?.Image,
                ImageHash = kv.Value.Settings?.ImageHash,
                Version = kv.Value.Version ??
                    GetVersionFromImageName(kv.Value.Settings?.Image),
                Status = kv.Value.Status ?? kv.Value.RuntimeStatus
            });
        }

        /// <summary>
        /// Parse version out of image name
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static string GetVersionFromImageName(string image) {
            var index = image?.LastIndexOf(':') ?? -1;
            return index == -1 ? null : image.Substring(index + 1);
        }

        /// <summary>
        /// Edge agent managed module
        /// </summary>
        public class EdgeAgentModuleModel {

            /// <summary> Module version </summary>
            [JsonProperty(PropertyName = "version")]
            public string Version { get; set; }

            /// <summary> Module status </summary>
            [JsonProperty(PropertyName = "status")]
            public string Status { get; set; }

            /// <summary> Module status </summary>
            [JsonProperty(PropertyName = "runtimeStatus")]
            public string RuntimeStatus { get; set; }

            /// <summary> Module settings</summary>
            [JsonProperty(PropertyName = "settings")]
            public EdgeAgentModuleSettingsModel Settings { get; set; }
        }

        /// <summary>
        /// Edge agent managed settings
        /// </summary>
        public class EdgeAgentModuleSettingsModel {

            /// <summary> Module image </summary>
            [JsonProperty(PropertyName = "image")]
            public string Image { get; set; }

            /// <summary> Image hash </summary>
            [JsonProperty(PropertyName = "imageHash")]
            public string ImageHash { get; set; }
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
    }
}
