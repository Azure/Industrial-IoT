// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System;

    /// <summary>
    /// Deletes data at times
    /// </summary>
    public class DeleteValuesAtTimesDetailsModel {

        /// <summary>
        /// The timestamps to delete
        /// </summary>
        public DateTime[] ReqTimes { get; set; }
    }
}
