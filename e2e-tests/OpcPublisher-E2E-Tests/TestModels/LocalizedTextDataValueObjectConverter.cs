// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestModels
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    internal sealed class LocalizedTextDataValueObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DataValueObject<string>);
        }

        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var encoded = JObject.Load(reader);
            var value = encoded[nameof(DataValueObject<string>.Value)];
            if (value is JObject localizedText)
            {
                value = localizedText["Text"];
            }
            return new DataValueObject<string>
            {
                Value = value?.Type == JTokenType.Null
                    ? null
                    : value?.ToObject<string>(serializer),
                SourceTimestamp = encoded[
                    nameof(DataValueObject<string>.SourceTimestamp)]
                    ?.ToObject<DateTime?>(serializer),
                ServerTimestamp = encoded[
                    nameof(DataValueObject<string>.ServerTimestamp)]
                    ?.ToObject<DateTime?>(serializer)
            };
        }

        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            JObject.FromObject(value, serializer).WriteTo(writer);
        }
    }
}
