// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Session options
    /// </summary>
    public class SessionOptions : OpenOptions
    {
        /// <summary>
        /// Session name to use
        /// </summary>
        public string? SessionName { get; set; }

        /// <summary>
        /// Client certificate to use
        /// </summary>
        public X509Certificate2? ClientCertificate { get; set; }

        /// <summary>
        /// Session timeout
        /// </summary>
        public TimeSpan? SessionTimeout { get; set; }

        /// <summary>
        /// Check domain
        /// </summary>
        public bool CheckDomain { get; set; }

        /// <summary>
        /// Available endpoints
        /// </summary>
        public EndpointDescriptionCollection? AvailableEndpoints { get; set; }

        /// <summary>
        /// Discovery profile uris
        /// </summary>
        public StringCollection? DiscoveryProfileUris { get; set; }

        /// <summary>
        /// Channel to use
        /// </summary>
        public ITransportChannel? Channel { get; set; }

        /// <summary>
        /// Connection to use
        /// </summary>
        public ITransportWaitingConnection? Connection { get; set; }

        /// <summary>
        /// Activity source to use
        /// </summary>
        public ActivitySource? ActivitySource { get; set; }

        /// <summary>
        /// Meter factory to use
        /// </summary>
        public IMeterFactory? Meter { get; set; }
    }
}
