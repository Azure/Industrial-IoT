// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;


    /// <summary>
    /// PublishNodes direct method response
    /// </summary>
    public class PublishedNodesResponseApiModel {

        /// <summary>
        /// Endpoint URL for the OPC Nodes to monitor
        /// </summary>
        public List<string> StatusMessage { get; set; }

    }
}
