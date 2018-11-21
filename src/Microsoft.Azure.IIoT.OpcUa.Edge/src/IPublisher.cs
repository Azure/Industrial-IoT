// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher
    /// </summary>
    public interface IPublisher {

        /// <summary>
        /// Whether publishing is operational
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Start publisher
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stop publisher
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
