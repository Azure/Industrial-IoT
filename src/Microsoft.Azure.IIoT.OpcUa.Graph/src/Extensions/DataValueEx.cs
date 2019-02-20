// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using System;

    /// <summary>
    /// Data value extensions
    /// </summary>
    public static class DataValueEx {

        /// <summary>
        /// Convert to simple attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static T ToSimpleAttribute<T>(this DataValue value,
            Func<object, T> convert) {
            return value?.Value == null ? default(T) : convert(value?.Value);
        }

        /// <summary>
        /// Convert to variant value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="encoder"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static JToken ToJToken(this Variant value,
            IVariantEncoder encoder, ServiceMessageContext context) {
            return encoder.Encode(value, out var tmp, context);
        }

        /// <summary>
        /// Convert to text attribute
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static LocalizedTextModel ToTextAttribute(this LocalizedText text) {
            return new LocalizedTextModel(text.Text, text.Locale);
        }
    }
}
