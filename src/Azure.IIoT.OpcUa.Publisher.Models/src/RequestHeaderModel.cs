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
        /// Optional list of preferred locales in preference order.
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

        /// <summary>
        /// Operation timeout. This applies to every operation
        /// that is invoked, not to the entire transaction and
        /// overrides the configured operation timeout.
        /// </summary>
        [DataMember(Name = "operationTimeout", Order = 4,
            EmitDefaultValue = false)]
        public int? OperationTimeout { get; set; }

        /// <summary>
        /// Service call timeout. As opposed to the operation
        /// timeout this terminates the entire transaction if
        /// it takes longer than the timeout to complete. Note
        /// that a connect and reconnect during the service call
        /// resets the timeout therefore the overall time for
        /// the call to complete can be longer than specified.
        /// </summary>
        [DataMember(Name = "serviceCallTimeout", Order = 5,
            EmitDefaultValue = false)]
        public int? ServiceCallTimeout { get; set; }
    }
}
