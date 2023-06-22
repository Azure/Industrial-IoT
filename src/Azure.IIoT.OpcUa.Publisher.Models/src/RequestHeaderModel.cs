// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request header model
    /// </summary>
    [DataContract]
    public sealed record class RequestHeaderModel
    {
        /// <summary>
        /// Optional User Elevation
        /// </summary>
        [DataMember(Name = "elevation", Order = 0,
            EmitDefaultValue = false)]
        public CredentialModel? Elevation { get; set; }

        /// <summary>
        /// Optional list of locales in preference order.
        /// </summary>
        [DataMember(Name = "locales", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? Locales { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [DataMember(Name = "diagnostics", Order = 2,
            EmitDefaultValue = false)]
        public DiagnosticsModel? Diagnostics { get; set; }

        /// <summary>
        /// Optional namespace format to use when serializing
        /// nodes and qualified names in responses.
        /// </summary>
        [DataMember(Name = "namespaceFormat", Order = 3,
            EmitDefaultValue = false)]
        public NamespaceFormat? NamespaceFormat { get; set; }
    }
}
