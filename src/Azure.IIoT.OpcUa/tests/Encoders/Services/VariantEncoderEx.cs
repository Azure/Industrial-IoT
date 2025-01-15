// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly.Extensions.Serializers;
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
        public static VariantValue Encode(this IVariantEncoder encoder, Variant value)
        {
            return encoder.Encode(value, out _);
        }
    }
}
