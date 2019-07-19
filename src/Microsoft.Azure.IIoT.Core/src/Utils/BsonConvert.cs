// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Bson helper
    /// </summary>
    public static class BsonConvert {

        /// <summary>
        /// Serialize object
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string SerializeObject(object o) {
            return Convert.ToBase64String(JToken.FromObject(o).ToBson());
        }

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string value) {
            return JTokenEx.FromBson(Convert.FromBase64String(value)).ToObject<T>();
        }

        /// <summary>
        /// Deserialize object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object DeserializeObject(string value, Type type) {
            return JTokenEx.FromBson(Convert.FromBase64String(value)).ToObject(type);
        }
    }
}
