// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Result of value read
    /// </summary>
    public class ValueReadResultModel {

        /// <summary>
        /// Value read
        /// </summary>
        public string Value { get; set; }

        public ushort? SourcePicoseconds { get; set; }

        public DateTime? SourceTimestamp { get; set; }

        public ushort? ServerPicoseconds { get; set; }

        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Diagnostics data in case of error
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
