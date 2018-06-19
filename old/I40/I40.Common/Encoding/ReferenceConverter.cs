// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Encoding {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Converts a reference into a single string value and back.
    /// </summary>
    public class ReferenceConverter : JsonConverter<Reference> {

        /// <inheritdoc/>
        public override Reference ReadJson(JsonReader reader, Type objectType,
            Reference existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            reader.ReadAsString()?.AsReference();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, Reference value,
            JsonSerializer serializer) => writer.WriteValue(value.AsString());
    }
}