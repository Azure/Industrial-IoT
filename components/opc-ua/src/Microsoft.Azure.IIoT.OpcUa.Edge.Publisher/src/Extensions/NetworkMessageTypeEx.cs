// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Pub Sub encoding extensions
    /// </summary>
    public static class NetworkMessageTypeEx {

        /// <summary>
        /// Get message content mask
        /// </summary>
        /// <returns></returns>
        public static string ToContentType(this MessageEncoding? encoding) {
            if (encoding == null) {
                return MessageSchemaTypes.NetworkMessageJson;
            }
            switch (encoding.Value) {
                case MessageEncoding.Json:
                    return MessageSchemaTypes.NetworkMessageJson;
                case MessageEncoding.Uadp:
                    return MessageSchemaTypes.NetworkMessageUadp;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }
    }
}