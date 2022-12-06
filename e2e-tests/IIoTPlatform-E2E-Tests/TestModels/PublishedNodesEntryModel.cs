// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {
    using System.Collections.Generic;

    /// <summary>
    /// Model with nodes to publish per Endpoint
    /// </summary>
    public class PublishedNodesEntryModel {

        /// <summary>
        /// Id Identifier of the DataFlow - DataSetWriterId.
        /// </summary>
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// The Group the stream belongs to - DataSetWriterGroup.
        /// </summary>
        public string DataSetWriterGroup { get; set; }

        /// <summary>
        /// The Publishing interval for a dataset writer
        /// </summary>
        public int? DataSetPublishingInterval { get; set; }

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Secure transport should be used to
        /// </summary>
        public bool UseSecurity { get; set; }

        /// <summary>
        /// Nodes defined in the collection.
        /// </summary>
        public OpcUaNodesModel[] OpcNodes { get; set; }

        /// <summary>
        /// plain username
        /// </summary>
        public string EncryptedAuthUsername { get; set; }

        /// <summary>
        /// plain password
        /// </summary>
        public string EncryptedAuthPassword { get; set; }

        /// <summary>
        /// plain username
        /// </summary>
        public string OpcAuthenticationUsername { get; set; }

        /// <summary>
        /// plain password
        /// </summary>
        public string OpcAuthenticationPassword { get; set; }

    }
}
