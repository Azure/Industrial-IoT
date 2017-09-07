
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Publisher
{
    using static Program;

    /// <summary>
    /// Class describing a list of nodes in the ExpandedNodeId format (using nsu as namespace syntax)
    /// </summary>
    public class OpcNodesOnEndpointUrl
    {
        public string ExpandedNodeId;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcSamplingInterval;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcPublishingInterval;
    }

    /// <summary>
    /// Class describing the nodes which should be published. It supports three formats:
    /// - NodeId syntax using the namespace index (ns) syntax
    /// - ExpandedNodeId syntax, using the namespace URI (nsu) syntax
    /// - List of ExpandedNodeId syntax, to allow putting nodes with similar publishing and/or sampling intervals in one object
    /// </summary>
    public partial class PublishConfigFileEntry
    {
        public PublishConfigFileEntry()
        {
        }

        public PublishConfigFileEntry(string nodeId, string endpointUrl)
        {
            NodeId = new NodeId(nodeId);
            EndpointUri = new Uri(endpointUrl);
        }

        [JsonProperty("EndpointUrl")]
        public Uri EndpointUri;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public NodeId NodeId;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<OpcNodesOnEndpointUrl> OpcNodes;
    }

    public class PublishNodeConfig
    {
        public Uri EndpointUri;
        public NodeId NodeId;
        public ExpandedNodeId ExpandedNodeId;
        public int OpcSamplingInterval;
        public int OpcPublishingInterval;

        public PublishNodeConfig(NodeId nodeId, Uri endpointUri, int opcSamplingInterval, int opcPublishingInterval)
        {
            NodeId = nodeId;
            ExpandedNodeId = null;
            EndpointUri = endpointUri;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }
        public PublishNodeConfig(ExpandedNodeId expandedNodeId, Uri endpointUri, int opcSamplingInterval, int opcPublishingInterval)
        {
            NodeId = null;
            ExpandedNodeId = expandedNodeId;
            EndpointUri = endpointUri;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }
    }
}
