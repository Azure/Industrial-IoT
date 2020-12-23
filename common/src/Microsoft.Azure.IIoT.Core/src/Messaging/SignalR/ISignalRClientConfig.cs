// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR {

    /// <summary>
    /// Signalr client configuration
    /// </summary>
    public interface ISignalRClientConfig {

        /// <summary>
        /// Use message pack or json
        /// </summary>
        bool UseMessagePackProtocol { get; }
    }
}