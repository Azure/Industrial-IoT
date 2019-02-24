// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System;

    /// <summary>
    /// Read historic values
    /// </summary>
    public class ReadValuesDetailsModel {

        /// <summary>
        /// The start time to read from
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// The end time to read to
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The number of values to read
        /// </summary>
        public uint? NumValues { get; set; }

        /// <summary>
        /// Whether to return the bounds as well.
        /// </summary>
        public bool? ReturnBounds { get; set; }
    }
}
