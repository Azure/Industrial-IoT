
using Newtonsoft.Json;
using System;

namespace Opc.Ua.Publisher
{
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

        public NodeId NodeId { get; set; }
    }
}
