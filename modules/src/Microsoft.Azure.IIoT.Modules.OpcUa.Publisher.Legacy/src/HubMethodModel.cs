using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpcPublisher
{
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
        public string SemanticVersion { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string InformationalVersion { get; set; }

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
        public int NumberOfOpcSessionsConfigured { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSessionsConnected { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSubscriptionsConfigured { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSubscriptionsConnected { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcMonitoredItemsConfigured { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcMonitoredItemsMonitored { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcMonitoredItemsToRemove { get; set; }

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
        public List<string> Log { get; } = new List<string>();
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
            OpcNodes = new List<OpcNodeOnEndpointModel>();
            EndpointUrl = endpointUrl;
            UseSecurity = useSecurity;
            UserName = userName;
            Password = password;
        }

        public string EndpointUrl { get; set; }
        public List<OpcNodeOnEndpointModel> OpcNodes { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OpcAuthenticationMode? OpcAuthenticationMode { get; set; }

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
            OpcNodes = new List<OpcNodeOnEndpointModel>();
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }

        public List<OpcNodeOnEndpointModel> OpcNodes { get; }
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
    /// Model for configured endpoint response element.
    /// </summary>
    public class ConfiguredEndpointModel
    {
        public ConfiguredEndpointModel(string endpointUrl)
        {
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }
    }

    /// <summary>
    /// Model for a get configured endpoints response.
    /// </summary>
    public class GetConfiguredEndpointsMethodResponseModel
    {
        public GetConfiguredEndpointsMethodResponseModel()
        {
            Endpoints = new List<ConfiguredEndpointModel>();
        }

        public GetConfiguredEndpointsMethodResponseModel(List<ConfiguredEndpointModel> endpoints)
        {
            Endpoints = endpoints;
        }
        public List<ConfiguredEndpointModel> Endpoints { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken { get; set; }
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
            OpcNodes = new List<OpcNodeOnEndpointModel>();
        }

        /// <param name="nodes"></param>
        public GetConfiguredNodesOnEndpointMethodResponseModel(List<OpcNodeOnEndpointModel> nodes)
        {
            OpcNodes = nodes;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public List<OpcNodeOnEndpointModel> OpcNodes { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? ContinuationToken { get; set; }
    }
}
