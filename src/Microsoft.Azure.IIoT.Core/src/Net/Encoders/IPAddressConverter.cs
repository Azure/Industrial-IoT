// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using System;
    using System.Net;

    /// <summary>
    /// Writes and reads address from json
    /// </summary>
    internal class IPAddressConverter : JsonConverter<IPAddress> {

        /// <summary>
        /// Can read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="hasExistingValue"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override IPAddress ReadJson(JsonReader reader, Type objectType,
            IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var str = reader.ReadAsString();
            if (string.IsNullOrEmpty(str)) {
                return IPAddress.Any;
            }
            return IPAddress.Parse(str);
        }

        /// <summary>
        /// Can write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, IPAddress value,
            JsonSerializer serializer) {
            writer.WriteToken(JsonToken.String, value?.ToString() ?? string.Empty);
        }
    }
}
