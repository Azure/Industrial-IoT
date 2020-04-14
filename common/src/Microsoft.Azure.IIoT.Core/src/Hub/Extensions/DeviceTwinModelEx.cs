// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Model extensions
    /// </summary>
    public static class DeviceTwinModelEx {

        /// <summary>
        /// Check whether twin is connected
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static bool? IsConnected(this DeviceTwinModel twin) {
            return twin.ConnectionState?.EqualsIgnoreCase("connected");
        }

        /// <summary>
        /// Check whether twin is enabled
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static bool? IsEnabled(this DeviceTwinModel twin) {
            return twin.Status?.EqualsIgnoreCase("enabled");
        }

        /// <summary>
        /// Check whether twin is disabled
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static bool? IsDisabled(this DeviceTwinModel twin) {
            return twin.Status?.EqualsIgnoreCase("disabled");
        }

        /// <summary>
        /// Clone twin
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DeviceTwinModel Clone(this DeviceTwinModel model) {
            if (model == null) {
                return null;
            }
            return new DeviceTwinModel {
                Capabilities = model.Capabilities == null ? null :
                    new DeviceCapabilitiesModel {
                        IotEdge = model.Capabilities.IotEdge
                    },
                ConnectionState = model.ConnectionState,
                Etag = model.Etag,
                Id = model.Id,
                LastActivityTime = model.LastActivityTime,
                ModuleId = model.ModuleId,
                Properties = model.Properties.Clone(),
                Status = model.Status,
                StatusReason = model.StatusReason,
                StatusUpdatedTime = model.StatusUpdatedTime,
                Tags = model.Tags?
                    .ToDictionary(kv => kv.Key, kv => kv.Value?.Copy()),
                Version = model.Version
            };
        }

        /// <summary>
        /// Consolidated
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static Dictionary<string, VariantValue> GetConsolidatedProperties(
            this DeviceTwinModel model) {

            var desired = model.Properties?.Desired;
            var reported = model.Properties?.Reported;
            if (reported == null || desired == null) {
                return (reported ?? desired) ??
                    new Dictionary<string, VariantValue>();
            }

            var properties = new Dictionary<string, VariantValue>(desired);

            // Merge with reported
            foreach (var prop in reported) {
                if (properties.TryGetValue(prop.Key, out var existing)) {
                    if (VariantValueEx.IsNull(existing) || VariantValueEx.IsNull(prop.Value)) {
                        if (VariantValueEx.IsNull(existing) && VariantValueEx.IsNull(prop.Value)) {
                            continue;
                        }
                    }
                    else if (VariantValue.DeepEquals(existing, prop.Value)) {
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
