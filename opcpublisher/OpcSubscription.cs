
using Opc.Ua.Client;
using System.Collections.Generic;

namespace OpcPublisher
{
    using static OpcApplicationConfiguration;

    /// <summary>
    /// Class to manage OPC subscriptions. We create a subscription for each different publishing interval
    /// on an Endpoint.
    /// </summary>
    public class OpcSubscription
    {
        public List<OpcMonitoredItem> OpcMonitoredItems;

        public Subscription OpcUaClientSubscription;

        public int RequestedPublishingInterval { get; set; }

        public double PublishingInterval { get; set; }

        public bool RequestedPublishingIntervalFromConfiguration { get; set; }

        public OpcSubscription(int? publishingInterval)
        {
            OpcMonitoredItems = new List<OpcMonitoredItem>();
            RequestedPublishingInterval = publishingInterval == null ? OpcPublishingInterval : (int)publishingInterval;
            RequestedPublishingIntervalFromConfiguration = publishingInterval != null ? true : false;
            PublishingInterval = RequestedPublishingInterval;
        }
    }
}
