// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Opc.Ua.Extensions;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Writes and reads qualified names
    /// </summary>
    public sealed class QualifiedNameConverter : JsonConverter<QualifiedName> {

        /// <inheritdoc/>
        public override QualifiedName ReadJson(JsonReader reader, Type objectType,
            QualifiedName existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                return null;
            }
            if (!(serializer.Context.Context is IServiceMessageContext context)) {
                context = ServiceMessageContext.GlobalContext;
            }
            return ((string)reader.Value).ToQualifiedName(context);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, QualifiedName value,
            JsonSerializer serializer) {
            if (!(serializer.Context.Context is IServiceMessageContext context)) {
                context = ServiceMessageContext.GlobalContext;
            }
            writer.WriteToken(JsonToken.String, value.AsString(context));
        }
    }
}
