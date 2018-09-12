// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Value read response model for twin module
    /// </summary>
    public class ValueReadResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        ValueReadResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ValueReadResponseApiModel(ValueReadResultModel model) {
            Value = model.Value;
            SourcePicoseconds = model.SourcePicoseconds;
            SourceTimestamp = model.SourceTimestamp;
            ServerPicoseconds = model.ServerPicoseconds;
            ServerTimestamp = model.ServerTimestamp;
            Diagnostics = model.Diagnostics;
        }

        /// <summary>
        /// Value read
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at source.
        /// </summary>
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at source.
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at server.
        /// </summary>
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at server.
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
