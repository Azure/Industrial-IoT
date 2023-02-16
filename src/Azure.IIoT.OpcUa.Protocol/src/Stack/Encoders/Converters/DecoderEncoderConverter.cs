// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Opc.Ua.Encoders;
    using Newtonsoft.Json;

    /// <summary>
    /// Base json encoder/decoder based converter
    /// </summary>
    public abstract class DecoderEncoderConverter : JsonConverter {

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        protected JsonDecoderEx CreateDecoder(JsonReader reader, JsonSerializer serializer) {
            if (!(serializer.Context.Context is IServiceMessageContext context)) {
                context = ServiceMessageContext.GlobalContext;
            }
            return new JsonDecoderEx(reader, context, useJsonLoader: false);
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        protected JsonEncoderEx CreateEncoder(JsonWriter writer, JsonSerializer serializer) {
            if (!(serializer.Context.Context is IServiceMessageContext context)) {
                context = ServiceMessageContext.GlobalContext;
            }
            return new JsonEncoderEx(writer, context, JsonEncoderEx.JsonEncoding.Token) {
                IgnoreDefaultValues = serializer.DefaultValueHandling == DefaultValueHandling.Ignore,
                IgnoreNullValues = serializer.NullValueHandling == NullValueHandling.Ignore,
                UseAdvancedEncoding = true
            };
        }
    }
}
