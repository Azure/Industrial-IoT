// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.EventHub {

    /// <summary>
    /// Event hub configuration
    /// </summary>
    public interface IEventHubConfig {

        /// <summary>
        /// Event hub connection string
        /// </summary>
        string EventHubConnString { get; }

        /// <summary>
        /// Event hub name
        /// </summary>
        string EventHubPath { get; }
    }
}
