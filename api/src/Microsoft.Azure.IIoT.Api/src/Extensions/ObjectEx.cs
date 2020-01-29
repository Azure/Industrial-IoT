// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using Newtonsoft.Json;

    /// <summary>
    /// Helper mapper
    /// </summary>
    internal static class ObjectEx {


        /// <summary>
        /// Convert from to
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        internal static T Map<T>(this object model) {
            if (model == null) {
                return default;
            }
            return JsonConvertEx.DeserializeObject<T>(
                JsonConvertEx.SerializeObject(model));
        }
    }
}
