// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using System;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Writes address as json
    /// </summary>
    public class PhysicalAddressConverter : JsonConverter<PhysicalAddress> {

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override PhysicalAddress ReadJson(JsonReader reader, Type objectType,
            PhysicalAddress existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var str = reader.ReadAsString();
            if (string.IsNullOrEmpty(str)) {
                return PhysicalAddress.None;
            }
            return PhysicalAddress.Parse(str);
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, PhysicalAddress value,
            JsonSerializer serializer) {
            writer.WriteToken(JsonToken.String, value?.ToString() ??
                string.Empty);
        }
    }
}
