// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Endpoint to talk to
    /// </summary>
    public class EndpointModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Alternative endpoints that can be used for accessing
        /// the server
        /// </summary>
        public HashSet<string> AlternativeUrls { get; set; }

        /// <summary>
        /// Default user credential to use for all access.
        /// </summary>
        public CredentialModel User { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Certificate to validate against - null = trust all
        /// </summary>
        public byte[] ServerThumbprint { get; set; }

        /// <summary>
        /// Certificate with private key to use to connect to
        /// endpoint - null = create self signed certificate.
        /// </summary>
        public byte[] ClientCertificate { get; set; }
    }
}
