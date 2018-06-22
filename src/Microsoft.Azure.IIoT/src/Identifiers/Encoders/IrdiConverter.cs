// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using Microsoft.Azure.IIoT.Identifiers;
    using System;

    /// <summary>
    /// Converts an irdi into a single string value and back.
    /// </summary>
    public class IrdiConverter : JsonConverter<Irdi> {

        /// <inheritdoc/>
        public override Irdi ReadJson(JsonReader reader, Type objectType,
            Irdi existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var str = reader.ReadAsString();
            if (string.IsNullOrEmpty(str)) {
                return null;
            }
            return Irdi.Parse(reader.ReadAsString());
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, Irdi value,
            JsonSerializer serializer) => writer.WriteValue(value.ToString());
    }
}