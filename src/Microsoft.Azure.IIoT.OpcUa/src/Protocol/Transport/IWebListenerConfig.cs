// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    /// <summary>
    /// Http configurations
    /// </summary>
    public interface IWebListenerConfig {

        /// <summary>
        /// Listener urls
        /// </summary>
        string[] ListenUrls { get; }
    }
}
