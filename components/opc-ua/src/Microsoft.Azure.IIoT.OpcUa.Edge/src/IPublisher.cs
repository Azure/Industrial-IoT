// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {

    /// <summary>
    /// Publisher
    /// </summary>
    public interface IPublisher : IHost {

        /// <summary>
        /// Whether publishing is operational
        /// </summary>
        bool IsRunning { get; }
    }
}
