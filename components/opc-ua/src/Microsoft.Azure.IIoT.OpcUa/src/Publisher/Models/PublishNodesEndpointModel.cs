// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Contains Endpoint info for diagnostic
    /// </summary>
    public class PublishNodesEndpointModel {

        /// <summary>
        /// The Group the stream belongs to - DataSetWriterGroup.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string DataSetWriterGroup { get; set; }

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public Uri EndpointUrl { get; set; }

        /// <summary>
        /// Secure transport should be used to
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public bool UseSecurity { get; set; }

        /// <summary>
        /// authentication mode
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// plain username
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string OpcAuthenticationUsername { get; set; }
    }
}
