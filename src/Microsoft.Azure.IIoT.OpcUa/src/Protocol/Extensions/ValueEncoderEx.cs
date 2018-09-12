// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Newtonsoft.Json.Linq;
    using Opc.Ua;

    /// <summary>
    /// Value encoder extensions
    /// </summary>
    public static class ValueEncoderEx {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JToken Encode(this IValueEncoder encoder, Variant value) =>
            encoder.Encode(value, null);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        public static Variant Decode(this IValueEncoder encoder, JToken value,
            BuiltInType builtinType, int? valueRank) =>
            encoder.Decode(value, builtinType, valueRank, null);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        public static Variant Decode(this IValueEncoder encoder, JToken value,
            BuiltInType builtinType) =>
            encoder.Decode(value, builtinType, null, null);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Variant Decode(this IValueEncoder encoder, JToken value) =>
            encoder.Decode(value, BuiltInType.Null, null, null);
    }
}
