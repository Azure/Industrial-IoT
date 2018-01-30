// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models {
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Endpoint to talk to
    /// </summary>
    public class ServerEndpointModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        public object Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        public bool? IsTrusted { get; set; }

        /// <summary>
        /// Returns the public certificate presented by the server
        /// </summary>
        public X509Certificate2 ServerCertificate { get; set; }

        /// <summary>
        /// Returns the public certificate to present to the server.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// Edge controller device to use - if not set, uses
        /// proxy to access.
        /// </summary>
        public string EdgeController { get; set; }
    }
}
