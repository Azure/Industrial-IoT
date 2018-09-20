// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Newtonsoft.Json.Linq;
    using Opc.Ua;

    /// <summary>
    /// Variant encoder extensions
    /// </summary>
    public static class VariantEncoderEx {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="builtInType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JToken Encode(this IVariantEncoder encoder, Variant value,
            out BuiltInType builtInType) =>
            encoder.Encode(value, out builtInType, null);

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JToken Encode(this IVariantEncoder encoder, Variant value) =>
            encoder.Encode(value, out var tmp, null);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, JToken value,
            BuiltInType builtinType) =>
            encoder.Decode(value, builtinType, null);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, JToken value) =>
            encoder.Decode(value, BuiltInType.Null, null);
    }
}
