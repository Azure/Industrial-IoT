// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Uses encoder and decoder on UA types
    /// </summary>
    public sealed class VariantConverter : DecoderEncoderConverter {

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Variant) || objectType == typeof(Variant?);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) {
                if (objectType == typeof(Variant?)) {
                    return null;
                }
                return Variant.Null;
            }
            using (var decoder = CreateDecoder(reader, serializer)) {
                return decoder.ReadVariant(null);
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer) {
            var variant = value as Variant?;
            if (variant == null) {
                writer.WriteNull();
            }
            else {
                using (var encoder = CreateEncoder(writer, serializer)) {
                    encoder.WriteVariant(null, variant.Value);
                }
            }
        }
    }
}
