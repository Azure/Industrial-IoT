// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    /// <summary>
    /// Subscription callbacks
    /// </summary>
    public interface ISubscriptionCallbacks
    {
        /// <summary>
        /// Called when the subscription is updated
        /// </summary>
        /// <param name="subscriptionHandle"></param>
        public void OnSubscriptionUpdated(
            ISubscriptionHandle? subscriptionHandle);

        /// <summary>
        /// Called when a keep alive notification is received
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionKeepAlive(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when subscription data changes
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionDataChange(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when event changes
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionEventChange(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// Diagnostics for data change notifications
        /// </summary>
        /// <param name="liveData"></param>
        /// <param name="valueChanges"></param>
        /// <param name="heartbeats"></param>
        /// <param name="cyclicReads"></param>
        void OnSubscriptionDataDiagnosticsChange(bool liveData,
            int valueChanges, int heartbeats, int cyclicReads);

        /// <summary>
        /// Event diagnostics
        /// </summary>
        /// <param name="liveData"></param>
        /// <param name="events"></param>
        void OnSubscriptionEventDiagnosticsChange(bool liveData,
            int events);
    }
}
