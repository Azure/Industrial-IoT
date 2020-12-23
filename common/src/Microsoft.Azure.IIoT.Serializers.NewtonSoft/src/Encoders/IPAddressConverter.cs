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
    internal sealed class IPAddressConverter : JsonConverter<IPAddress> {

        /// <inheritdoc/>
        public override IPAddress ReadJson(JsonReader reader, Type objectType,
            IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                return null;
            }
            return IPAddress.Parse((string)reader.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IPAddress value,
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
