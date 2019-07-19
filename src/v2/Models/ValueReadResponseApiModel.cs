// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Value read response model for module
    /// </summary>
    public class ValueReadResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ValueReadResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ValueReadResponseApiModel(ValueReadResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Value = model.Value;
            DataType = model.DataType;
            SourcePicoseconds = model.SourcePicoseconds;
            SourceTimestamp = model.SourceTimestamp;
            ServerPicoseconds = model.ServerPicoseconds;
            ServerTimestamp = model.ServerTimestamp;
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// Value read
        /// </summary>
        [JsonProperty(PropertyName = "Value",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary>
        /// Built in data type of the value read.
        /// </summary>
        [JsonProperty(PropertyName = "DataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at source.
        /// </summary>
        [JsonProperty(PropertyName = "SourcePicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at source.
        /// </summary>
        [JsonProperty(PropertyName = "SourceTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at server.
        /// </summary>
        [JsonProperty(PropertyName = "ServerPicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at server.
        /// </summary>
        [JsonProperty(PropertyName = "ServerTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "ErrorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
