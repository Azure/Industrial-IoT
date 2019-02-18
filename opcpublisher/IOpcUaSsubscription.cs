using Opc.Ua.Client;
using System;
using System.Collections.Generic;

namespace OpcPublisher
{
    /// <summary>
    /// Interface for an OPC UA subscription.
    /// </summary>
    public interface IOpcUaSubscription : IDisposable
    {
        uint Id { get; }

        uint MonitoredItemCount { get; }

        IEnumerable<MonitoredItem> MonitoredItems { get; }

        int PublishingInterval { get; set; }

        Subscription Subscription { get; }


        void AddItem(IOpcUaMonitoredItem monitoredItem);

        void AddItem(MonitoredItem monitoredItem);

        void ApplyChanges();

        void Create();

        void Delete(bool silent);

        void RemoveItems(IEnumerable<IOpcUaMonitoredItem> monitoredItems);

        void RemoveItems(IEnumerable<MonitoredItem> monitoredItems);

        void SetPublishingMode(bool enabled);
    }
}
