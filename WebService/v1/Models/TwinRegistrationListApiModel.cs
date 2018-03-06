// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Twin registration list
    /// </summary>
    public class TwinRegistrationListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationListApiModel() {
            Items = new List<TwinRegistrationApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationListApiModel(TwinRegistrationListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new TwinRegistrationApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<TwinRegistrationApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationListModel ToServiceModel() {
            return new TwinRegistrationListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Twin registrations
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<TwinRegistrationApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
