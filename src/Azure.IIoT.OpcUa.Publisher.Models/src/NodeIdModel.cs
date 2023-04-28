// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Node id serialized as object
    /// </summary>
    [DataContract]
    public sealed record class NodeIdModel
    {
        /// <summary>
        /// Identifier
        /// </summary>
        [DataMember(Name = "Identifier", Order = 0,
            EmitDefaultValue = false)]
        public string? Identifier { get; set; }
    }
}
