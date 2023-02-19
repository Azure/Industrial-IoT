// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Opc.Ua.Extensions;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Writes and reads localized text from json
    /// </summary>
    public sealed class LocalizedTextConverter : JsonConverter<LocalizedText> {

        /// <inheritdoc/>
        public override LocalizedText ReadJson(JsonReader reader, Type objectType,
            LocalizedText existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                return null;
            }
            return ((string)reader.Value).ToLocalizedText();
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, LocalizedText value,
            JsonSerializer serializer) {
            var str = value.AsString();
            if (str == null) {
                writer.WriteNull();
            }
            else {
                writer.WriteToken(JsonToken.String, str);
            }
        }
    }
}
