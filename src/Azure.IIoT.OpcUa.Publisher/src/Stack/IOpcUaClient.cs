// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using System;

    /// <summary>
    /// Opc Ua client provides access to sessions services. It must be disposed
    /// when not used as the inner session state is ref counted.
    /// </summary>
    internal interface IOpcUaClient : IDisposable
    {
        /// <summary>
        /// Registers a callback that will trigger at the specified
        /// sampling rate and executing the read operation.
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="nodeToRead"></param>
        /// <returns></returns>
        IOpcUaSampler Sample(TimeSpan samplingRate, ReadValueId nodeToRead);

        /// <summary>
        /// Create a browser to browse the address space and provide
        /// the differences from last browsing operation.
        /// </summary>
        /// <param name="rebrowsePeriod"></param>
        /// <param name="startNodeId"></param>
        /// <returns></returns>
        IOpcUaBrowser Browse(TimeSpan rebrowsePeriod, NodeId startNodeId);

        /// <summary>
        /// Trigger the client to manage the subscription. This is a
        /// no op if the subscription is not registered or the client
        /// is not connected.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="closeSubscription"></param>
        void ManageSubscription(IOpcUaSubscription subscription,
            bool closeSubscription = false);
    }
}
