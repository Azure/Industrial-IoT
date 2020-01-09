// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    /// <summary>
    /// Model that represents the command line arguments in the format of the legacy OPC Publisher.
    /// </summary>
    public class LegacyCliModel {
        /// <summary>
        /// The site of the publisher.
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// The published nodes file.
        /// </summary>
        public string PublishedNodesFile { get; set; }

        /// <summary>
        /// The time to wait to connect a session.
        /// </summary>
        public TimeSpan? SessionConnectWait { get; set; }

        /// <summary>
        /// The default interval for heartbeats if not set on node level.
        /// </summary>
        public TimeSpan? DefaultHeartbeatInterval { get; set; }

        /// <summary>
        /// The default flag whether to skip the first value if not set on node level.
        /// </summary>
        public bool DefaultSkipFirst { get; set; }

        /// <summary>
        /// The default sampling interval.
        /// </summary>
        public TimeSpan? DefaultSamplingInterval { get; set; }

        /// <summary>
        /// The default publishing interval.
        /// </summary>
        public TimeSpan? DefaultPublishingInterval { get; set; }

        /// <summary>
        /// Flag wether to grab the display name of nodes form the OPC UA Server.
        /// </summary>
        public bool FetchOpcNodeDisplayName { get; set; }

        /// <summary>
        /// The interval to show diagnostics information.
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <summary>
        /// The time to flush the log file to the disc.
        /// </summary>
        public TimeSpan? LogFileFlushTimeSpan { get; set; }

        /// <summary>
        /// The filename of the logfile.
        /// </summary>
        public string LogFilename { get; set; }

        /// <summary>
        /// The transport mode.
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
