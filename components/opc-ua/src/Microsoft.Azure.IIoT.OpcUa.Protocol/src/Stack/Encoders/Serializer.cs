// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Newtonsoft.Json;

    /// <summary>
    /// Serializer helper
    /// </summary>
    public static class Serializer {

        /// <summary>
        /// Serialize object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string SerializeObject<T>(T obj, ServiceMessageContext context = null) {
            return JsonConvert.SerializeObject(obj, GetSettings(context));
        }

        /// <summary>
        /// Serialize object with formatting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string SerializeObjectPretty<T>(T obj, ServiceMessageContext context = null) {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, GetSettings(context));
        }

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string json, ServiceMessageContext context = null) {
            return JsonConvert.DeserializeObject<T>(json, GetSettings(context));
        }


        /// <summary>
        /// Get ua settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static JsonSerializerSettings GetSettings(ServiceMessageContext context) {
            return JsonConvertEx.GetSettings().AddUaConverters(context);
        }
    }
}
