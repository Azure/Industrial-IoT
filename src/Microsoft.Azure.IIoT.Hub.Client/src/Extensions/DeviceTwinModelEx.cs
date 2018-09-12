// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Device twin model extensions
    /// </summary>
    public static class DeviceTwinModelEx {

        /// <summary>
        /// Convert twin to device
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static Device ToDevice(this DeviceTwinModel twin) =>
            new Device(twin.Id) {
                Capabilities = twin.Capabilities?.ToCapabilities()
            };

        /// <summary>
        /// Convert twin to module
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static Module ToModule(this DeviceTwinModel twin) {
            return new Module(twin.Id, twin.ModuleId) {
                ManagedBy = twin.Id
            };
        }

        /// <summary>
        /// Convert twin to module
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="isPatch"></param>
        /// <returns></returns>
        public static Twin ToTwin(this DeviceTwinModel twin, bool isPatch = false) {
            return new Twin(twin.Id) {
                ETag = twin.Etag,
                ModuleId = twin.ModuleId,
                DeviceId = twin.Id,
                Tags = twin.Tags?.ToTwinCollection(),
                Properties = new TwinProperties {
                    Desired =
                        twin.Properties?.Desired?.ToTwinCollection(),
                    Reported = isPatch ? null :
                        twin.Properties?.Reported?.ToTwinCollection()
                }
            };
        }

        /// <summary>
        /// Convert to twin patch
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        public static Twin ToTwin(this Dictionary<string, JToken> props) {
            return new Twin {
                Properties = new TwinProperties {
                    Desired = props?.ToTwinCollection()
                }
            };
        }

        /// <summary>
        /// Convert twin to module
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToModel(this Twin twin) {
            return new DeviceTwinModel {
                Id = twin.DeviceId,
                Etag = twin.ETag,
                ModuleId = twin.ModuleId,
                Tags = twin.Tags?.ToModel(),
                Properties = new TwinPropertiesModel {
                    Desired =
                        twin.Properties?.Desired?.ToModel(),
                    Reported =
                        twin.Properties?.Reported?.ToModel()
                },
                Capabilities = twin.Capabilities?.ToModel()
            };
        }

        /// <summary>
        /// Convert to twin collection
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        public static TwinCollection ToTwinCollection(
            this Dictionary<string, JToken> props) {
            var collection = new TwinCollection();
            foreach (var item in props) {
                collection[item.Key] = item.Value;
            }
            return collection;
        }

        /// <summary>
        /// Convert to twin collection
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        public static Dictionary<string, JToken> ToModel(
            this TwinCollection props) {
            var model = new Dictionary<string, JToken>();
            foreach (KeyValuePair<string, dynamic> item in props) {
                model.AddOrUpdate(item.Key, (JToken)item.Value);
            }
            return model;
        }
    }
}
