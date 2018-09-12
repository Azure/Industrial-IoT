// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Model extensions
    /// </summary>
    public static class DeviceTwinModelEx {

        /// <summary>
        /// Convert jtoken to twin
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwinModel(JToken model) =>
            model.ToObject<DeviceTwinModel>();

        /// <summary>
        /// Convert json to twin
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwinModel(string json) =>
             ToDeviceTwinModel(JToken.Parse(json));

        /// <summary>
        /// Convert twin to json
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static JToken ToJson(this DeviceTwinModel twin) =>
            JToken.FromObject(twin);

        /// <summary>
        /// Consolidated
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static Dictionary<string, JToken> GetConsolidatedProperties(
            this DeviceTwinModel model) {

            var desired = model.Properties?.Desired;
            var reported = model.Properties?.Reported;
            if (reported == null || desired == null) {
                return (reported ?? desired) ??
                    new Dictionary<string, JToken>();
            }

            var properties = new Dictionary<string, JToken>(desired);

            // Merge with reported
            foreach (var prop in reported) {
                if (properties.TryGetValue(prop.Key, out var existing)) {
                    if (existing == null || prop.Value == null) {
                        if (existing == prop.Value) {
                            continue;
                        }
                    }
                    else if (JToken.DeepEquals(existing, prop.Value)) {
                        continue;
                    }
                    properties[prop.Key] = prop.Value;
                }
                else {
                    properties.Add(prop.Key, prop.Value);
                }
            }
            return properties;
        }
    }
}
