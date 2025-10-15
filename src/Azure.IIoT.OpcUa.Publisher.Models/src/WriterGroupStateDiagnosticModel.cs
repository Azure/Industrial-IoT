// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for writer group status diagnostics.
    /// </summary>
    [DataContract]
    public record class WriterGroupStateDiagnosticModel
    {
        /// <summary>
        /// Writer group identifier.
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public required string Id { get; init; }

        /// <summary>
        /// Writer group name.
        /// </summary>
        [DataMember(Name = "writerGroupName", Order = 1)]
        public string? WriterGroupName { get; init; }

        /// <summary>
        /// Writer group version
        /// </summary>
        [DataMember(Name = "version", Order = 2)]
        public uint Version { get; set; }

        /// <summary>
        /// Diagnostics for the dataset writers
        /// </summary>
        [DataMember(Name = "dataSetWriters", Order = 3,
            EmitDefaultValue = true)]
        public required IReadOnlyList<DataSetWriterStateDiagnosticModel> DataSetWriters { get; init; }
    }
}
