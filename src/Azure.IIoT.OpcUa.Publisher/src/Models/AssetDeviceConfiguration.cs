// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data point additional configuration model
    /// </summary>
    [DataContract]
    public sealed record class DataPointModel : OpcNodeModel
    {
        /// <summary>
        /// Adapt the sampling interval property
        /// </summary>
        [DataMember(Name = "samplingInterval", Order = 100,
            EmitDefaultValue = false)]
        public int? SamplingInterval
        {
            get => OpcSamplingInterval;
            set => OpcSamplingInterval = value;
        }

        /// <summary>
        /// Adapt the publishing interval property
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 101,
            EmitDefaultValue = false)]
        public int? PublishingInterval
        {
            get => OpcPublishingInterval;
            set => OpcPublishingInterval = value;
        }
    }

    /// <summary>
    /// Dataset reource additional configuration
    /// model.
    /// </summary>
    [DataContract]
    public sealed record class DataSetModel : PublishedNodesEntryModel
    {
        /// <summary>
        /// Adapt the sampling interval property
        /// </summary>
        [DataMember(Name = "samplingInterval", Order = 100,
            EmitDefaultValue = false)]
        public int? SamplingInterval
        {
            get => DataSetSamplingInterval;
            set => DataSetSamplingInterval = value;
        }

        /// <summary>
        /// Adapt the publishing interval property
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 101,
            EmitDefaultValue = false)]
        public int? PublishingInterval
        {
            get => DataSetPublishingInterval;
            set => DataSetPublishingInterval = value;
        }

        /// <summary>
        /// Adapt the key frame count property
        /// </summary>
        [DataMember(Name = "keyFrameCount", Order = 102,
            EmitDefaultValue = false)]
        public uint? KeyFrameCount
        {
            get => DataSetKeyFrameCount;
            set => DataSetKeyFrameCount = value;
        }

        /// <summary>
        /// Adapt the start instance property
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 103,
            EmitDefaultValue = false)]
        public string? StartInstance { get; set; }
    }

    /// <summary>
    /// Event reource additional configuration
    /// model.
    /// </summary>
    [DataContract]
    public sealed record class EventModel : PublishedNodesEntryModel
    {
        /// <summary>
        /// Adapt the sampling interval property
        /// </summary>
        [DataMember(Name = "samplingInterval", Order = 100,
            EmitDefaultValue = false)]
        public int? SamplingInterval
        {
            get => DataSetSamplingInterval;
            set => DataSetSamplingInterval = value;
        }

        /// <summary>
        /// Adapt the publishing interval property
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 101,
            EmitDefaultValue = false)]
        public int? PublishingInterval
        {
            get => DataSetPublishingInterval;
            set => DataSetPublishingInterval = value;
        }

        /// <summary>
        /// Adapt the key frame count property
        /// </summary>
        [DataMember(Name = "keyFrameCount", Order = 102,
            EmitDefaultValue = false)]
        public uint? KeyFrameCount
        {
            get => DataSetKeyFrameCount;
            set => DataSetKeyFrameCount = value;
        }

        /// <summary>
        /// Size of the server-side queue for this monitored item.
        /// Controls how many values can be buffered during slow
        /// connections. Values are discarded according to DiscardNew
        /// when queue is full. Default is 1 unless otherwise
        /// configured. Larger queues help prevent data loss but use
        /// more server memory.
        /// </summary>
        [DataMember(Name = "QueueSize", Order = 104,
            EmitDefaultValue = false)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Controls queue overflow behavior for monitored items.
        /// True: Discard newest values when queue is full (LIFO).
        /// False: Discard oldest values when queue is full (FIFO,
        /// default). Use True to preserve historical data during
        /// connection issues. Use False to maintain current value
        /// accuracy.
        /// </summary>
        [DataMember(Name = "DiscardNew", Order = 105,
            EmitDefaultValue = false)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Event Filter to apply. When specified the node is assmed
        /// to be an event notifier node to subscribe to.
        /// </summary>
        [DataMember(Name = "EventFilter", Order = 106,
            EmitDefaultValue = false)]
        public EventFilterModel? EventFilter { get; set; }

        /// <summary>
        /// Settings for pending condition handling
        /// </summary>
        [DataMember(Name = "ConditionHandling", Order = 107,
            EmitDefaultValue = false)]
        public ConditionHandlingOptionsModel? ConditionHandling { get; set; }

        /// <summary>
        /// Controls handling of initial value notification. True:
        /// Suppress first value from monitored item. False: Publish
        /// initial value (default). Useful when only changes are
        /// relevant. Server always sends initial value on creation.
        /// </summary>
        [DataMember(Name = "SkipFirst", Order = 108,
            EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }
    }

    /// <summary>
    /// Endpoint additional configuration for devices. Property names are
    /// the same as in <see cref="PublishedNodesEntryModel"/> but this
    /// configuration is used when parsing device endpoint configuration
    /// from a ADR device resource.
    /// </summary>
    [DataContract]
    public sealed record class DeviceEndpointModel
    {
        /// <summary>
        /// The specific security mode to use for the specified endpoint.
        /// If the security mode is not available with any configured
        /// security policy connectivity will fail. Default:
        /// <see cref="SecurityMode.NotNone"/>
        /// </summary>
        [DataMember(Name = "EndpointSecurityMode", Order = 0,
            EmitDefaultValue = false)]
        public SecurityMode? EndpointSecurityMode { get; set; }

        /// <summary>
        /// The security policy URI to use for the endpoint connection.
        /// Overrides UseSecurity setting and refines
        /// EndpointSecurityMode choice. Examples include
        /// "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256".
        /// If the specified policy is not available with the chosen
        /// security mode, connectivity will fail. This allows enforcing
        /// specific security requirements.
        /// </summary>
        [DataMember(Name = "EndpointSecurityPolicy", Order = 1,
            EmitDefaultValue = false)]
        public string? EndpointSecurityPolicy { get; set; }

        /// <summary>
        /// Use reverse connect to connect ot the endpoint
        /// </summary>
        [DataMember(Name = "UseReverseConnect", Order = 2,
            EmitDefaultValue = false)]
        public bool? UseReverseConnect { get; set; }

        /// <summary>
        /// Specifies the authentication mode for connecting to the OPC
        /// UA server. Supported modes:
        /// - Anonymous: No authentication (default)
        /// - UsernamePassword: Username and password authentication
        /// - Certificate: Certificate-based authentication using X.509
        ///   certificates
        /// When using credentials or certificates, encrypted
        /// communication should be enabled via UseSecurity or
        /// EndpointSecurityMode to protect secrets. For certificate
        /// auth, the certificate must be in the User certificate
        /// store.
        /// </summary>
        [DataMember(Name = "OpcAuthenticationMode", Order = 4)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// The plaintext username for UsernamePassword authentication,
        /// or the subject name of the certificate for Certificate
        /// authentication. When using Certificate mode, this refers to
        /// a certificate in the User certificate store of the PKI
        /// configuration.
        /// </summary>
        [DataMember(Name = "OpcAuthenticationUsername", Order = 5,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationUsername { get; set; }

        /// <summary>
        /// The plaintext password for UsernamePassword authentication,
        /// or the password protecting the private key for Certificate
        /// authentication. For Certificate mode, this must match the
        /// password used when adding the certificate to the PKI store.
        /// </summary>
        [DataMember(Name = "OpcAuthenticationPassword", Order = 6,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationPassword { get; set; }

        /// <summary>
        /// Enables detailed server diagnostics logging for the
        /// connection. When enabled, provides additional diagnostic
        /// information useful for troubleshooting connectivity,
        /// authentication, and subscription issues. The diagnostics
        /// data is included in the publisher's logs. Default: false
        /// </summary>
        [DataMember(Name = "DumpConnectionDiagnostics", Order = 10,
            EmitDefaultValue = false)]
        public bool? DumpConnectionDiagnostics { get; set; }

        /// <summary>
        /// Runs asset discovery on the endpoint
        /// </summary>
        [DataMember(Name = "RunAssetDiscovery", Order = 11,
            EmitDefaultValue = false)]
        public bool? RunAssetDiscovery { get; set; }

        /// <summary>
        /// Finds assets for the selected types.
        /// </summary>
        [DataMember(Name = "AssetTypes", Order = 12,
            EmitDefaultValue = false)]
        public HashSet<string>? AssetTypes { get; set; }

        /// <summary>
        /// Source of this endpoint configuration
        /// </summary>
        [DataMember(Name = "Source", Order = 13,
            EmitDefaultValue = false)]
        public string? Source { get; set; }
    }
}
