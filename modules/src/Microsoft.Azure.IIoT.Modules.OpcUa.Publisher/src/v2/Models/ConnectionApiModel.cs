// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Connection model
    /// </summary>
    public class ConnectionApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ConnectionApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ConnectionApiModel(ConnectionModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Endpoint = model.Endpoint == null ? null :
                new EndpointApiModel(model.Endpoint);
            User = model.User == null ? null :
                new CredentialApiModel(model.User);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ConnectionModel ToServiceModel() {
            return new ConnectionModel {
                Endpoint = Endpoint?.ToServiceModel(),
                User = User?.ToServiceModel(),
                Diagnostics = Diagnostics?.ToServiceModel()
            };
        }

        /// <summary>
        /// Endpoint information
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Elevation
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        public CredentialApiModel User { get; set; }

        /// <summary>
        /// Diagnostics configuration
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
             NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}