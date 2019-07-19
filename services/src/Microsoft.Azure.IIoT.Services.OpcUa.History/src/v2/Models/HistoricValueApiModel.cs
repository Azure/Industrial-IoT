// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Historic data
    /// </summary>
    public class HistoricValueApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoricValueApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoricValueApiModel(HistoricValueModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Value = model.Value;
            StatusCode = model.StatusCode;
            SourceTimestamp = model.SourceTimestamp;
            SourcePicoseconds = model.SourcePicoseconds;
            ServerTimestamp = model.ServerTimestamp;
            ServerPicoseconds = model.ServerPicoseconds;
            ModificationInfo = model.ModificationInfo == null ? null :
                new ModificationInfoApiModel(model.ModificationInfo);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public HistoricValueModel ToServiceModel() {
            return new HistoricValueModel {
                Value = Value,
                StatusCode = StatusCode,
                SourceTimestamp = SourceTimestamp,
                SourcePicoseconds = SourcePicoseconds,
                ServerTimestamp = ServerTimestamp,
                ServerPicoseconds = ServerPicoseconds,
                ModificationInfo = ModificationInfo?.ToServiceModel()
            };
        }

        /// <summary>,
        /// The value of data value.
        /// </summary>
        [JsonProperty(PropertyName = "value",
           NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary>
        /// The status code associated with the value.
        /// </summary>
        [JsonProperty(PropertyName = "statusCode",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? StatusCode { get; set; }

        /// <summary>
        /// The source timestamp associated with the value.
        /// </summary>
        [JsonProperty(PropertyName = "sourceTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the source timestamp.
        /// </summary>
        [JsonProperty(PropertyName = "sourcePicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// The server timestamp associated with the value.
        /// </summary>
        [JsonProperty(PropertyName = "serverTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the server timestamp.
        /// </summary>
        [JsonProperty(PropertyName = "serverPicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// modification information when reading modifications.
        /// </summary>
        [JsonProperty(PropertyName = "modificationInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ModificationInfoApiModel ModificationInfo { get; set; }
    }
}
