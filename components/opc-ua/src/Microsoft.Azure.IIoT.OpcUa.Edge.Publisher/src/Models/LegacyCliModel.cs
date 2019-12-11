using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    public class LegacyCliModel {
        /// <summary>
        /// 
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PublishedNodesFile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? SessionConnectWait { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? DefaultHeartbeatInterval { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool DefaultSkipFirst { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? DefaultSamplingInterval { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? DefaultPublishingInterval { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool FetchOpcNodeDisplayName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? LogFileFlushTimeSpan { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string LogFilename { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Transport { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string EdgeHubConnectionString { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long? MaxStringLength { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? SessionCreationTimeout { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool TrustSelf { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool AutoAcceptUntrustedCertificates { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ApplicationCertificateStoreType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ApplicationCertificateStorePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TrustedPeerCertificatesPath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string RejectedCertificateStorePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TrustedIssuerCertificatesPath { get; set; }
    }
}
