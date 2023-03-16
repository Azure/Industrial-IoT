// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method metadata model
    /// </summary>
    [DataContract]
    public record class MethodMetadataModel
    {
        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        [DataMember(Name = "objectId", Order = 0,
            EmitDefaultValue = false)]
        public string? ObjectId { get; set; }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        [DataMember(Name = "inputArguments", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<MethodMetadataArgumentModel>? InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [DataMember(Name = "outputArguments", Order = 2,
            EmitDefaultValue = false)]
        public IReadOnlyList<MethodMetadataArgumentModel>? OutputArguments { get; set; }
    }
}
