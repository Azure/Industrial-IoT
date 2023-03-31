// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using Opc.Ua.Extensions;

    /// <summary>
    /// Variant encoder extensions
    /// </summary>
    public static class VariantEncoderEx
    {
        /// <summary>
        /// Decode with data type as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, VariantValue value,
            string? type)
        {
            return encoder.Decode(value, string.IsNullOrEmpty(type) ? BuiltInType.Null :
                TypeInfo.GetBuiltInType(type.ToNodeId(encoder.Context)));
        }
    }
}
