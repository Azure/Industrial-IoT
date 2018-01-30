// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Server registration list
    /// </summary>
    public class ServerRegistrationListApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerRegistrationListApiModel() {
            Items = new List<ServerRegistrationApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerRegistrationListApiModel(ServerRegistrationListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new ServerRegistrationApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<ServerRegistrationApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public ServerRegistrationListModel ToServiceModel() {
            return new ServerRegistrationListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<ServerRegistrationApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
