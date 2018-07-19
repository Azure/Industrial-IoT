
using Newtonsoft.Json;
using Opc.Ua;
using System;
using System.Collections.Generic;

namespace OpcPublisher
{
    using System.ComponentModel;


    /// <summary>
    /// Class describing a list of nodes
    /// </summary>
    public class OpcNodeOnEndpointModel
    {
        // Id can be:
        // a NodeId ("ns=")
        // an ExpandedNodeId ("nsu=")
        public string Id;

        // support legacy configuration file syntax
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExpandedNodeId;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcSamplingInterval;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcPublishingInterval;
    }

    /// <summary>
    /// Class describing the nodes which should be published.
    /// </summary>
    public partial class PublisherConfigurationFileEntryModel
    {
        public PublisherConfigurationFileEntryModel()
        {
        }

        public PublisherConfigurationFileEntryModel(string nodeId, string endpointUrl)
        {
            EndpointUrl = new Uri(endpointUrl);
            OpcNodes = new List<OpcNodeOnEndpointModel>();
        }

        public Uri EndpointUrl { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<OpcNodeOnEndpointModel> OpcNodes { get; set; }
    }

    /// <summary>
    /// Describes the publishing information of a node.
    /// </summary>
    public class NodePublishingConfigurationModel
    {
        public Uri EndpointUrl;
        public bool UseSecurity;
        public NodeId NodeId;
        public ExpandedNodeId ExpandedNodeId;
        public string OriginalId;
        public int OpcSamplingInterval;
        public int OpcPublishingInterval;

        public NodePublishingConfigurationModel(ExpandedNodeId expandedNodeId, string originalId, Uri endpointUrl, bool? useSecurity, int opcSamplingInterval, int opcPublishingInterval)
        {
            NodeId = null;
            ExpandedNodeId = expandedNodeId;
            OriginalId = originalId;
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity ?? true;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }

        public NodePublishingConfigurationModel(NodeId nodeId, string originalId, Uri endpointUrl, bool? useSecurity, int opcSamplingInterval, int opcPublishingInterval)
        {
            NodeId = nodeId;
            ExpandedNodeId = null;
            OriginalId = originalId;
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity ?? true;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }
    }

    /// <summary>
    /// Class describing the nodes which should be published. It supports two formats:
    /// - NodeId syntax using the namespace index (ns) syntax
    /// - List of ExpandedNodeId syntax, to allow putting nodes with similar publishing and/or sampling intervals in one object
    /// </summary>
    public partial class PublisherConfigurationFileEntryLegacyModel
    {
        public PublisherConfigurationFileEntryLegacyModel()
        {
        }

        public PublisherConfigurationFileEntryLegacyModel(string nodeId, string endpointUrl)
        {
            NodeId = new NodeId(nodeId);
            EndpointUrl = new Uri(endpointUrl);
        }

        public Uri EndpointUrl { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public NodeId NodeId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<OpcNodeOnEndpointModel> OpcNodes { get; set; }
    }



}
