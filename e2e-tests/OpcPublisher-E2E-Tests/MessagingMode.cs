// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests
{
    /// <summary>
    /// Message modes the E2E deployments can configure on the module. The
    /// value is passed straight through to the module as <c>--mm</c>, so it
    /// must name a mode the publisher still accepts.
    /// </summary>
    public enum MessagingMode
    {
        /// <summary>
        /// Network and dataset messages (default)
        /// </summary>
        PubSub,

        /// <summary>
        /// PubSub with the full header set. Replaces the Samples and
        /// FullSamples modes, which 3.0 removed: they emitted a
        /// MonitoredItemMessage that has no representation in OPC UA Part 14,
        /// and a module told to use either refuses to start.
        /// </summary>
        FullNetworkMessages
    }
}
