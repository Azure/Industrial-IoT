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
    internal sealed class PhysicalAddressConverter : JsonConverter<PhysicalAddress> {

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="hasExistingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override PhysicalAddress ReadJson(JsonReader reader, Type objectType,
            PhysicalAddress existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                return null;
            }
            return PhysicalAddress.Parse((string)reader.Value);
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, PhysicalAddress value,
            JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
            }
            else {
                writer.WriteToken(JsonToken.String, value.ToString());
            }
        }
    }
}
