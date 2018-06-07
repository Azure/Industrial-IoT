// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    public static class DeviceTwinModelEx {

        /// <summary>
        /// Convert jtoken to twin
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwinModel(JToken model) {
            var twin = (dynamic)model;
            return new DeviceTwinModel {
                Etag = twin.etag,
                Id = twin.deviceId,
                ModuleId = twin.moduleId,
                Tags = ((JObject)twin.tags)?.Children().ToDictionary(
                    p => ((JProperty)p).Name, p => ((JProperty)p).Value),
                Properties = new TwinPropertiesModel {
                    Desired = ((JObject)twin.properties.desired)?.Children().ToDictionary(
                        p => ((JProperty)p).Name, p => ((JProperty)p).Value),
                    Reported = ((JObject)twin.properties.reported)?.Children().ToDictionary(
                        p => ((JProperty)p).Name, p => ((JProperty)p).Value)
                }
            };
        }

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
        public static JToken ToJson(this DeviceTwinModel twin) {
            return JToken.FromObject(new {
                etag = twin.Etag,
                deviceId = twin.Id,
                moduleId = twin.ModuleId,
                tags = twin.Tags == null ? null :
                    JObject.FromObject(twin.Tags),
                properties = twin.Properties == null ? null : new {
                    desired = twin.Properties.Desired == null ? null :
                        JObject.FromObject(twin.Properties.Desired),
                    reported = twin.Properties.Reported == null ? null :
                        JObject.FromObject(twin.Properties.Reported),
                }
            });
        }

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
