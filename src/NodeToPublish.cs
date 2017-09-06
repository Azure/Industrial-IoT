
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Opc.Ua.Publisher
{
    /// <summary>
    /// Class describing a list of nodes in the ExpandedNodeId format (using nsu as namespace syntax)
    /// </summary>
    public class NodesOnEndpoint
    {
        public List<ExpandedNodeId> ExpandedNodeIds;
        public int? OpcSamplingInterval;
        public int? OpcPublishingInterval;
    }

    /// <summary>
    /// Class describing the nodes which should be published. It supports three formats:
    /// - NodeId syntax using the namespace index (ns) syntax
    /// - ExpandedNodeId syntax, using the namespace URI (nsu) syntax
    /// - List of ExpandedNodeId syntax, to allow putting nodes with similar publishing and/or sampling intervals in one object
    /// </summary>
    public partial class NodeToPublish
    {
        public NodeToPublish()
        {
        }

        public NodeToPublish(string nodeId, string endpointUrl)
        {
            NodeId = new NodeId(nodeId);
            EndPointUri = new Uri(endpointUrl);
        }

        [JsonProperty("EndpointUrl")]
        public Uri EndPointUri;

        public bool ShouldGiveUp;

        public ExpandedNodeId ExpandedNodeId;

        public NodeId NodeId;

        public int? OpcSamplingInterval;
        public int? OpcPublishingInterval;
        public NodesOnEndpoint Nodes;
    }

    /// <summary>
    /// Comparer using the publishing interval as comparison element. Used to identify nodes to put on the same subscription.
    /// </summary>
    class NodePublishingIntervalComparer : IEqualityComparer<NodeToPublish>
    {
        public bool Equals(NodeToPublish n1, NodeToPublish n2)
        {
            if (n1.OpcPublishingInterval == null || n2.OpcPublishingInterval == null)
            {
                throw new Exception("Please make sure that the OpcPublishingInverval value is set for all nodes.");
            }
            if (n1.OpcPublishingInterval == n2.OpcPublishingInterval)
                return true;
            return false;
        }

        public int GetHashCode(NodeToPublish n)
        {
            return n.GetHashCode();
        }
    }
}
