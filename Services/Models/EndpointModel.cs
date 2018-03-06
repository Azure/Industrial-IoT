// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Endpoint to talk to
    /// </summary>
    public class EndpointModel {

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
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        public bool? IsTrusted { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Target device to use for command control
        /// </summary>
        public string TwinId { get; set; }

        /// <summary>
        /// Edge supervisor that owns this endpoint
        /// </summary>
        public string SupervisorId { get; set; }
    }
}
