// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Discovery event
    /// </summary>
    public class DiscoveryEventApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryEventApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryEventApiModel(DiscoveryEventModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            TimeStamp = model.TimeStamp;
            Index = model.Index;
            Registration = model.Registration == null ? null :
                new EndpointRegistrationApiModel(model.Registration);
            Application = model.Application == null ? null :
                new ApplicationInfoApiModel(model.Application);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscoveryEventModel ToServiceModel() {
            return new DiscoveryEventModel {
                TimeStamp = TimeStamp,
                Result = null,
                Registration = Registration?.ToServiceModel(),
                Application = Application?.ToServiceModel(),
                Index = Index
            };
        }

        /// <summary>
        /// Timestamp of the discovery sweep.
        /// </summary>
        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in the batch with same timestamp.
        /// </summary>
        [JsonProperty(PropertyName = "index")]
        public int Index { get; set; }

        /// <summary>
        /// Discovered endpoint in form of endpoint registration
        /// </summary>
        [JsonProperty(PropertyName = "registration")]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application to which this endpoint belongs
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        public ApplicationInfoApiModel Application { get; set; }
    }
}