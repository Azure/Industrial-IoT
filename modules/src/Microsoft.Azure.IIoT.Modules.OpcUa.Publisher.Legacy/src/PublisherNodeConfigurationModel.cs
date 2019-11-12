// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Crypto;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// Class describing a list of nodes
    /// </summary>
    public class OpcNodeOnEndpointModel
    {
        public OpcNodeOnEndpointModel(string id, string expandedNodeId = null, int? opcSamplingInterval = null, int? opcPublishingInterval = null,
            string displayName = null, int? heartbeatInterval = null, bool? skipFirst = null)
        {
            Id = id;
            ExpandedNodeId = expandedNodeId;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
            DisplayName = displayName;
            HeartbeatInterval = heartbeatInterval;
            SkipFirst = skipFirst;
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

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? HeartbeatInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? SkipFirst { get; set; }
    }
    /// <summary>
    /// Class describing the nodes which should be published.
    /// </summary>
    public class PublisherConfigurationFileEntryModel
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

        // Optional - since 2.6

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityProfileUri { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityMode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Gets ot sets the authentication mode to authenticate against the OPC UA Server.
        /// </summary>
        [DefaultValue(OpcAuthenticationMode.Anonymous)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// Gets or sets the encrypted username to authenticate against the OPC UA Server (when OpcAuthenticationMode is set to UsernamePassword
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string EncryptedAuthUsername
        {
            get
            {
                return EncryptedAuthCredential?.UserName;
            }
            set
            {
                if (EncryptedAuthCredential == null)
                {
                    EncryptedAuthCredential = new EncryptedNetworkCredential();
                }

                EncryptedAuthCredential.UserName = value;
            }
        }

        /// <summary>
        /// Gets or sets the encrypted password to authenticate against the OPC UA Server (when OpcAuthenticationMode is set to UsernamePassword
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string EncryptedAuthPassword
        {
            get
            {
                return EncryptedAuthCredential?.Password;
            }
            set
            {
                if (EncryptedAuthCredential == null)
                {
                    EncryptedAuthCredential = new EncryptedNetworkCredential();
                }

                EncryptedAuthCredential.Password = value;
            }
        }

        /// <summary>
        /// Gets or sets the encrpyted credential to authenticate against the OPC UA Server (when OpcAuthenticationMode is set to UsernamePassword.
        /// </summary>
        [JsonIgnore]
        public EncryptedNetworkCredential EncryptedAuthCredential { get; set; }

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
        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Optional Endpoint identity - defaults to Uri
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Flag if a secure transport should be used to connect to the endpoint.
        /// </summary>
        public bool? UseSecurity { get; set; }

        // Optional - defines the exact endpoint - otherwise UseSecurity is used - since 2.6

        public string SecurityProfileUri { get; set; }
        public string SecurityMode { get; set; }

        /// <summary>
        /// The node id as it was configured.
        /// </summary>
        public string OriginalId { get; set; }

        /// <summary>
        /// The display name to use for the node in telemetry events.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The OPC UA sampling interval for the node.
        /// </summary>
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// The OPC UA publishing interval for the node.
        /// </summary>
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// Flag to enable a hardbeat telemetry event publish for the node.
        /// </summary>
        public int? HeartbeatInterval { get; set; }

        /// <summary>
        /// Flag to skip the first telemetry event for the node after connect.
        /// </summary>
        public bool? SkipFirst { get; set; }

        /// <summary>
        /// Gets or sets the authentication mode to authenticate against the OPC UA Server.
        /// </summary>
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// Gets or sets the encrypted auth credential when OpcAuthenticationMode is set to UsernamePassword.
        /// </summary>
        public EncryptedNetworkCredential EncryptedAuthCredential { get; set; }

        /// <summary>
        /// Ctor of the object.
        /// </summary>
        public NodePublishingConfigurationModel(string originalId, string endpointUrl,
            string endpointId, bool? useSecurity, string securityProfileUri, string securityMode,
            int? opcPublishingInterval, int? opcSamplingInterval, string displayName,
            int? heartbeatInterval, bool? skipFirst, OpcAuthenticationMode opcAuthenticationMode,
            EncryptedNetworkCredential encryptedAuthCredential)

        {
            OriginalId = originalId;
            EndpointUrl = endpointUrl;
            if (endpointId == null)
            {
                // Create an endpoint id lazily
                endpointId = CreateEndpointId(endpointUrl, useSecurity, securityProfileUri,
                    securityMode, encryptedAuthCredential);
            }
            EndpointId = endpointId;
            UseSecurity = useSecurity;
            SecurityProfileUri = securityProfileUri;
            SecurityMode = securityMode;
            DisplayName = displayName;
            OpcSamplingInterval = opcSamplingInterval;
            OpcPublishingInterval = opcPublishingInterval;
            HeartbeatInterval = heartbeatInterval;
            SkipFirst = skipFirst;
            OpcAuthenticationMode = opcAuthenticationMode;
            EncryptedAuthCredential = encryptedAuthCredential;
        }

        /// <summary>
        /// Create endpoint id
        /// </summary>
        public static string CreateEndpointId(string endpointUrl, bool? useSecurity,
            string securityProfileUri, string securityMode,
            EncryptedNetworkCredential encryptedAuthCredential)
        {
            // Create unique id to distinguish subscriptions
            return "uap" + (endpointUrl.ToLowerInvariant() +
                securityProfileUri?.ToLowerInvariant() ?? "" +
                securityMode?.ToLowerInvariant() ?? "" +
                encryptedAuthCredential?.GetHashCode().ToString() ?? "" +
                (useSecurity == null ? "" : useSecurity.ToString()))
                    .ToSha1Hash();
        }
    }

    /// <summary>
    /// Class describing the nodes which should be published. It supports two formats:
    /// - NodeId syntax using the namespace index (ns) syntax. This is only used in legacy environments and is only supported for backward compatibility.
    /// - List of ExpandedNodeId syntax, to allow putting nodes with similar publishing and/or sampling intervals in one object
    /// </summary>
    public class PublisherConfigurationFileEntryLegacyModel
    {
        /// <summary>
        /// Ctor of the object.
        /// </summary>
        public PublisherConfigurationFileEntryLegacyModel()
        {
        }

        /// <summary>
        /// Ctor of the object.
        /// </summary>
        public PublisherConfigurationFileEntryLegacyModel(string nodeId, string endpointUrl)
        {
            NodeId = nodeId;
            EndpointUrl = new Uri(endpointUrl);
        }

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        public Uri EndpointUrl { get; set; }

        /// <summary>
        /// Flag if a secure transport should be used to connect to the endpoint.
        /// </summary>
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }

        // Optional - since 2.6

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityProfileUri { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityMode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }

        /// <summary>
        /// The node to monitor in "ns=" syntax. This key is only supported for backward compatibility and should not be used anymore.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string NodeId { get; set; }

        /// <summary>
        /// Gets ot sets the authentication mode to authenticate against the OPC UA Server.
        /// </summary>
        [DefaultValue(OpcAuthenticationMode.Anonymous)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// Gets or sets the encrypted username to authenticate against the OPC UA Server (when OpcAuthenticationMode is set to UsernamePassword
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string EncryptedAuthUsername
        {
            get
            {
                return EncryptedAuthCredential?.UserName;
            }
            set
            {
                if (EncryptedAuthCredential == null)
                {
                    EncryptedAuthCredential = new EncryptedNetworkCredential();
                }

                EncryptedAuthCredential.UserName = value;
            }
        }

        /// <summary>
        /// Gets or sets the encrypted password to authenticate against the OPC UA Server (when OpcAuthenticationMode is set to UsernamePassword
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string EncryptedAuthPassword
        {
            get
            {
                return EncryptedAuthCredential?.Password;
            }
            set
            {
                if (EncryptedAuthCredential == null)
                {
                    EncryptedAuthCredential = new EncryptedNetworkCredential();
                }

                EncryptedAuthCredential.Password = value;
            }
        }

        /// <summary>
        /// Gets or sets the encrypted auth credential when OpcAuthenticationMode is set to UsernamePassword.
        /// </summary>
        [JsonIgnore]
        public EncryptedNetworkCredential EncryptedAuthCredential { get; set; }

        /// <summary>
        /// Instead all nodes should be defined in this collection.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<OpcNodeOnEndpointModel> OpcNodes { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
