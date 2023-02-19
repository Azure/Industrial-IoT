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
    public sealed class DataValueConverter : DecoderEncoderConverter {

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(DataValue);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) {
            using (var decoder = CreateDecoder(reader, serializer)) {
                return decoder.ReadDataValue(null);
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }
            using (var encoder = CreateEncoder(writer, serializer)) {
                encoder.WriteDataValue(null, value as DataValue);
            }
        }
    }
}
