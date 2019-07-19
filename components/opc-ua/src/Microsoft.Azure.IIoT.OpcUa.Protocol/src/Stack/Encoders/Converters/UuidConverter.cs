// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Writes and reads uuids
    /// </summary>
    public sealed class UuidConverter : JsonConverter {

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) {
            return
                objectType == typeof(Uuid) ||
                objectType == typeof(Uuid?);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                if (objectType == typeof(Uuid?)) {
                    return null;
                }
                return Uuid.Empty;
            }
            return new Uuid((string)reader.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer) {
            var uuid = value as Uuid?;
            if (uuid == null) {
                writer.WriteNull();
            }
            else {
                writer.WriteToken(JsonToken.String, uuid.Value.ToString());
            }
        }
    }
}
