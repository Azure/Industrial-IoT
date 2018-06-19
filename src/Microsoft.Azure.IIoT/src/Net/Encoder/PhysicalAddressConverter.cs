// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using System;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Writes address as json
    /// </summary>
    public class PhysicalAddressConverter : JsonConverter {

        /// <summary>
        /// Handles all exceptions
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) =>
            typeof(PhysicalAddress).IsAssignableFrom(objectType);

        public override bool CanRead => true;

        /// <summary>
        /// Cannot read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) {
            var str = reader.ReadAsString();
            if (string.IsNullOrEmpty(str)) {
                return PhysicalAddress.None;
            }
            return PhysicalAddress.Parse(str);
        }

        public override bool CanWrite => true;

        /// <summary>
        /// Can write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer) {
            writer.WriteToken(JsonToken.String, value?.ToString() ?? 
                string.Empty);
        }
    }
}
