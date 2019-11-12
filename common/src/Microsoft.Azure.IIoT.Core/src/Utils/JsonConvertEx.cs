// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Serilog;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Json convert helpers
    /// </summary>
    public static class JsonConvertEx {

        /// <summary>
        /// Serialize object pretty
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string SerializeObjectPretty(object o) {
            return JsonConvert.SerializeObject(o, Formatting.Indented, ExtendedSettings);
        }

        /// <summary>
        /// Serialize object
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string SerializeObject(object o) {
            return JsonConvert.SerializeObject(o, Formatting.None, DefaultSettings);
        }

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string value) {
            return JsonConvert.DeserializeObject<T>(value, DefaultSettings);
        }

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object DeserializeObject(string value, Type type) {
            return JsonConvert.DeserializeObject(value, type, DefaultSettings);
        }

        /// <summary>
        /// Get core settings
        /// </summary>
        /// <returns></returns>
        internal static JsonSerializerSettings GetDefaultSettings() {
            return new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver(),
                Converters = new List<JsonConverter>(),
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 20
            };
        }

        /// <summary>
        /// Get settings
        /// </summary>
        /// <param name="permissive"></param>
        /// <returns></returns>
        public static JsonSerializerSettings GetSettings(bool permissive = false) {
            var defaultSettings = GetDefaultSettings();
            defaultSettings.Converters.Add(new ExceptionConverter(permissive));
            defaultSettings.Converters.Add(new IsoDateTimeConverter());
            defaultSettings.Converters.Add(new PhysicalAddressConverter());
            defaultSettings.Converters.Add(new IPAddressConverter());
            defaultSettings.Converters.Add(new StringEnumConverter());
            if (!permissive) {
                defaultSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
            }
            return defaultSettings;
        }

        /// <summary>
        /// Get default settings
        /// </summary>
        internal static JsonSerializerSettings DefaultSettings { get; } = GetSettings(false);

        /// <summary>
        /// Get extended settings
        /// </summary>
        internal static JsonSerializerSettings ExtendedSettings { get; } = GetSettings(true);
    }
}
