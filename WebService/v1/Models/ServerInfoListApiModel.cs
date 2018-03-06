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
    /// List of registered servers
    /// </summary>
    public class ServerInfoListApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerInfoListApiModel() {
            Items = new List<ServerInfoApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerInfoListApiModel(ServerInfoListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new ServerInfoApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<ServerInfoApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public ServerInfoListModel ToServiceModel() {
            return new ServerInfoListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Server infos
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<ServerInfoApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
