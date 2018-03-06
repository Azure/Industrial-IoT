// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Request value write
    /// </summary>
    public class ValueWriteRequestModel {

        /// <summary>
        /// Node information to allow writing - from browse.
        /// </summary>
        public NodeModel Node { get; set; }

        /// <summary>
        /// Value to write
        /// </summary>
        public string Value { get; set; }
    }
}
