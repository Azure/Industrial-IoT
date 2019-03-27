// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The events to delete
    /// </summary>
    public class DeleteEventsDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeleteEventsDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DeleteEventsDetailsApiModel(DeleteEventsDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            EventIds = model.EventIds;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DeleteEventsDetailsModel ToServiceModel() {
            return new DeleteEventsDetailsModel {
                EventIds = EventIds
            };
        }

        /// <summary>
        /// Events to delete
        /// </summary>
        [JsonProperty(PropertyName = "eventIds")]
        [Required]
        public List<byte[]> EventIds { get; set; }
    }
}
