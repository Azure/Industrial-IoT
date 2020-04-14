// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Server connection model
    /// </summary>
    public class ConnectionModel {

        /// <summary>
        /// Endpoint
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public CredentialModel User { get; set; }

        /// <summary>
        /// Diagnostics
        /// </summary>
        public DiagnosticsModel Diagnostics { get; set; }

        /// <summary>
        /// The operation timeout to create sessions.
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }
    }
}