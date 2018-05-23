// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {
    using System;

    /// <summary>
    /// Twin registration info
    /// </summary>
    public class TwinInfoModel {

        /// <summary>
        /// Twin registration
        /// </summary>
        public TwinRegistrationModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Whether twin is activated on supervisor
        /// </summary>
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether twin is running under supervisor on edge
        /// </summary>
        public bool? Connected { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (edge) and server (service) (default: false).
        /// </summary>
        public bool? OutOfSync { get; set; }
    }
}
