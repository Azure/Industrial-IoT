// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models
{
    /// <summary>
    /// Endpoint security info
    /// </summary>
    public class SecurityEventPayloadModel
    {
        /// <summary>
        /// Connfiguration name
        /// </summary>
        public string ConfigurationName { get; set; } = "EndpointSecurity";

        /// <summary>
        /// Error type
        /// </summary>
        public string ErrorType { get; set; } = "NotOptimal";

        /// <summary>
        /// Used confguration 
        /// </summary>
        public string UsedConfiguration { get; set; } = "SecurityMode: None, SecurityProfile: None";

        /// <summary>
        /// Message 
        /// </summary>
        public string Message { get; set; } 

        /// <summary>
        /// Extra details
        /// </summary>
        public Dictionary<string, string> ExtraDetails { get; set; }
    }
}
