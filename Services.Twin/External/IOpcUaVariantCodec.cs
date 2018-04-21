// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.External {
    using Opc.Ua;

    /// <summary>
    /// Variant codec
    /// </summary>
    public interface IOpcUaVariantCodec {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        string Encode(Variant value);

        /// <summary>
        /// Parse string to variant
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        Variant Decode(string value, BuiltInType builtinType,
            int? valueRank);
    }
}