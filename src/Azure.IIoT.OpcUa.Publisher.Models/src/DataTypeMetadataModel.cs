// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Data type metadata model
    /// </summary>
    [DataContract]
    public sealed record class DataTypeMetadataModel
    {
        /// <summary>
        /// The data type for the instance declaration.
        /// </summary>
        [DataMember(Name = "dataType", Order = 0,
            EmitDefaultValue = false)]
        public string? DataType { get; set; }
    }
}
