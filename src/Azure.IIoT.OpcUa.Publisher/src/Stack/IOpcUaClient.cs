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
        /// Registers a value to read with results pushed to the provided
        /// subscription callback
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="clientHandle"></param>
        /// <returns></returns>
        IAsyncDisposable Sample(TimeSpan samplingRate, ReadValueId nodeToRead,
            string subscriptionName, uint clientHandle);

        /// <summary>
        /// Create a browser to browse the address space and provide
        /// the differences from last browsing operation.
        /// </summary>
        /// <param name="rebrowsePeriod"></param>
        /// <param name="subscriptionName"></param>
        /// <returns></returns>
        IOpcUaBrowser Browse(TimeSpan rebrowsePeriod, string subscriptionName);

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
