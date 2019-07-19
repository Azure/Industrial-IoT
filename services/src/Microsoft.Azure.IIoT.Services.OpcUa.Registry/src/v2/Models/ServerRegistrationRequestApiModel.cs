// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application registration request
    /// </summary>
    public class ServerRegistrationRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerRegistrationRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerRegistrationRequestApiModel(
            ServerRegistrationRequestModel model) {
            DiscoveryUrl = model.DiscoveryUrl;
            Callback = model.Callback == null ? null :
                new CallbackApiModel(model.Callback);
            ActivationFilter = model.ActivationFilter == null ? null :
                new EndpointActivationFilterApiModel(model.ActivationFilter);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ServerRegistrationRequestModel ToServiceModel() {
            return new ServerRegistrationRequestModel {
                DiscoveryUrl = DiscoveryUrl,
                RegistrationId = RegistrationId,
                Callback = Callback?.ToServiceModel(),
                ActivationFilter = ActivationFilter?.ToServiceModel()
            };
        }

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrl")]
        [Required]
        public string DiscoveryUrl { get; set; }

        /// <summary>
        /// Registration id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string RegistrationId { get; set; }

        /// <summary>
        /// An optional callback hook to register.
        /// </summary>
        [JsonProperty(PropertyName = "callback",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public CallbackApiModel Callback { get; private set; }

        /// <summary>
        /// Upon discovery, activate all endpoints with this filter.
        /// </summary>
        [JsonProperty(PropertyName = "activationFilter",
           NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
