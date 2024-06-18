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
        /// Optional User Elevation. We suggest to use the
        /// connection object to elevate the user instead of
        /// using the request elevation.
        /// </summary>
        [DataMember(Name = "elevation", Order = 0,
            EmitDefaultValue = false)]
        public CredentialModel? Elevation { get; set; }

        /// <summary>
        /// Optional list of preferred locales in preference
        /// order to be used during connecting the session.
        /// We suggest to use the connection object to set
        /// the locales
        /// </summary>
        [DataMember(Name = "locales", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? Locales { get; set; }

        /// <summary>
        /// Optional diagnostics configuration for the
        /// service call. This configures the returned
        /// diagnostic information in the result.
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
        /// Operation timeout in ms. This applies to every
        /// operation that is invoked, not to the entire
        /// transaction and overrides the configured operation
        /// timeout.
        /// </summary>
        [DataMember(Name = "operationTimeout", Order = 4,
            EmitDefaultValue = false)]
        public int? OperationTimeout { get; set; }

        /// <summary>
        /// Service call timeout in ms. As opposed to the
        /// operation timeout this terminates the entire
        /// transaction if it takes longer than the timeout to
        /// complete. Note that a connect and reconnect during
        /// the service call is gated by the connect timeout
        /// setting. If a connect timeout is not specified
        /// this timeout is used also for connect timeout.
        /// </summary>
        [DataMember(Name = "serviceCallTimeout", Order = 5,
            EmitDefaultValue = false)]
        public int? ServiceCallTimeout { get; set; }

        /// <summary>
        /// Connect timeout in ms. As opposed to the service call
        /// timeout this terminates the entire transaction if
        /// it takes longer than the timeout to connect a session
        /// A connect and reconnect during the service call
        /// resets the timeout therefore the overall time for
        /// the call to complete can be longer than specified.
        /// </summary>
        [DataMember(Name = "connectTimeout", Order = 6,
            EmitDefaultValue = false)]
        public int? ConnectTimeout { get; set; }
    }
}
