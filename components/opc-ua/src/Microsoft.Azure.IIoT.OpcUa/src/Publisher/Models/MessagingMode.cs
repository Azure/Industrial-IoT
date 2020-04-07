// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// Message modes
    /// </summary>
    public enum MessagingMode {

        /// <summary>
        /// Network and dataset messages (default)
        /// </summary>
        PubSub,

        /// <summary>
        /// Monitored item samples
        /// </summary>
        Samples
    }
}