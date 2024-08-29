// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Used to generate various message schemas for a data set message
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetMessageSchemaModel
    {
        /// <summary>
        /// Metadata describing the data set message content
        /// </summary>
        [DataMember(Name = "metaData", Order = 1,
            EmitDefaultValue = false)]
        public required PublishedDataSetMetaDataModel MetaData { get; init; }

        /// <summary>
        /// Dataset content encoding flags
        /// </summary>
        [DataMember(Name = "dataSetMessageContentFlags", Order = 2,
            EmitDefaultValue = false)]
        public required DataSetMessageContentFlags? DataSetMessageContentFlags { get; init; }

        /// <summary>
        /// Dataset field encoding flags
        /// </summary>
        [DataMember(Name = "dataSetFieldContentFlags", Order = 3,
            EmitDefaultValue = false)]
        public required DataSetFieldContentFlags? DataSetFieldContentFlags { get; init; }

        /// <summary>
        /// Optional type name for the message type
        /// </summary>
        [DataMember(Name = "typeName", Order = 4,
            EmitDefaultValue = false)]
        public string? TypeName { get; init; }
    }
}
