// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
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
            Uri = model.Uri;
            AuthenticationHeader = model.AuthenticationHeader;
            Method = model.Method;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
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
        public Uri Uri { get; set; }

        /// <summary>
        /// Http method to use
        /// </summary>
        public CallbackMethodType? Method { get; set; }

        /// <summary>
        /// Authentication header to add or null if not needed
        /// </summary>
        public string AuthenticationHeader { get; set; }
    }
}
