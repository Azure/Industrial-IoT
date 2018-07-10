using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpcPublisher
{
    /// <summary>
    /// Classes for method models
    /// </summary>
    public class GetConfiguredNodesMethodData
    {
        public string EndpointUrl { get; set; }
        public string PublishInterval { get; set; }
        public string SamplingInterval { get; set; }
    }

    public class NodeModel
    {
        public NodeModel(string id, int? opcPublishingInterval = null, int? opcSamplingInterval = null)
        {
            Id = id;
            OpcPublishingInterval = opcPublishingInterval;
            OpcSamplingInterval = opcSamplingInterval;
        }

        public string Id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? OpcPublishingInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? OpcSamplingInterval { get; set; }
    }

    public class PublishNodesMethodRequestModel
    {
        public PublishNodesMethodRequestModel(string endpointUrl, bool useSecurity = true, string userName = null, string password = null)
        {
            Nodes = new List<NodeModel>();
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity;
            UserName = userName;
            Password = password;
        }

        public string EndpointUrl { get; set; }
        public List<NodeModel> Nodes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool UseSecurity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string UserName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }
    }

    public class UnpublishNodesMethodRequestModel
    {
        public UnpublishNodesMethodRequestModel(string endpointUrl)
        {
            Nodes = new List<NodeModel>();
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }
        public List<NodeModel> Nodes { get; set; }
    }

    public class UnpublishAllNodesMethodRequestModel
    {
        public UnpublishAllNodesMethodRequestModel(string endpointUrl = null)
        {
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }
    }


    public class GetConfiguredEndpointsMethodRequestModel
    {
        public GetConfiguredEndpointsMethodRequestModel(ulong? continuationToken = null)
        {
            ContinuationToken = continuationToken;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }
    }


    public class GetConfiguredEndpointsMethodResponseModel
    {
        public GetConfiguredEndpointsMethodResponseModel()
        {
            Endpoints = new List<string>();
        }

        public GetConfiguredEndpointsMethodResponseModel(List<string> endpoints)
        {
            Endpoints = endpoints;
        }
        public List<string> Endpoints { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken;
    }

    public class GetConfiguredNodesOnEndpointMethodRequestModel
    {
        public GetConfiguredNodesOnEndpointMethodRequestModel(string endpointUrl, ulong? continuationToken = null)
        {
            EndpointUrl = endpointUrl;
            ContinuationToken = continuationToken;
        }
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }
    }


    public class GetConfiguredNodesOnEndpointMethodResponseModel
    {
        public GetConfiguredNodesOnEndpointMethodResponseModel()
        {
            Nodes = new List<NodeModel>();
        }

        public GetConfiguredNodesOnEndpointMethodResponseModel(List<NodeModel> nodes)
        {
            Nodes = nodes;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public List<NodeModel> Nodes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken { get; set; }
    }
}
