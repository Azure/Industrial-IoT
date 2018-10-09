// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    /// <summary>
    /// Twin query
    /// </summary>
    public class TwinRegistrationQueryModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// User name in authentication
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Certificate of the endpoint
        /// </summary>
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Whether the twin is activated
        /// </summary>
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether the twin is connected
        /// </summary>
        public bool? Connected { get; set; }

        /// <summary>
        /// Whether to include twins that were soft deleted
        /// </summary>
        public bool? IncludeNotSeenSince { get; set; }
    }
}

