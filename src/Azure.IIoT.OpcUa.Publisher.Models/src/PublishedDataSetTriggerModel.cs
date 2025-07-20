// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Triggered items.
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetTriggerModel
    {
        /// <summary>
        /// Either published data variables
        /// </summary>
        [DataMember(Name = "publishedVariables", Order = 0,
            EmitDefaultValue = false)]
        public PublishedDataItemsModel? PublishedVariables { get; set; }

        /// <summary>
        /// Or published events triggering
        /// </summary>
        [DataMember(Name = "publishedEvents", Order = 1,
            EmitDefaultValue = false)]
        public PublishedEventItemsModel? PublishedEvents { get; set; }

        /// <summary>
        /// Or published calls
        /// </summary>
        [DataMember(Name = "publishedMethods", Order = 2,
            EmitDefaultValue = false)]
        public PublishedMethodItemsModel? PublishedMethods { get; set; }
    }
}
