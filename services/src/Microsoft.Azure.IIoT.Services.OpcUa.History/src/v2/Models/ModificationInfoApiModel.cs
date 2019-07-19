// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Modification information
    /// </summary>
    public class ModificationInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ModificationInfoApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ModificationInfoApiModel(ModificationInfoModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ModificationTime = model.ModificationTime;
            UpdateType = model.UpdateType;
            UserName = model.UserName;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ModificationInfoModel ToServiceModel() {
            return new ModificationInfoModel {
                ModificationTime = ModificationTime,
                UpdateType = UpdateType,
                UserName = UserName
            };
        }

        /// <summary>
        /// Modification time
        /// </summary>
        [JsonProperty(PropertyName = "modificationTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ModificationTime { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        [JsonProperty(PropertyName = "updateType",
            NullValueHandling = NullValueHandling.Ignore)]
        public HistoryUpdateOperation? UpdateType { get; set; }

        /// <summary>
        /// User who made the change
        /// </summary>
        [JsonProperty(PropertyName = "userName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string UserName { get; set; }
    }
}
