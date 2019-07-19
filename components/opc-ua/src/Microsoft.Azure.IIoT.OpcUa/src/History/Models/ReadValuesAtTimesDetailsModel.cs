// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System;

    /// <summary>
    /// Read data at specified times
    /// </summary>
    public class ReadValuesAtTimesDetailsModel {

        /// <summary>
        /// Requested datums
        /// </summary>
        public DateTime[] ReqTimes { get; set; }

        /// <summary>
        /// Whether to use simple bounds
        /// </summary>
        public bool? UseSimpleBounds { get; set; }
    }
}
