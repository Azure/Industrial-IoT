// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models
{
    using Microsoft.Azure.Devices.Shared;
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;

    /// <summary>
    /// Device twin model extensions
    /// </summary>
    public static class DeviceTwinModelEx
    {
        /// <summary>
        /// Convert twin to twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="isPatch"></param>
        /// <returns></returns>
        public static Twin ToTwin(this DeviceTwinModel twin, bool isPatch)
        {
            if (twin == null)
            {
                return null;
            }
            return new Twin(twin.Id)
            {
                ETag = twin.Etag,
                ModuleId = twin.ModuleId,
                DeviceId = twin.Id,
                Tags = twin.Tags?.ToTwinCollection(),
                Capabilities = null, // r/o
                Version = null, // r/o
                Properties = new TwinProperties
                {
                    Desired =
                        twin.Properties?.Desired?.ToTwinCollection(),
                    Reported = isPatch ? null :
                        twin.Properties?.Reported?.ToTwinCollection()
                }
            };
        }

        /// <summary>
        /// Convert to twin collection
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        private static TwinCollection ToTwinCollection(
            this Dictionary<string, VariantValue> props)
        {
            var collection = new TwinCollection();
            foreach (var item in props)
            {
                collection[item.Key] = item.Value.IsListOfValues ? item.Value.Values : item.Value.Value;
            }
            return collection;
        }

        /// <summary>
        /// Convert to twin properties model
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        public static Dictionary<string, VariantValue> DeserializeTwinProperties(
            this IJsonSerializer serializer, TwinCollection props)
        {
            if (props == null)
            {
                return null;
            }
            var model = new Dictionary<string, VariantValue>();
            foreach (KeyValuePair<string, dynamic> item in props)
            {
                model.AddOrUpdate(item.Key, (VariantValue)serializer.FromObject(item.Value));
            }
            return model;
        }
    }
}
