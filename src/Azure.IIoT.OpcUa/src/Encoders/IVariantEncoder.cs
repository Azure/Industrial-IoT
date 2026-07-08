// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly.Extensions.Serializers;
    using Opc.Ua;

    /// <summary>
    /// Variant codec
    /// </summary>
    public interface IVariantEncoder
    {
        /// <summary>
        /// Get context
        /// </summary>
        IServiceMessageContext Context { get; }

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <param name="useReversibleEncoding">Whether to use the reversible
        /// OPC UA JSON encoding (the default) which preserves all type
        /// information, or the non-reversible encoding which produces a more
        /// compact representation.</param>
        /// <returns></returns>
        VariantValue Encode(Variant? value, out BuiltInType builtinType,
            bool useReversibleEncoding = true);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        Variant Decode(VariantValue value, BuiltInType builtinType);
    }
}
