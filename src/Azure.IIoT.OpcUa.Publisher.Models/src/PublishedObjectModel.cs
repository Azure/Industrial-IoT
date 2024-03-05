// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published data set container (Publisher extension)
    /// </summary>
    public sealed record class PublishedObjectModel
    {
        /// <summary>
        /// Unique Identifier of event in the dataset.
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Data set name
        /// </summary>
        [DataMember(Name = "name", Order = 1,
            EmitDefaultValue = false)]
        public string? Name { get; set; }

        /// <summary>
        /// Node id of the object
        /// </summary>
        [DataMember(Name = "publishedNodeId", Order = 2,
            EmitDefaultValue = false)]
        public string? PublishedNodeId { get; set; }

        /// <summary>
        /// Browse path to node
        /// </summary>
        [DataMember(Name = "browsePath", Order = 3,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; set; }

        /// <summary>
        /// Variable template
        /// </summary>
        [DataMember(Name = "template", Order = 4,
            EmitDefaultValue = false)]
        public required PublishedDataSetVariableModel Template { get; init; }

        /// <summary>
        /// Published variables in the container
        /// </summary>
        [DataMember(Name = "publishedVariables", Order = 5,
            EmitDefaultValue = false)]
        public PublishedDataItemsModel? PublishedVariables { get; set; }

        /// <summary>
        /// Node flags
        /// </summary>
        [DataMember(Name = "flags", Order = 6,
            EmitDefaultValue = false)]
        public PublishedNodeExpansion Flags { get; set; }

        /// <summary>
        /// Sets the current error state
        /// </summary>
        [DataMember(Name = "state", Order = 10,
            EmitDefaultValue = false)]
        public ServiceResultModel? State { get; set; }
    }
}
