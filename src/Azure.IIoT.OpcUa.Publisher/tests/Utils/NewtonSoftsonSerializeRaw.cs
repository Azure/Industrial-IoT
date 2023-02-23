﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Utils {
    using Furly.Extensions.Serializers.Newtonsoft;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Newtonsoft json raw serializer
    /// </summary>
    public class NewtonsoftJsonSerializerRaw : NewtonsoftJsonSerializer {
        /// <summary>
        /// Json raw converter
        /// </summary>
        public sealed class RawJsonConverter : JsonConverter {
            /// <inheritdoc/>
            public override bool CanConvert(Type objectType) {
                return true;
            }

            /// <inheritdoc/>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                // convert parsed JSON back to string
                return JToken.Load(reader).ToString(Formatting.None);
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                // write value directly to output; assumes string is already JSON
                writer.WriteRawValue((string)value);
            }
        }
    }
}
