// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {

    /// <summary>
    /// Monitored item encoding configuration
    /// </summary>
    public class MonitoredItemEncodingConfig : IMonitoredItemEncodingConfig {

        /// <inheritdoc/>
        public string ContentType { get; set; }

        /// <inheritdoc/>
        public uint MessageContentMask { get; set; }
    }
}