// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery cancel request
    /// </summary>
    [DataContract]
    public sealed record class DiscoveryCancelRequestModel
    {
        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 1,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }

        /// <summary>
        /// Cancel the discovery request with the specified
        /// identifier on this publisher when the discovery
        /// request originally was scoped to a single publisher.
        /// </summary>
        [DataMember(Name = "discovererId", Order = 2,
            EmitDefaultValue = false)]
        public string? DiscovererId { get; set; }
    }
}
