// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// A registered callback
    /// </summary>
    public class CallbackApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public CallbackApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public CallbackApiModel(CallbackModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Uri = model.Uri;
            AuthenticationHeader = model.AuthenticationHeader;
            Method = model.Method;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public CallbackModel ToServiceModel() {
            return new CallbackModel {
                Uri = Uri,
                AuthenticationHeader = AuthenticationHeader,
                Method = Method
            };
        }

        /// <summary>
        /// Uri to call - should use https scheme in which
        /// case security is enforced.
        /// </summary>
        [JsonProperty(PropertyName = "uri")]
        public Uri Uri { get; set; }

        /// <summary>
        /// Http Method to use for callback
        /// </summary>
        [JsonProperty(PropertyName = "method",
            NullValueHandling = NullValueHandling.Ignore)]
        public CallbackMethodType? Method { get; set; }

        /// <summary>
        /// Authentication header to add or null if not needed
        /// </summary>
        [JsonProperty(PropertyName = "authenticationHeader",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AuthenticationHeader { get; set; }
    }
}
