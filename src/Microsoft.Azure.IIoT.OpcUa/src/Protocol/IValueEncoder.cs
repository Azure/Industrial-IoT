// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Newtonsoft.Json.Linq;
    using Opc.Ua;

    /// <summary>
    /// Variant codec
    /// </summary>
    public interface IValueEncoder {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        JToken Encode(Variant value, ServiceMessageContext context);

        /// <summary>
        /// Parse token to variant
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <param name="valueRank"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        Variant Decode(JToken value, BuiltInType builtinType,
            int? valueRank, ServiceMessageContext context);
    }
}
