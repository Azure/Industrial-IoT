// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    public class PublishNodesRequestApiModel {

        /// <summary> The Group the stream belongs to - DataSetWriterGroup. </summary>
        public string DataSetWriterGroup { get; set; }

        /// <summary> Id Identifier of the DataFlow - DataSetWriterId. </summary>
        public string DataSetWriterId { get; set; }

        /// <summary> The Publishing interval for a dataset writer </summary>
        public int? DataSetPublishingInterval { get; set; }

        /// <summary> Endpoint URL for the OPC Nodes to monitor </summary>
        public string EndpointUrl { get; set; }

        /// <summary> Use a secured channel for the opc ua communication </summary>
        public bool UseSecurity { get; set; }

        /// <summary> endpoint authentication mode </summary>
        public AuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary> Endpoint's username </summary>
        public string UserName { get; set; }

        /// <summary> endpoint password </summary>
        public string Password { get; set; }

        /// <summary> List of the OpcNodes to be monitored </summary>
        public List<PublishedNodeApiModel> OpcNodes { get; set; }
    }
}
