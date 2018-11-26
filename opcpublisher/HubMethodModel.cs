
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

    /// <summary>
    /// Model for a node description.
    /// </summary>
    public class NodeModel
    {
        public NodeModel(string id, int? opcPublishingInterval = null, int? opcSamplingInterval = null, string displayName = null)
        {
            Id = id;
            OpcPublishingInterval = opcPublishingInterval;
            OpcSamplingInterval = opcSamplingInterval;
            DisplayName = displayName;
        }

        public string Id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? OpcPublishingInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? OpcSamplingInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Model for a get info response.
    /// </summary>
    public class GetInfoMethodResponseModel
    {
        public GetInfoMethodResponseModel()
        {
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int VersionMajor { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int VersionMinor { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int VersionPatch { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string OS { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public Architecture OSArchitecture { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string FrameworkDescription { get; set; }
    }

    /// <summary>
    /// Model for a diagnostic info response.
    /// </summary>
    public class DiagnosticInfoMethodResponseModel
    {
        public DiagnosticInfoMethodResponseModel()
        {
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime PublisherStartTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSessions { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfConnectedOpcSessions { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfConnectedOpcSubscriptions { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfMonitoredItems { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int MonitoredItemsQueueCapacity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long MonitoredItemsQueueCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long EnqueueCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long EnqueueFailureCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long NumberOfEvents { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long SentMessages { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime SentLastTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long SentBytes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long FailedMessages { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long TooLargeCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long MissedSendIntervalCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long WorkingSetMB { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int DefaultSendIntervalSeconds { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public uint HubMessageSize { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public TransportType HubProtocol { get; set; }
    }

    /// <summary>
    /// Model for a diagnostic log response.
    /// </summary>
    public class DiagnosticLogMethodResponseModel
    {
        public DiagnosticLogMethodResponseModel()
        {
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int MissedMessageCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int LogMessageCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string[] Log { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int StartupLogMessageCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string[] StartupLog { get; set; }
    }

    /// <summary>
    /// Model for an exit application request.
    /// </summary>
    public class ExitApplicationMethodRequestModel
    {
        public ExitApplicationMethodRequestModel()
        {
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int SecondsTillExit { get; set; }
    }

    /// <summary>
    /// ´Model for a publish node request.
    /// </summary>
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

    /// <summary>
    /// Model for an unpublish node request.
    /// </summary>
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

    /// <summary>
    /// Model for an unpublish all nodes request.
    /// </summary>
    public class UnpublishAllNodesMethodRequestModel
    {
        public UnpublishAllNodesMethodRequestModel(string endpointUrl = null)
        {
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }
    }

    /// <summary>
    /// Model for a get configured endpoints request.
    /// </summary>
    public class GetConfiguredEndpointsMethodRequestModel
    {
        public GetConfiguredEndpointsMethodRequestModel(ulong? continuationToken = null)
        {
            ContinuationToken = continuationToken;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }
    }

    /// <summary>
    /// Model for a get configured endpoints response.
    /// </summary>
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

    /// <summary>
    /// Model for a get configured nodes on endpoint request.
    /// </summary>
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

    /// <summary>
    /// Model class for a get configured nodes on endpoint response.
    /// </summary>
    public class GetConfiguredNodesOnEndpointMethodResponseModel
    {
        public GetConfiguredNodesOnEndpointMethodResponseModel()
        {
            Nodes = new List<NodeModel>();
        }

        /// <param name="nodes"></param>
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
