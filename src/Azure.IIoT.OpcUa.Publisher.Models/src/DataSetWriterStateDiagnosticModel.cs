// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for writer diagnostic info.
    /// </summary>
    [DataContract]
    public record class DataSetWriterStateDiagnosticModel
    {
        /// <summary>
        /// Dataset writer identifier.
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public required string Id { get; set; }

        /// <summary>
        /// Dataset writer name.
        /// </summary>
        [DataMember(Name = "dataSetWriterName", Order = 5)]
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// Diagnostics for the source of the dataset
        /// </summary>
        [DataMember(Name = "source", Order = 3,
            EmitDefaultValue = true)]
        public PublishedDataSetSourceDiagnosticModel? Source { get; set; }
    }
}
