using System.Collections.Generic;

namespace OpcPublisher
{
    using System;

    /// <summary>
    /// Interface to manage OPC subscriptions. We create a subscription for each different publishing interval
    /// on an endpoint.
    /// </summary>
    public interface IOpcSubscription : IDisposable
    {
        /// <summary>
        /// List of monitored items on this subscription.
        /// </summary>
        List<OpcMonitoredItem> OpcMonitoredItems { get; }

        /// <summary>
        /// The OPC UA stack subscription object.
        /// </summary>
        IOpcUaSubscription OpcUaClientSubscription { get; set; }

        /// <summary>
        /// The publishing interval requested to be used for the subscription.
        /// </summary>
        int RequestedPublishingInterval { get; set; }

        /// <summary>
        /// The actual publishing interval used for the subscription.
        /// </summary>
        double PublishingInterval { get; set; }

        /// <summary>
        /// Flag to signal that the publishing interval was requested by the node configuration.
        /// </summary>
        bool RequestedPublishingIntervalFromConfiguration { get; set; }
    }
}
