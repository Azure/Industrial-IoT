// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;

    public static class JsonConvertEx {

        /// <summary>
        /// Serialize object pretty
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string SerializeObjectPretty(object o) =>
            JsonConvert.SerializeObject(o, Formatting.Indented, ExtendedSettings);

        /// <summary>
        /// Serialize object
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string SerializeObject(object o) =>
            JsonConvert.SerializeObject(o, Formatting.None, DefaultSettings);

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string value) =>
            JsonConvert.DeserializeObject<T>(value, DefaultSettings);

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object DeserializeObject(string value, Type type) =>
            JsonConvert.DeserializeObject(value, type, DefaultSettings);

        /// <summary>
        /// Get default settings
        /// </summary>
        public static JsonSerializerSettings DefaultSettings { get; } =
            new JsonSerializerSettings {
                Converters = new List<JsonConverter> {
                    new ExceptionConverter(),
                    new IsoDateTimeConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 10
            };

        /// <summary>
        /// Get extended settings
        /// </summary>
        public static JsonSerializerSettings ExtendedSettings { get; } =
            new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver {
                    IgnoreSerializableAttribute = true,
                    IgnoreSerializableInterface = true
                },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                Converters = new List<JsonConverter> {
                    new ExceptionConverter(true)
                },
                MaxDepth = 10
            };
    }
}
