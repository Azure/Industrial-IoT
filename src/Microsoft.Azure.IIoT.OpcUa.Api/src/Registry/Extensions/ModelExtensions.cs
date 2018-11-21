// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Model extensions
    /// </summary>
    internal static class Extensions {

        /// <summary>
        /// Convert from to
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static R ConvertTo<T, R>(T model) {
            return JsonConvertEx.DeserializeObject<R>(
                JsonConvertEx.SerializeObject(model));
        }
    }
}
