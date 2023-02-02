// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Server connection model
    /// </summary>
    public class ConnectionModel {

        /// <summary>
        /// Group Id of the data set associated to the connection that the stram belongs to - DataSetWriterGroup
        /// </summary>
        public string Group { get; set; }

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
    }
}
