// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Result of value read
    /// </summary>
    public class ValueReadResultModel {

        /// <summary>
        /// Value read
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Diagnostics data in case of error
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
