using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    public static class LegacyCliConfigKeys {
        public const string DefaultPublishedNodesFilename = "publishednodes.json";
        public const string PublisherSite = "Site";
        public const string PublisherNodeConfigurationFilename = "PublishedNodesFile";
        public const string SessionConnectWaitSec = "SessionConnectWait";
        public const string HeartbeatIntervalDefault = "DefaultHeartbeatInterval";
        public const string SkipFirstDefault = "DefaultSkipFirst";
        public const string OpcSamplingInterval = "DefaultSamplingInterval";
        public const string OpcPublishingInterval = "DefaultPublishingInterval";
        public const string FetchOpcNodeDisplayName = "FetchOpcNodeDisplayName";
        public const string DiagnosticsInterval = "DiagnosticsInterval";
        public const string LogFileFlushTimeSpanSec = "LogFileFlushTimeSpan";
        public const string LogFileName = "LogFileName";
        public const string HubTransport = ModuleConfig.TransportKey;
        public const string EdgeHubConnectionString = ModuleConfig.EdgeHubConnectionStringKey;
        public const string OpcOperationTimeout = TransportQuotaConfig.OperationTimeoutKey;
        public const string OpcMaxStringLength = TransportQuotaConfig.MaxStringLengthKey;
        public const string OpcSessionCreationTimeout = ClientServicesConfig.DefaultSessionTimeoutKey;
        public const string OpcKeepAliveIntervalInSec = ClientServicesConfig.KeepAliveIntervalKey;
        public const string OpcKeepAliveDisconnectThreshold = ClientServicesConfig.MaxKeepAliveCountKey;
        public const string TrustMyself = "TrustSelf";
        public const string AutoAcceptCerts = SecurityConfig.AutoAcceptUntrustedCertificatesKey;
        public const string OpcOwnCertStoreType = SecurityConfig.ApplicationCertificateStoreTypeKey;
        public const string OpcOwnCertStorePath = SecurityConfig.ApplicationCertificateStorePathKey;
        public const string OpcTrustedCertStorePath = SecurityConfig.TrustedPeerCertificatesPathKey;
        public const string OpcRejectedCertStorePath = SecurityConfig.RejectedCertificateStorePathKey;
        public const string OpcIssuerCertStorePath = SecurityConfig.TrustedIssuerCertificatesPathKey;
    }
}
