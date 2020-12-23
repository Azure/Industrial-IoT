// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;

    /// <summary>
    /// Device Twin Event
    /// </summary>
    public class DeviceTwinEvent {

        /// <summary>
        /// Event
        /// </summary>
        public DeviceTwinEventType Event { get; set; }

        /// <summary>
        /// Twin
        /// </summary>
        public DeviceTwinModel Twin { get; set; }

        /// <summary>
        /// Twin is not a patch
        /// </summary>
        public bool IsPatch { get; set; }

        /// <summary>
        /// User causing the change
        /// </summary>
        public string AuthorityId { get; set; }

        /// <summary>
        /// Timestamp of event
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether event was handled
        /// </summary>
        public bool Handled { get; set; }
    }
}
