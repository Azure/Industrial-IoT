// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Service result
    /// </summary>
    [DataContract]
    public sealed record class ServiceResultModel
    {
        /// <summary>
        /// Error code - if null operation succeeded.
        /// </summary>
        [DataMember(Name = "statusCode", Order = 0,
            EmitDefaultValue = false)]
        public uint StatusCode { get; set; }

        /// <summary>
        /// Error message in case of error or null.
        /// </summary>
        [DataMember(Name = "errorMessage", Order = 1,
            EmitDefaultValue = false)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Symbolic identifier
        /// </summary>
        [DataMember(Name = "symbolicId", Order = 2)]
        public string? SymbolicId { get; set; }

        /// <summary>
        /// Locale of the error message
        /// </summary>
        [DataMember(Name = "locale", Order = 3,
            EmitDefaultValue = false)]
        public string? Locale { get; set; }

        /// <summary>
        /// Additional information if available
        /// </summary>
        [DataMember(Name = "additionalInfo", Order = 4,
            EmitDefaultValue = false)]
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Namespace uri
        /// </summary>
        [DataMember(Name = "namespaceUri", Order = 5,
            EmitDefaultValue = false)]
        public string? NamespaceUri { get; set; }

        /// <summary>
        /// Inner result if any
        /// </summary>
        [DataMember(Name = "inner", Order = 6,
            EmitDefaultValue = false)]
        public ServiceResultModel? Inner { get; set; }
    }
}
