// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Endpoint registration model
    /// </summary>
    public class EndpointInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointInfoApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointInfoApiModel(EndpointInfoModel model) {
            Registration = new EndpointRegistrationApiModel(model.Registration);
            ApplicationId = model.ApplicationId;
            NotSeenSince = model.NotSeenSince;
            ActivationState = model.ActivationState;
            OutOfSync = model.OutOfSync;
            EndpointState = model.EndpointState;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointInfoModel ToServiceModel() {
            return new EndpointInfoModel {
                ApplicationId = ApplicationId,
                NotSeenSince = NotSeenSince,
                Registration = Registration.ToServiceModel(),
                ActivationState = ActivationState,
                EndpointState = EndpointState,
                OutOfSync = OutOfSync
            };
        }

        /// <summary>
        /// Endpoint registration
        /// </summary>
        [JsonProperty(PropertyName = "registration")]
        [Required]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        [Required]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Activation state of endpoint
        /// </summary>
        [JsonProperty(PropertyName = "activationState",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public EndpointActivationState? ActivationState { get; set; }

        /// <summary>
        /// Last state of the activated endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpointState",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether the registration is out of sync
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Last time endpoint was seen
        /// </summary>
        [JsonProperty(PropertyName = "notSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DateTime? NotSeenSince { get; set; }
    }
}
