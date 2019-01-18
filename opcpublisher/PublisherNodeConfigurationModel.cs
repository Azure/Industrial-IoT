
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
        public OpcNodeOnEndpointModel(string id, string expandedNodeId = null, int? opcSamplingInterval = null, int? opcPublishingInterval = null, string displayName = null)
        {
            Id = id;
            ExpandedNodeId = expandedNodeId;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
            DisplayName = displayName;
        }

        // Id can be:
        // a NodeId ("ns=")
        // an ExpandedNodeId ("nsu=")
        public string Id { get; set; }

        // support legacy configuration file syntax
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExpandedNodeId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcSamplingInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcPublishingInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Class describing the nodes which should be published.
    /// </summary>
    public partial class PublisherConfigurationFileEntryModel
    {
        public PublisherConfigurationFileEntryModel()
        {
        }

        public PublisherConfigurationFileEntryModel(string endpointUrl)
        {
            EndpointUrl = new Uri(endpointUrl);
            OpcNodes = new List<OpcNodeOnEndpointModel>();
        }

        public Uri EndpointUrl { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<OpcNodeOnEndpointModel> OpcNodes { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }

    /// <summary>
    /// Describes the publishing information of a node.
    /// </summary>
    public class NodePublishingConfigurationModel
    {
        public string EndpointUrl { get; set; }
        public bool UseSecurity { get; set; }
        public NodeId NodeId { get; set; }
        public ExpandedNodeId ExpandedNodeId { get; set; }
        public string OriginalId { get; set; }
        public string DisplayName { get; set; }
        public int? OpcSamplingInterval { get; set; }
        public int? OpcPublishingInterval { get; set; }

        public NodePublishingConfigurationModel(ExpandedNodeId expandedNodeId, string originalId, string endpointUrl, bool? useSecurity, int? opcPublishingInterval, int? opcSamplingInterval, string displayName)
        {
            NodeId = null;
            ExpandedNodeId = expandedNodeId;
            OriginalId = originalId;
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity ?? true;
            DisplayName = displayName;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
        }

        public NodePublishingConfigurationModel(NodeId nodeId, string originalId, string endpointUrl, bool? useSecurity, int? opcPublishingInterval, int? opcSamplingInterval, string displayName)
        {
            NodeId = nodeId;
            ExpandedNodeId = null;
            OriginalId = originalId;
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity ?? true;
            DisplayName = displayName;
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
#pragma warning disable CA2227 // Collection properties should be read only
        public List<OpcNodeOnEndpointModel> OpcNodes { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }



}
