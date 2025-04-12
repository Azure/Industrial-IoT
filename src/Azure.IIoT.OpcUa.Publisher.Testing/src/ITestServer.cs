// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    /// <summary>
    /// Test server interface
    /// </summary>
    public interface ITestServer
    {
        /// <summary>
        /// Get published nodes json
        /// </summary>
        /// <returns></returns>
        public string PublishedNodesJson { get; }

        /// <summary>
        /// Run in choas mode
        /// </summary>
        bool Chaos { get; set; }

        /// <summary>
        /// Close sessions
        /// </summary>
        /// <param name="deleteSubscriptions"></param>
        void CloseSessions(bool deleteSubscriptions = false);
        void CloseSubscription(uint subscriptionId, bool notifyExpiration);

        /// <summary>
        /// Close subscriptions
        /// </summary>
        /// <param name="notifyExpiration"></param>
        void CloseSubscriptions(bool notifyExpiration = false);
        void NotifySubscriptionExpiration(uint subscriptionId);
    }
}
