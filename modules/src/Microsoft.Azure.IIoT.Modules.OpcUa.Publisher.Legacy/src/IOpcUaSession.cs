using Opc.Ua.Client;
using System;
using System.Collections.Generic;

namespace OpcPublisher
{
    using Opc.Ua;

    /// <summary>
    /// Interface for an OPC UA session.
    /// </summary>
    public interface IOpcUaSession : IDisposable
    {
        int KeepAliveInterval { get; set; }

        NodeId SessionId { get; }

        int SubscriptionCount { get; }


        event KeepAliveEventHandler KeepAlive;


        bool AddSubscription(IOpcUaSubscription subscription);

        bool AddSubscription(Subscription subscription);

        StatusCode Close();

        DataValue ReadValue(NodeId nodeId);

        Node ReadNode(NodeId nodeId);

        bool RemoveSubscription(IOpcUaSubscription subscription);

        bool RemoveSubscription(Subscription subscription);

        bool RemoveSubscriptions(IEnumerable<IOpcUaSubscription> subscriptions);

        bool RemoveSubscriptions(IEnumerable<Subscription> subscriptions);
    }
}
