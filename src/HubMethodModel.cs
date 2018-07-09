using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpcPublisher
{
    /// <summary>
    /// Classes for method models
    /// </summary>
    public class GetConfiguredNodesMethodData
    {
        public string EndpointUrl;
        public string PublishInterval;
        public string SamplingInterval;
    }

    public class NodeModel
    {
        public NodeModel(string id, int? opcPublishingInterval = null, int? opcSamplingInterval = null)
        {
            Id = id;
            OpcPublishingInterval = opcPublishingInterval;
            OpcSamplingInterval = opcSamplingInterval;
        }

        public string Id;

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? OpcPublishingInterval;

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? OpcSamplingInterval;
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

        public string EndpointUrl;
        public List<NodeModel> Nodes;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool UseSecurity;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string UserName;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Password;
    }

    public class UnpublishNodesMethodRequestModel
    {
        public UnpublishNodesMethodRequestModel(string endpointUrl)
        {
            Nodes = new List<NodeModel>();
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl;
        public List<NodeModel> Nodes;
    }

    public class UnpublishAllNodesMethodRequestModel
    {
        public UnpublishAllNodesMethodRequestModel(string endpointUrl = null)
        {
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl;
    }


    public class GetConfiguredEndpointsMethodRequestModel
    {
        public GetConfiguredEndpointsMethodRequestModel(uint? count = null, ulong? continuationToken = null)
        {
            Count = count;
            ContinuationToken = continuationToken;
        }
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public uint? Count;

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken;
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
        public List<string> Endpoints;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken;
    }

    public class GetConfiguredNodesOnEndpointMethodRequestModel
    {
        public GetConfiguredNodesOnEndpointMethodRequestModel(string endpointUrl, uint? count = null, ulong? continuationToken = null)
        {
            EndpointUrl = endpointUrl;
            Count = count;
            ContinuationToken = continuationToken;
        }
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl;

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public uint? Count;

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken;
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
        public List<NodeModel> Nodes;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken;
    }
}
