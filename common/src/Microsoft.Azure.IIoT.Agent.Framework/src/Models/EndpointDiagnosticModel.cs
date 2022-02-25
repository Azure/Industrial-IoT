// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;

    /// <summary>
    /// Contains Endpoint info for diagnostic
    /// </summary>
    public class EndpointDiagnosticModel {

        /// <summary>
        /// The Group the stream belongs to - DataSetWriterGroup.
        /// </summary>
        public string DataSetWriterGroup { get; set; }

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        public Uri EndpointUrl { get; set; }

        /// <summary>
        /// Secure transport should be used to
        /// </summary>
        public bool UseSecurity { get; set; }

        /// <summary>
        /// authentication mode
        /// </summary>
        public AuthMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// plain username
        /// </summary>
        public string OpcAuthenticationUsername { get; set; }
    }
}
