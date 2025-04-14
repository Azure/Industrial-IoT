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
        /// Run in choas mode and randomly delete sessions, subscriptions
        /// inject errors and so on.
        /// </summary>
        bool Chaos { get; set; }

        /// <summary>
        /// Inject errors responding to incoming requests. The error
        /// rate is the probability of injection, e.g. 3 means 1 out
        /// of 3 requests will be injected with an error.
        /// </summary>
        int InjectErrorResponseRate { get; set; }

        /// <summary>
        /// Close sessions
        /// </summary>
        /// <param name="deleteSubscriptions"></param>
        void CloseSessions(bool deleteSubscriptions = false);

        /// <summary>
        /// Close subscription. Notify expiration (timeout) of the
        /// subscription before closing (status message) if notifyExpiration
        /// is set to true.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="notifyExpiration"></param>
        void CloseSubscription(uint subscriptionId, bool notifyExpiration);

        /// <summary>
        /// Close all subscriptions. Notify expiration (timeout) of the
        /// subscription before closing (status message) if notifyExpiration
        /// is set to true.
        /// </summary>
        /// <param name="notifyExpiration"></param>
        void CloseSubscriptions(bool notifyExpiration = false);
    }
}
