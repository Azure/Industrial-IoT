using Opc.Ua.Client;
using System;
using System.Collections.Generic;

namespace OpcPublisher
{
    using Opc.Ua;
    using System.Linq;

    /// <summary>
    /// Class to encapsulate OPC UA session API.
    /// </summary>
    public class OpcUaSession : IOpcUaSession
    {
        public OpcUaSession(ApplicationConfiguration configuration, ConfiguredEndpoint endpoint, bool updateBeforeConnect, bool checkDomain, string sessionName, uint sessionTimeout, IUserIdentity identity, IList<string> preferredLocales)
        {
            _session = Session.Create(configuration, endpoint, updateBeforeConnect, checkDomain, sessionName, sessionTimeout, identity, preferredLocales).Result;
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _session?.Dispose();
                _session = null;
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            // do cleanup
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int KeepAliveInterval
        {
            get
            {
                return _session.KeepAliveInterval;
            }
            set
            {
                _session.KeepAliveInterval = value;
            }
        }

        public NodeId SessionId => _session.SessionId;

        public int SubscriptionCount => _session.SubscriptionCount;


        public event KeepAliveEventHandler KeepAlive
        {
            add
            {
                _session.KeepAlive += value;

            }
            remove
            {
                _session.KeepAlive -= value;
            }
        }


        public bool AddSubscription(IOpcUaSubscription subscription) => _session.AddSubscription(subscription.Subscription);

        public bool AddSubscription(Subscription subscription) => _session.AddSubscription(subscription);

        public StatusCode Close() => _session.Close();

        public Node ReadNode(NodeId nodeId) => _session.ReadNode(nodeId);

        public DataValue ReadValue(NodeId nodeId) => _session.ReadValue(nodeId);

        public bool RemoveSubscription(IOpcUaSubscription subscription) => _session.RemoveSubscription(subscription.Subscription);

        public bool RemoveSubscription(Subscription subscription) => _session.RemoveSubscription(subscription);

        public bool RemoveSubscriptions(IEnumerable<IOpcUaSubscription> subscriptions) => _session.RemoveSubscriptions(subscriptions.Select(s => s.Subscription));

        public bool RemoveSubscriptions(IEnumerable<Subscription> subscriptions) => _session.RemoveSubscriptions(subscriptions);

        private Session _session;
    }
}
