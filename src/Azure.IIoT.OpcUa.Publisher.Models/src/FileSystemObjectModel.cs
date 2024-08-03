// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// File system object model
    /// </summary>
    [DataContract]
    public record FileSystemObjectModel
    {
        /// <summary>
        /// The node id of the filesystem object
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0,
            EmitDefaultValue = false)]
        public string? NodeId { get; init; }

        /// <summary>
        /// The browse path to the filesystem object
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1,
           EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; init; }

        /// <summary>
        /// The name of the filesystem object
        /// </summary>
        [DataMember(Name = "name", Order = 2,
           EmitDefaultValue = false)]
        public string? Name { get; init; }
    }
}
