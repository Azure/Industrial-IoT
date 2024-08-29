// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Query compiler request model
    /// </summary>
    [DataContract]
    public record class QueryCompilationRequestModel
    {
        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 0,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; init; }

        /// <summary>
        /// The query to compile.
        /// </summary>
        [DataMember(Name = "query", Order = 1)]
        [Required]
        public required string Query { get; init; }

        /// <summary>
        /// Query type
        /// </summary>
        [DataMember(Name = "queryType", Order = 2)]
        public QueryType QueryType { get; init; }
    }
}
