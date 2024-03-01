// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Linq;

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

        /// <summary>
        /// Major version
        /// </summary>
        [DataMember(Name = "majorVersion", Order = 5,
            EmitDefaultValue = false)]
        public int MajorVersion { get; set; }

        /// <summary>
        /// The number of items in a subscription for which
        /// loading of metadata should be done inline during
        /// subscription creation (otherwise will be completed
        /// asynchronously). If the number of items in the
        /// subscription is below this value it is guaranteed
        /// that the first notification contains metadata.
        /// </summary>
        [DataMember(Name = "asyncMetaDataLoadThreshold", Order = 6,
            EmitDefaultValue = false)]
        public int? AsyncMetaDataLoadThreshold { get; set; }
    }
}
