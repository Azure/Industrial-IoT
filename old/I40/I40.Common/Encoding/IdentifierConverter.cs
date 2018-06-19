// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Encoding {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Converts an identifier into a single string value and back.
    /// </summary>
    public class IdentifierConverter : JsonConverter<Identification> {

        /// <inheritdoc/>
        public override Identification ReadJson(JsonReader reader, Type objectType,
            Identification existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            reader.ReadAsString()?.AsIdentifier();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, Identification value,
            JsonSerializer serializer) => writer.WriteValue(value.AsString());
    }
}