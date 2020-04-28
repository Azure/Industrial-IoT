// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Redundancy configuration
    /// </summary>
    [DataContract]
    public class RedundancyConfigApiModel {

        /// <summary>
        /// Number of desired active agents
        /// </summary>
        [DataMember(Name = "desiredActiveAgents", Order = 0,
            EmitDefaultValue = false)]
        public int DesiredActiveAgents { get; set; }

        /// <summary>
        /// Number of passive agents
        /// </summary>
        [DataMember(Name = "desiredPassiveAgents", Order = 1,
            EmitDefaultValue = false)]
        public int DesiredPassiveAgents { get; set; }
    }
}