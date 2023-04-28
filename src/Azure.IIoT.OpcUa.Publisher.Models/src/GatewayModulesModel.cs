// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway modules
    /// </summary>
    [DataContract]
    public sealed record class GatewayModulesModel
    {
        /// <summary>
        /// Supervisor identity if deployed
        /// </summary>
        [DataMember(Name = "supervisor", Order = 0,
            EmitDefaultValue = false)]
        public SupervisorModel? Supervisor { get; set; }

        /// <summary>
        /// Publisher identity if deployed
        /// </summary>
        [DataMember(Name = "publisher", Order = 1,
            EmitDefaultValue = false)]
        public PublisherModel? Publisher { get; set; }

        /// <summary>
        /// Discoverer identity if deployed
        /// </summary>
        [DataMember(Name = "discoverer", Order = 2,
            EmitDefaultValue = false)]
        public DiscovererModel? Discoverer { get; set; }
    }
}
