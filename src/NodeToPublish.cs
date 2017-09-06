
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Opc.Ua.Publisher
{

    public class NodesOnEndpoint
    {
        public List<ExpandedNodeId> ExpandedNodeIds;
        public int SamplingInterval;
    }

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
        public Uri EndPointUri { get; set; }

        public ExpandedNodeId ExpandedNodeId { get; set; }

        public NodeId NodeId { get; set; }

        public int SamplingInterval { get; set; }

        public NodesOnEndpoint Nodes { get; set; }
    }
}
