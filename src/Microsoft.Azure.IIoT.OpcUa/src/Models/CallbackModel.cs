// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System;

    /// <summary>
    /// A registered callback
    /// </summary>
    public class CallbackModel {

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
