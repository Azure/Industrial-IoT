// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Info about a published nodes
    /// </summary>
    public class PublishedNodeModel {

        /// <summary>
        /// Node
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Enabled or disabled 
        /// </summary>
        public bool Enabled { get; set; }
    }
}
