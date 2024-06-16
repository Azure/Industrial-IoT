// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Used to generate various message schemas for a network message
    /// </summary>
    public sealed record class PublishedNetworkMessageSchemaModel
    {
        /// <summary>
        /// Data set messages in the network message
        /// </summary>
        [DataMember(Name = "dataSetMessages", Order = 1,
            EmitDefaultValue = false)]
        public required IReadOnlyList<PublishedDataSetMessageSchemaModel?> DataSetMessages { get; init; }

        /// <summary>
        /// Dataset content encoding flags
        /// </summary>
        [DataMember(Name = "networkMessageContentFlags", Order = 2,
            EmitDefaultValue = false)]
        public required NetworkMessageContentFlags? NetworkMessageContentFlags { get; init; }

        /// <summary>
        /// Optional type name for the message type
        /// </summary>
        [DataMember(Name = "typeName", Order = 4,
            EmitDefaultValue = false)]
        public string? TypeName { get; init; }
    }
}
