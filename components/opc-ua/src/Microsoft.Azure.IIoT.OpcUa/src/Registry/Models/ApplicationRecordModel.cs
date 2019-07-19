// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Application with optional list of endpoints
    /// </summary>
    public class ApplicationRecordModel {

        /// <summary>
        /// Record id
        /// </summary>
        public uint RecordId { get; set; }

        /// <summary>
        /// Application information
        /// </summary>
        public ApplicationInfoModel Application { get; set; }
    }
}
