// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.IIoT.Module.Deployment.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration content model extension
    /// </summary>
    public static class ConfigurationContentModelEx {

        /// <summary>
        /// Add modules content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="name"></param>
        /// <param name="model"></param>
        public static void AddModulesContent<T>(this ConfigurationContentModel config,
            string name, T model) {
            if (config.ModulesContent == null) {
                config.ModulesContent = new Dictionary<string, IDictionary<string, object>>();
            }
            if (string.IsNullOrEmpty(name)) {
                name = typeof(T).Name;
            }
            config.ModulesContent.AddOrUpdate(name, new Dictionary<string, object> {
                ["properties.desired"] = JToken.FromObject(model)
            });
        }

        /// <summary>
        /// Add iotedge agent configuration model as modules content
        /// </summary>
        /// <param name="config"></param>
        /// <param name="model"></param>
        public static void AddModulesContent(this ConfigurationContentModel config,
            EdgeAgentConfigurationModel model) {
            config.AddModulesContent("$edgeAgent", model);
        }

        /// <summary>
        /// Add iotedge hub configuration model as modules content
        /// </summary>
        /// <param name="config"></param>
        /// <param name="model"></param>
        public static void AddModulesContent(this ConfigurationContentModel config,
            EdgeHubConfigurationModel model) {
            config.AddModulesContent("$edgeHub", model);
        }

        /// <summary>
        /// Return module content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetModulesContent<T>(this ConfigurationContentModel config,
            string name) {
            if (config?.ModulesContent == null) {
                throw new ArgumentNullException(nameof(config));
            }
            if (!config.ModulesContent.TryGetValue(name, out var prop)) {
                throw new ArgumentException($"config does not contain {name} section",
                    nameof(config));
            }
            if (!prop.TryGetValue("properties.desired", out var module)) {
                throw new ArgumentException($"{name} does not properties.desired section",
                    nameof(config));
            }
            return JsonConvertEx.DeserializeObject<T>(JsonConvertEx.SerializeObject(
                module));
        }

        /// <summary>
        /// Get iotedge agent configuration model from modules content
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static EdgeAgentConfigurationModel GetEdgeAgentConfiguration(
            this ConfigurationContentModel config) {
            return config.GetModulesContent<EdgeAgentConfigurationModel>("$edgeAgent");
        }

        /// <summary>
        /// Get iotedge hub configuration model from modules content
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static EdgeHubConfigurationModel GetEdgeHubConfiguration(
            this ConfigurationContentModel config) {
            return config.GetModulesContent<EdgeHubConfigurationModel>("$edgeHub");
        }
    }
}
