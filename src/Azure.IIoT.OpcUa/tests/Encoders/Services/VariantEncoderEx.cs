// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System.Text.Json.Nodes;
    using Opc.Ua;

    /// <summary>
    /// Variant encoder extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JsonNode? Encode(this IVariantEncoder encoder, Variant value)
        {
            return encoder.Encode(value, out _);
        }

        /// <summary>
        /// Decode a raw string value
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, string value,
            BuiltInType builtinType)
        {
            return encoder.Decode(JsonValue.Create(value), builtinType);
        }
    }
}
