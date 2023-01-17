// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;

    /// <summary>
    /// Static class that contains the default keys for the standalone command line arguments how they
    /// will be represented in the IConfiguration-instance.
    /// </summary>
    public static class StandaloneCliConfigKeys {
        /// <summary>
        /// Key for default published nodes file.
        /// </summary>
        public const string DefaultPublishedNodesFilename = "publishednodes.json";

        /// <summary>
        /// Key for default published nodes schema file.
        /// </summary>
        public const string DefaultPublishedNodesSchemaFilename = "";

        /// <summary>
        /// Key for the publisher site.
        /// </summary>
        public const string PublisherSite = "site";

        /// <summary>
        /// Key for the specified published nodes filename.
        /// </summary>
        public const string PublishedNodesConfigurationFilename = "PublishedNodesFile";

        /// <summary>
        /// Key for the specified published nodes schema filename.
        /// </summary>
        public const string PublishedNodesConfigurationSchemaFilename = "PublishedNodesSchemaFile";

        /// <summary>
        /// Key for the default heartbeat interval in seconds.
        /// </summary>
        public const string HeartbeatIntervalDefault = "DefaultHeartbeatInterval";

        /// <summary>
        /// Key for the default flag to skip the first notification
        /// </summary>
        public const string SkipFirstDefault = "DefaultSkipFirst";

        /// <summary>
        /// Key for the default flag to discard new items in server queue
        /// </summary>
        public const string DiscardNewDefault = "DiscardNew";

        /// <summary>
        /// Key for the messaging mode
        /// </summary>
        public const string MessagingMode = "MessagingMode";

        /// <summary>
        /// Key for the messaging mode
        /// </summary>
        public const string MessageEncoding = "MessageEncoding";

        /// <summary>
        /// Flag to demand full featured message creation from publisher
        /// </summary>
        public const string FullFeaturedMessage = "FullFeaturedMessage";

        /// <summary>
        /// Flag to force strict encoding for messages
        /// </summary>
        public const string UseStandardsCompliantEncoding = "UseStandardsCompliantEncoding";

        /// <summary>
        /// Key for the default sampling interval in milliseconds.
        /// </summary>
        public const string OpcSamplingInterval = "DefaultSamplingInterval";

        /// <summary>
        /// Key for the default publishing interval in milliseconds.
        /// </summary>
        public const string OpcPublishingInterval = "DefaultPublishingInterval";

        /// <summary>
        /// Key for default keyframe count
        /// </summary>
        public const string DefaultKeyFrameCount = "DefaultKeyFrameCount";

        /// <summary>
        /// Key for default metadata send interval
        /// </summary>
        public const string DefaultMetaDataSendInterval = "DefaultMetaDataSendInterval";

        /// <summary>
        /// Key to disable metadata sending
        /// </summary>
        public const string DisableDataSetMetaData = "DisableDataSetMetaData";

        /// <summary>
        /// Key for the default queue size fro monitored items
        /// </summary>
        public const string DefaultQueueSize = "DefaultQueueSize";

        /// <summary>
        /// Key for the default data change filter for monitored items.
        /// </summary>
        public const string DefaultDataChangeTrigger = "DefaulDataChangeTrigger";

        /// <summary>
        /// Key for the flag whether to fetch the display names of the nodes.
        /// </summary>
        public const string FetchOpcNodeDisplayName = "FetchOpcNodeDisplayName";

        /// <summary>
        /// Key for the diagnostics interval in seconds.
        /// </summary>
        public const string DiagnosticsInterval = "DiagnosticsInterval";

        /// <summary>
        /// Key for the batch size of the batching buffer
        /// </summary>
        public const string BatchSize = "BatchSize";

        /// <summary>
        /// Key for the batch size of the batching buffer
        /// </summary>
        public const string BatchTriggerInterval = "BatchTriggerInterval";

        /// <summary>
        /// Key for the max (IoT Hub D2C)message size
        /// </summary>
        public const string IoTHubMaxMessageSize = "IoTHubMaxMessageSize";

        /// <summary>
        /// Key for the max (IoT Hub D2C) messages
        /// </summary>
        public const string MaxOutgressMessages = "MaxOutgressMessages";

        /// <summary>
        /// Key for configuration flag to determine if a telemetry routing info is enabled.
        /// </summary>
        public const string EnableRoutingInfo = "EnableRoutingInfo";

        /// <summary>
        /// Key for the maximum number of nodes per DataSet/Subscription
        /// </summary>
        public const string MaxNodesPerDataSet = "MaxNodesPerDataSet";

        /// <summary>
        /// Key for the scale test monitored items clones count .
        /// </summary>
        public const string ScaleTestCount = "ScaleTestCount";

        /// <summary>
        /// Key for the time for the logfile to flush to disc in seconds.
        /// </summary>
        public const string LogFileFlushTimeSpanSec = "LogFileFlushTimeSpan";

        /// <summary>
        /// Key for the log file name.
        /// </summary>
        public const string LogFileName = "LogFileName";

        /// <summary>
        /// Key for the Legacy (2.5.x) compatibility mode.
        /// </summary>
        public const string LegacyCompatibility = "LegacyCompatibility";

        /// <summary>
        /// Key for the transport mode to IoT Hub.
        /// </summary>
        public const string HubTransport = ModuleConfig.kTransportKey;

        /// <summary>
        /// Key for the EdgeHub connection string.
        /// </summary>
        public const string EdgeHubConnectionString = ModuleConfig.kEdgeHubConnectionStringKey;

        /// <summary>
        /// Key for bypass cert validation flag.
        /// </summary>
        public const string BypassCertVerificationKey = ModuleConfig.kBypassCertVerificationKey;

        /// <summary>
        /// Key for the Mqtt client connection string.
        /// </summary>
        public const string MqttClientConnectionString = ModuleConfig.kMqttClientConnectionStringKey;

        /// <summary>
        /// Key for the Mqtt client connection string.
        /// </summary>
        public const string TelemetryTopicTemplateKey = ModuleConfig.kTelemetryTopicTemplateKey;

        /// <summary>
        /// Key for enable metrics flag.
        /// </summary>
        public const string EnableMetricsKey = ModuleConfig.kEnableMetricsKey;

        /// <summary>
        /// Key for the operation timeout in milliseconds.
        /// </summary>
        public const string OpcOperationTimeout = TransportQuotaConfig.OperationTimeoutKey;

        /// <summary>
        /// Key for the max string length.
        /// </summary>
        public const string OpcMaxStringLength = TransportQuotaConfig.MaxStringLengthKey;

        /// <summary>
        /// Key for security token lifetime in milliseconds.
        /// </summary>
        public const string SecurityTokenLifetimeKey = TransportQuotaConfig.SecurityTokenLifetimeKey;

        /// <summary>
        /// Key for channel lifetime in milliseconds.
        /// </summary>
        public const string ChannelLifetimeKey = TransportQuotaConfig.ChannelLifetimeKey;

        /// <summary>
        /// Key for max buffer size.
        /// </summary>
        public const string MaxBufferSizeKey = TransportQuotaConfig.MaxBufferSizeKey;

        /// <summary>
        /// Key for max message size.
        /// </summary>
        public const string MaxMessageSizeKey = TransportQuotaConfig.MaxMessageSizeKey;

        /// <summary>
        /// Key for max array length.
        /// </summary>
        public const string MaxArrayLengthKey = TransportQuotaConfig.MaxArrayLengthKey;

        /// <summary>
        /// Key for max byte string length.
        /// </summary>
        public const string MaxByteStringLengthKey = TransportQuotaConfig.MaxByteStringLengthKey;

        /// <summary>
        /// Key for application uri.
        /// </summary>
        public const string ApplicationUriKey = ClientServicesConfig.ApplicationUriKey;

        /// <summary>
        /// Key for product uri.
        /// </summary>
        public const string ProductUriKey = ClientServicesConfig.ProductUriKey;

        /// <summary>
        /// Key for the default OPC Session timeout in seconds - to request from the OPC server at session creation.
        /// </summary>
        public const string OpcSessionCreationTimeout = ClientServicesConfig.DefaultSessionTimeoutKey;

        /// <summary>
        /// Key for minimum subscription lifetime in seconds.
        /// </summary>
        public const string MinSubscriptionLifetimeKey = ClientServicesConfig.MinSubscriptionLifetimeKey;

        /// <summary>
        /// Key for the OPC Keep Alive Interval in seconds.
        /// </summary>
        public const string OpcKeepAliveIntervalInSec = ClientServicesConfig.KeepAliveIntervalKey;

        /// <summary>
        /// Key for the disconnect thresholt for missed keep alive signals.
        /// </summary>
        public const string OpcKeepAliveDisconnectThreshold = ClientServicesConfig.MaxKeepAliveCountKey;

        /// <summary>
        /// Key for the flag to trust own certificate.
        /// </summary>
        public const string TrustMyself = SecurityConfig.AddAppCertToTrustedStoreKey;

        /// <summary>
        /// Key for the flat to auto-accept untrusted certificates.
        /// </summary>
        public const string AutoAcceptCerts = SecurityConfig.AutoAcceptUntrustedCertificatesKey;

        /// <summary>
        /// Key for the Application Certificate store type.
        /// </summary>
        public const string OpcOwnCertStoreType = SecurityConfig.ApplicationCertificateStoreTypeKey;

        /// <summary>
        /// Key for the Aplication Certificate Store path.
        /// </summary>
        public const string OpcOwnCertStorePath = SecurityConfig.ApplicationCertificateStorePathKey;

        /// <summary>
        /// Key for app cert subject name.
        /// </summary>
        public const string OpcApplicationCertificateSubjectName = SecurityConfig.ApplicationCertificateSubjectNameKey;

        /// <summary>
        /// Key for app name.
        /// </summary>
        public const string OpcApplicationName = SecurityConfig.ApplicationNameKey;

        /// <summary>
        /// Key for the trusted peer certificates path.
        /// </summary>
        public const string OpcTrustedCertStorePath = SecurityConfig.TrustedPeerCertificatesPathKey;

        /// <summary>
        /// Key for the rejected certificate store path.
        /// </summary>
        public const string OpcRejectedCertStorePath = SecurityConfig.RejectedCertificateStorePathKey;

        /// <summary>
        /// Key for the trusted issuer certificates.
        /// </summary>
        public const string OpcIssuerCertStorePath = SecurityConfig.TrustedIssuerCertificatesPathKey;

        /// <summary>
        /// Key for PkiRootPath.
        /// </summary>
        public const string PkiRootPathKey = SecurityConfig.PkiRootPathKey;

        /// <summary>
        /// Key for trusted issuer certificates type.
        /// </summary>
        public const string TrustedIssuerCertificatesTypeKey = SecurityConfig.TrustedIssuerCertificatesTypeKey;

        /// <summary>
        /// Key for trusted peer certificates type.
        /// </summary>
        public const string TrustedPeerCertificatesTypeKey = SecurityConfig.TrustedPeerCertificatesTypeKey;

        /// <summary>
        /// Key for rejected certificate store type.
        /// </summary>
        public const string RejectedCertificateStoreTypeKey = SecurityConfig.RejectedCertificateStoreTypeKey;

        /// <summary>
        /// Key for the reject unsecure signatures flag.
        /// </summary>
        public const string RejectSha1SignedCertificatesKey = SecurityConfig.RejectSha1SignedCertificatesKey;

        /// <summary>
        /// Key for minimum certificate size.
        /// </summary>
        public const string MinimumCertificateKeySizeKey = SecurityConfig.MinimumCertificateKeySizeKey;

        /// <summary>
        /// Key for configuring reporting of OPC Publisher restarts.
        /// </summary>
        public const string RuntimeStateReporting = "RuntimeStateReporting";
    }
}
