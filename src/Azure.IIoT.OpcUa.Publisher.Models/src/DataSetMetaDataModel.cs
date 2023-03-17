// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Metadata for the published dataset
    /// </summary>
    [DataContract]
    public sealed record class DataSetMetaDataModel
    {
        /// <summary>
        /// Name of the dataset
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public string? Name { get; set; }

        /// <summary>
        /// Dataset class id
        /// </summary>
        [DataMember(Name = "dataSetClassId", Order = 3,
            EmitDefaultValue = false)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// Description of the dataset
        /// </summary>
        [DataMember(Name = "description", Order = 4,
            EmitDefaultValue = false)]
        public string? Description { get; set; }
    }
}
