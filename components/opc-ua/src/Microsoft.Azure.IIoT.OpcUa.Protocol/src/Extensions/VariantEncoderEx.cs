// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Extensions;

    /// <summary>
    /// Variant encoder extensions
    /// </summary>
    public static class VariantEncoderEx {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JToken Encode(this IVariantEncoder encoder, Variant value) {
            return encoder.Encode(value, out var tmp);
        }

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, JToken value) {
            return encoder.Decode(value, BuiltInType.Null);
        }

        /// <summary>
        /// Decode with data type as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, JToken value,
            string type) {
            return encoder.Decode(value, string.IsNullOrEmpty(type) ? BuiltInType.Null :
                TypeInfo.GetBuiltInType(type.ToNodeId(encoder.Context)));
        }
    }
}
