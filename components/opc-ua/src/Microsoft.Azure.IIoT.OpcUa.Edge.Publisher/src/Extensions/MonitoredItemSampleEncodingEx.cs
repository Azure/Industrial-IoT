// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Extensions for encoding
    /// </summary>
    public static class MonitoredItemSampleEncodingEx {

        /// <summary>
        /// Get message content mask
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ToContentType(this MonitoredItemMessageEncoding? encoding) {
            if (encoding == null) {
                return MessageSchemaTypes.MonitoredItemMessageJson;
            }
            switch (encoding.Value) {
                case MonitoredItemMessageEncoding.Json:
                    return MessageSchemaTypes.MonitoredItemMessageJson;
                // case MonitoredItemMessageEncoding.Binary:
                //    return MessageSchemaTypes.MonitoredItemMessageBinary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }
    }
}