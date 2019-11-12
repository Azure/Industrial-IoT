// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {

    /// <summary>
    /// Encoding configuration for monitored item
    /// </summary>
    public interface IMonitoredItemEncodingConfig : IEncodingConfig {

        /// <summary>
        /// Content the message should contain
        /// </summary>
        uint MessageContentMask { get; }
    }
}